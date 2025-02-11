using GameStore.Models.DTO;
using GameStore.Models.Entities;
using GameStore.Models.Enums;
using GameStore.Repositories;
using GameStore.Services;
using GameStore.Services.Impl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

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
        private readonly IOrderRepository _orderRepository;
        private readonly IEmailService _emailService;
        // Allowed image extensions
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };

        public UserController(UserManager<ApplicationUser> userManager, IShoppingCartRepository shoppingCartRepository, ILogger<UserController> logger, BlobService blobService, IGameRepository gameRepository, IOrderRepository orderRepository, IEmailService emailService)
        {
            _userManager = userManager;
            _shoppingCartRepository = shoppingCartRepository;
            _logger = logger;
            _blobService = blobService;
            _gameRepository = gameRepository;
            _orderRepository = orderRepository;
            _emailService = emailService;
        }

        [HttpGet("orders")]
        [Authorize]
        public async Task<IActionResult> GetOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { message = "The provided token is invalid or has expired. Please authenticate again." });
            }
            
            var userOrders = await _orderRepository.GetOrdersByUserAsync(userId);

            var orderDtos = userOrders.Select(order => new OrderDTO
            {
                Id = order.Id,
                OrderDate = order.OrderDate.ToString("dd.MM.yyyy HH:mm"),
                Status = order.Status.ToString(),
                TotalPrice = order.TotalPrice,
                BoughtGames = order.BoughtGames.Select(game => new OrderGameDTO
                {
                    Id = game.Id,
                    Title = game.Title,
                    Price = game.Price
                }).ToList()
            });

            return Ok(orderDtos);
        }

        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            return Ok(new OrderDTO
            {
                Id = order.Id,
                TotalPrice = order.TotalPrice,
                Status = order.Status.ToString(),
                BoughtGames = order.BoughtGames.Select(g => new OrderGameDTO
                {
                    Id = g.Id,
                    Title = g.Title,
                    Price = g.Price
                }).ToList(),
                OrderDate = order.OrderDate.ToString("dd.MM.yyyy HH:mm:ss"),
                CustomerName = order.Customer.FirstName + " " + order.Customer.LastName
            });
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
                var url = await _blobService.UploadFileAsync(stream, newFileName, "profile-images");
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

            var order = new Order
            {
                Customer = user,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Approved,
                TotalPrice = game.Price
            };
            order.BoughtGames.Add(game);
            user.OwnedGames.Add(game);
            await _orderRepository.AddOrderAsync(order);
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

        [HttpPost("shopping-cart/checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout()
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
                return BadRequest(new { message = "Shopping cart is not found." });
            }

            var order = new Order
            {
                Customer = user,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Approved,
                TotalPrice = shoppingCart.TotalPrice
            };

            foreach (var game in shoppingCart.Games)
            {
                order.BoughtGames.Add(game);
                user.OwnedGames.Add(game);
            }

            shoppingCart.Games.Clear();

            await _orderRepository.AddOrderAsync(order);
            await _userManager.UpdateAsync(user);
            await _shoppingCartRepository.UpdateShoppingCart(shoppingCart);

            string message = CreateConfirmationEmail(order, user);
            string subject = "Order Confirmation";
            await _emailService.SendEmailAsync(user.Email, subject, message);

            return Ok(new { message = "Checkout successful." });
        }

        private static string CreateConfirmationEmail(Order order, ApplicationUser customer)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Game game in order.BoughtGames)
            {
                sb.Append("<li>").Append(game.Title).Append("</li>");
            }

            return "<!DOCTYPE html>\n" +
                    "<html lang=\"en\">\n" +
                    "<head>\n" +
                    "    <meta charset=\"UTF-8\">\n" +
                    "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">\n" +
                    "    <title>Order Confirmation</title>\n" +
                    "</head>\n" +
                    "<body style=\"font-family: Arial, sans-serif; color: #333; line-height: 1.6; margin: 0; padding: 0;\">\n" +
                    "    <div style=\"max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd;\">\n" +
                    "        <h2 style=\"color: #2d3748;\">Thank You for Your Purchase!</h2>\n" +
                    "        \n" +
                    "        <p>Hi <strong>" + customer.FirstName + "</strong>,</p>\n" +
                    "        <p>Thank you for your recent purchase with <strong>Game Store</strong>! We’re excited to let you know that your order (#<strong>" + order.Id + "</strong>) has been successfully processed.</p>\n" +
                    "        \n" +
                    "        <h3 style=\"color: #4a5568;\">Order Details:</h3>\n" +
                    "        <ul>\n" +
                    "            <li><strong>Item(s) Purchased: </strong></li>\n" +
                    "       " + sb + "\n" +
                    "            <li><strong>Order Total: </strong>" + order.TotalPrice + " лв.</li>\n" +
                    "        </ul>\n" +
                    "\n" +
                    "        <h3 style=\"color: #4a5568;\">What’s Next?</h3>\n" +
                    "        <p><strong>Need Help?</strong> If you have any questions or need further assistance, feel free to reach out to us at <a href=\"mailto:[Customer Support Email]\" style=\"color: #3182ce;\">[Customer Support Email]</a> or call us at [Phone Number].</p>\n" +
                    "\n" +
                    "        <p>We truly appreciate your business and hope you love your new [product]! Keep an eye out for future offers and product updates from us.</p>\n" +
                    "\n" +
                    "        <p>Thanks again for choosing <strong>Game Store</strong>!</p>\n" +
                    "\n" +
                    "        <p>Best regards,</p>\n" +
                    "        <p><strong>The Game Store Team</strong></p>\n" +
                    "        <p>[Company Contact Information]</p>\n" +
                    "        <p><a href=\"[Company Social Media Link]\" style=\"color: #3182ce;\">Follow us on social media</a></p>\n" +
                    "    </div>\n" +
                    "</body>\n" +
                    "</html>\n";
        }
    }
}
