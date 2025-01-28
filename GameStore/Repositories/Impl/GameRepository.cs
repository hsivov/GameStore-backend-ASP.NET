using GameStore.Data;
using GameStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Repositories.Impl
{
    public class GameRepository : IGameRepository
    {
        private readonly ApplicationDbContext _context;

        public GameRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Game> GetByIdAsync(Guid id)
        {
            return await _context.Games
                .Include(g => g.Genre)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<IEnumerable<Comment>> GetCommentsAsync(Guid id)
        {
            return await _context.Comments
                .Where(c => c.Game.Id == id)
                .Include(c => c.Author)
                .ToListAsync();
        }

        public async Task<bool> AddCommentAsync(Comment comment)
        {
            var game = await GetByIdAsync(comment.Game.Id);
            if (game == null)
            {
                return false;
            }
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Game>> GetAllAsync()
        {
            return await _context.Games.Include(g => g.Genre).ToListAsync();
        }

        public async Task AddAsync(Game game)
        {
            await _context.Games.AddAsync(game);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Game game)
        {
            _context.Games.Update(game);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var game = await GetByIdAsync(id);
            if (game != null)
            {
                _context.Games.Remove(game);
                await _context.SaveChangesAsync();
            }
        }
    }
}
