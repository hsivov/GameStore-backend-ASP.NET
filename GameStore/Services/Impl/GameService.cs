using GameStore.Exceptions;
using GameStore.Models.DTO;
using GameStore.Models.Entities;
using GameStore.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Services.Impl
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGenreRepository _genreRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public GameService(IGameRepository gameRepository, IGenreRepository genreRepository, UserManager<ApplicationUser> userManager)
        {
            _gameRepository = gameRepository;
            _genreRepository = genreRepository;
            _userManager = userManager;
        }

        public async Task<IEnumerable<GameDTO>> GetAllGamesAsync()
        {
            var games = await _gameRepository.GetAllAsync();

            var gameDtos = games.Select(game => new GameDTO
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                Price = game.Price,
                ReleaseDate = game.ReleaseDate.ToString("dd-MM-yyyy"),
                Publisher = game.Publisher,
                Genre = game.Genre?.Name ?? "Unknown",
                ImageUrl = game.ImageUrl,
                VideoUrl = game.VideoUrl
            }).ToList();

            return gameDtos;
        }

        public async Task<GameDTO> GetGameByIdAsync(Guid id)
        {
            var game = await _gameRepository.GetGameByIdAsync(id);

            if (game == null)
            {
                return null;
            }

            var gameDto = new GameDTO
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                Price = game.Price,
                ReleaseDate = game.ReleaseDate.ToString("yyyy-MM-dd"),
                Publisher = game.Publisher,
                Genre = game.Genre.Name,
                ImageUrl = game.ImageUrl,
                VideoUrl = game.VideoUrl
            };

            return gameDto;
        }

        public async Task<IEnumerable<CommentDTO>> GetGameCommentsAsync(Guid id)
        {
            var comments = await _gameRepository.GetCommentsAsync(id);
            if (comments == null)
            {
                return null;
            }

            var commentsDto = comments.Select(comment => new CommentDTO
            {
                Id = comment.Id,
                Content = comment.Content,
                AuthorName = comment.Author.UserName,
                AuthorAvatarUrl = comment.Author.ProfilePictureUrl,
                CreatedAt = comment.CreatedAt.ToString("dd.MM.yyyy HH:mm")
            }).ToList();

            return commentsDto;
        }

        public async Task<bool> AddCommentAsync(string? userId, AddCommentRequest request)
        {
            var game = await _gameRepository.GetGameByIdAsync(request.GameId);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || game == null)
            {
                return false;
            }

            var comment = new Comment
            {
                Content = request.Content,
                Author = user,
                Game = game,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _gameRepository.AddCommentAsync(comment);
            if (!result)
            {
                return false;
            }

            return true;
        }

        public async Task<Game> AddGameAsync(AddGameRequest request)
        {
            var genre = await _genreRepository.GetByNameAsync(request.Genre);

            var game = new Game
            {
                Title = request.Title,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                VideoUrl = request.VideoUrl,
                ReleaseDate = request.ReleaseDate,
                Publisher = request.Publisher,
                Price = request.Price,
                Genre = genre
            };

            await _gameRepository.AddAsync(game);

            return game;
        }

        public async Task UpdateGameAsync(Guid id, AddGameRequest request)
        {
            var game = await _gameRepository.GetGameByIdAsync(id);
            if (game == null)
            {
                throw new GameNotFoundException();
            }
            var genre = await _genreRepository.GetByNameAsync(request.Genre);
            game.Title = request.Title;
            game.Description = request.Description;
            game.ImageUrl = request.ImageUrl;
            game.VideoUrl = request.VideoUrl;
            game.ReleaseDate = request.ReleaseDate;
            game.Publisher = request.Publisher;
            game.Price = request.Price;
            game.Genre = genre;
            await _gameRepository.UpdateAsync(game);
        }

        public async Task DeleteGameAsync(Guid id)
        {
            var game = await _gameRepository.GetGameByIdAsync(id);
            if (game == null)
            {
                return;
            }
            await _gameRepository.DeleteAsync(id);
        }
    }
}
