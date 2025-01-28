using GameStore.Models.Entities;

namespace GameStore.Repositories
{
    public interface IGameRepository
    {
        Task<Game> GetByIdAsync(Guid id);
        Task<IEnumerable<Game>> GetAllAsync();
        Task<IEnumerable<Comment>> GetCommentsAsync(Guid id);
        Task AddAsync(Game game);
        Task UpdateAsync(Game game);
        Task DeleteAsync(Guid id);
        Task<bool> AddCommentAsync(Comment comment);
    }
}
