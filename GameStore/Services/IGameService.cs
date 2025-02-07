using GameStore.Models.DTO;
using GameStore.Models.Entities;

namespace GameStore.Services
{
    public interface IGameService
    {
        Task<bool> AddCommentAsync(string? userId, AddCommentRequest request);
        Task<Game> AddGameAsync(AddGameRequest request);
        Task UpdateGameAsync(Guid id, AddGameRequest request);
        Task DeleteGameAsync(Guid gameId);
        Task<IEnumerable<GameDTO>> GetAllGamesAsync();
        Task<GameDTO> GetGameByIdAsync(Guid id);
        Task<IEnumerable<CommentDTO>> GetGameCommentsAsync(Guid id);
    }
}
