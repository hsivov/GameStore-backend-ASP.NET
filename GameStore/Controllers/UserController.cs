using GameStore.Models.DTO;
using GameStore.Models.Entities;
using GameStore.Repositories;
using GameStore.Services.Impl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GameStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IShoppingCartRepository _shoppingCartRepository;
        private readonly BlobService _blobService;
        private readonly ILogger<UserController> _logger;
        private readonly IGameRepository _gameRepository;
        // Allowed image extensions
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };

        public UserController(UserManager<ApplicationUser> userManager, IShoppingCartRepository shoppingCartRepository, ILogger<UserController> logger, BlobService blobService, IGameRepository gameRepository)
        {
            _userManager = userManager;
            _shoppingCartRepository = shoppingCartRepository;
            _logger = logger;
            _blobService = blobService;
            _gameRepository = gameRepository;
        }

        [HttpPost("edit-profile")]
        [Authorize]
        public async Task<IActionResult> EditProfile(EditUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }
            var applicationUser = await _userManager.FindByNameAsync(username);
            if (applicationUser == null)
            {
                return NotFound(new { message = "User not found." });
            }
            applicationUser.Email = request.Email;
            applicationUser.FirstName = request.FirstName;
            applicationUser.LastName = request.LastName;
            applicationUser.Age = request.Age;
            var result = await _userManager.UpdateAsync(applicationUser);
            if (result.Succeeded)
            {
                return Ok(new { message = "Profile updated successfully." });
            }
            return BadRequest(new { message = "Failed to update profile." });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }

            var applicationUser = await _userManager.FindByNameAsync(username);
            if (applicationUser == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var result = await _userManager.ChangePasswordAsync(applicationUser, request.Password, request.NewPassword);
            if (result.Succeeded)
            {
                return Ok(new { message = "Password changed successfully." });
            }
            return BadRequest(new { message = "Failed to change password.", errors = result.Errors.Select(e => e.Description) });
        }

        [HttpPost("profile/image-upload")]
        [Authorize]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var remoteAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogInformation("Request received from: {RemoteAddress}", remoteAddress);

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Invalid file provided.");
                return BadRequest(new { message = "Please provide a valid file." });
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning("Invalid file type: {FileName}", file.FileName);
                return BadRequest("Invalid file type. Only JPG, JPEG, PNG, GIF, BMP, TIFF, and WEBP files are allowed.");
            }

            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string randomString = Guid.NewGuid().ToString("N").Substring(0, 8);
            string newFileName = $"{username}_{timestamp}_{randomString}{extension}";

            _logger.LogInformation("Uploading file: {FileName}", newFileName);

            using var stream = file.OpenReadStream();
            try
            {
                var url = await _blobService.UploadFileAsync(stream, newFileName);
                var user = await _userManager.FindByNameAsync(username);
                if (user == null)
                {
                    return NotFound(new { message = "User not found." });
                }
                user.ProfilePictureUrl = url;
                await _userManager.UpdateAsync(user);

                return Ok(new { FileUrl = url });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("library")]
        [Authorize]
        public async Task<IActionResult> GetLibrary()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }

            var user = await _userManager.Users
                .Include(u => u.OwnedGames)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var ownedGamesDto = user.OwnedGames.Select(game => new OwnedGameDTO
            {
                Id = game.Id,
                Title = game.Title,
                ImageUrl = game.ImageUrl
            }).ToList();

            return Ok(ownedGamesDto);
        }

        [HttpPost("library/add-game/{id}")]
        [Authorize]
        public async Task<IActionResult> AddGameToLibrary(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }
            var user = await _userManager.Users
                .Include(u => u.OwnedGames)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }
            var game = await _gameRepository.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { message = "Game not found." });
            }
            if (user.OwnedGames.Any(g => g.Id == id))
            {
                return BadRequest(new { message = "Game is already in the library." });
            }
            user.OwnedGames.Add(game);
            await _userManager.UpdateAsync(user);
            return Ok(new { message = "Game added to library." });
        }

        [HttpGet("shopping-cart")]
        [Authorize]
        public async Task<IActionResult> GetShoppingCart()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var shoppingCart = await _shoppingCartRepository.GetShoppingCartByCustomerAsync(user);

            if (shoppingCart == null)
            {
                shoppingCart = new ShoppingCart
                {
                    Id = Guid.NewGuid(),
                    Customer = user
                };
                await _shoppingCartRepository.SaveShoppingCart(shoppingCart);
            }

            var shoppingCartDto = new ShoppingCartDTO
            {
                Id = shoppingCart.Id,
                Games = shoppingCart.Games.Select(game => new GameDTO
                {
                    Id = game.Id,
                    Title = game.Title,
                    ImageUrl = game.ImageUrl,
                    Price = game.Price
                }).ToList(),
                TotalPrice = shoppingCart.TotalPrice,
                ItemCount = shoppingCart.Count
            };

            return Ok(shoppingCartDto);
        }

        [HttpPost("shopping-cart/add-game/{id}")]
        [Authorize]
        public async Task<IActionResult> AddGameToShoppingCart(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }
            var shoppingCart = await _shoppingCartRepository.GetShoppingCartByCustomerAsync(user);
            if (shoppingCart == null)
            {
                shoppingCart = new ShoppingCart
                {
                    Id = Guid.NewGuid(),
                    Customer = user
                };
                await _shoppingCartRepository.SaveShoppingCart(shoppingCart);
            }
            var game = await _gameRepository.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound(new { message = "Game not found." });
            }
            if (shoppingCart.Games.Any(g => g.Id == id))
            {
                return BadRequest(new { message = "Game is already in the shopping cart." });
            }
            shoppingCart.Games.AddLast(game);
            await _shoppingCartRepository.UpdateShoppingCart(shoppingCart);
            return Ok(new { message = "Game added to shopping cart." });
        }

        [HttpDelete("shopping-cart/remove-game/{id}")]
        [Authorize]
        public async Task<IActionResult> RemoveGameFromShoppingCart(Guid id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }
            var shoppingCart = await _shoppingCartRepository.GetShoppingCartByCustomerAsync(user);
            if (shoppingCart == null)
            {
                return NotFound(new { message = "Shopping cart not found." });
            }
            var game = shoppingCart.Games.FirstOrDefault(g => g.Id == id);
            if (game == null)
            {
                return NotFound(new { message = "Game not found in the shopping cart." });
            }
            shoppingCart.Games.Remove(game);
            await _shoppingCartRepository.UpdateShoppingCart(shoppingCart);
            return Ok(new { message = "Game removed from shopping cart." });
        }

        [HttpPost("shopping-cart/remove-all")]
        [Authorize]
        public async Task<IActionResult> RemoveAllGamesFromShoppingCart()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }
            var shoppingCart = await _shoppingCartRepository.GetShoppingCartByCustomerAsync(user);
            if (shoppingCart == null)
            {
                return NotFound(new { message = "Shopping cart not found." });
            }
            shoppingCart.Games.Clear();
            await _shoppingCartRepository.UpdateShoppingCart(shoppingCart);
            return Ok(new { message = "All games removed from shopping cart." });
        }
    }
}
