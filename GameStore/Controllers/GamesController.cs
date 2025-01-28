using GameStore.Models.DTO;
using GameStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GameStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GamesController : ControllerBase
    {
        private readonly IGameService _gameService;

        public GamesController(IGameService gameService)
        {
            _gameService = gameService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGames()
        {
            var games = await _gameService.GetAllGamesAsync();

            if (games == null || !games.Any())
            {
                return NotFound("No games found.");
            }

            return Ok(games);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGameById(Guid id)
        {
            var gameDto = await _gameService.GetGameByIdAsync(id);

            if (gameDto == null)
            {
                return NotFound($"Game with id {id} not found.");
            }

            return Ok(gameDto);
        }

        [HttpGet("game-details/comments/{id}")]
        public async Task<IActionResult> GetGameComments(Guid id)
        {
            var comments = await _gameService.GetGameCommentsAsync(id);

            if (comments == null || !comments.Any())
            {
                return NotFound("No comments found.");
            }

            return Ok(comments);
        }

        [HttpPost("game-details/comment/add")]
        [Authorize]
        public async Task<IActionResult> AddComment([FromBody] AddCommentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isCommentAdded = await _gameService.AddCommentAsync(userId, request);

            if (!isCommentAdded)
            {
                return BadRequest("Failed to add comment.");
            }

            return Ok("Comment added successfully.");
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddGame([FromBody] AddGameRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var game = await _gameService.AddGameAsync(request);
            return CreatedAtAction(nameof(AddGame), new { id = game.Id }, game);
        }
    }
}
