using GameStore.Models.DTO;
using GameStore.Models.Entities;
using GameStore.Models.Enums;
using GameStore.Repositories;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGameService _gameService;
        private readonly IGenreRepository _genreRepository;
        public AdminController(UserManager<ApplicationUser> userManager, IGameService gameService, IGenreRepository genreRepository)
        {
            _userManager = userManager;
            _gameService = gameService;
            _genreRepository = genreRepository;
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public IEnumerable<UserDTO> GetUsers()
        {
            var users = _userManager.Users;
            return users
                .OrderBy(u => u.Role.ToString())
                .Select(u => new UserDTO
                {
                    Id = u.Id,
                    Username = u.UserName,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role.ToString(),
                    IsConfirmed = u.EmailConfirmed
                });
        }

        [HttpPost("user/enable/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EnableUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        [HttpPost("user/disable/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DisableUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            user.EmailConfirmed = false;
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        [HttpPost("user/promote/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PromoteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            user.Role = RoleName.Admin;
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        [HttpPost("user/demote/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DemoteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            user.Role = RoleName.User;
            await _userManager.UpdateAsync(user);
            return Ok();
        }

        [HttpPost("add-game")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddGame(AddGameRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _gameService.AddGameAsync(request);

            return StatusCode(201, new { message = "Game was added successfully" });
        }

        [HttpDelete("delete-game/{gameId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteGame(Guid gameId)
        {
            var game = await _gameService.GetGameByIdAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }
            await _gameService.DeleteGameAsync(gameId);
            return Ok();
        }

        [HttpGet("genres")]
        [Authorize(Roles = "Admin")]
        public async Task<IEnumerable<Genre>> GetGenres()
        {
            return await _genreRepository.GetAllAsync();
        }

        [HttpPost("add-genre")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddGenre(AddGenreRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var genre = new Genre()
            { 
                Name = request.Name,
                Description = request.Description
            };

            await _genreRepository.AddAsync(genre);
            return StatusCode(201, new { message = "Genre was added successfully" });
        }

        [HttpGet("genre/{genreId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGenre(int genreId)
        {
            var genre = await _genreRepository.GetByIdAsync(genreId);
            if (genre == null)
            {
                return NotFound();
            }
            return Ok(genre);
        }

        [HttpPut("update-genre/{genreId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateGenre(int genreId, AddGenreRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var genre = await _genreRepository.GetByIdAsync(genreId);
            if (genre == null)
            {
                return NotFound();
            }
            genre.Name = request.Name;
            genre.Description = request.Description;
            await _genreRepository.UpdateAsync(genre);
            return Ok();
        }

        [HttpDelete("delete-genre/{genreId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteGenre(int genreId)
        {
            var genre = await _genreRepository.GetByIdAsync(genreId);
            if (genre == null)
            {
                return NotFound();
            }

            await _genreRepository.DeleteAsync(genreId);
            return Ok();
        }
    }
}
