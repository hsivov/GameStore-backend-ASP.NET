using GameStore.Models.DTO;
using GameStore.Models.Entities;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Services
{
    public interface IGameService
    {
        Task<bool> AddCommentAsync(string? userId, AddCommentRequest request);
        Task<Game> AddGameAsync(AddGameRequest request);
        Task DeleteGameAsync(Guid gameId);
        Task<IEnumerable<GameDTO>> GetAllGamesAsync();
        Task<GameDTO> GetGameByIdAsync(Guid id);
        Task<IEnumerable<CommentDTO>> GetGameCommentsAsync(Guid id);
    }
}
