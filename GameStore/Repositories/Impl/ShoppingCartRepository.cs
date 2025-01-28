using GameStore.Data;
using GameStore.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Repositories.Impl
{
    public class ShoppingCartRepository : IShoppingCartRepository
    {
        private readonly ApplicationDbContext _context;

        public ShoppingCartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShoppingCart> GetShoppingCartAsync(Guid id)
        {
            return await _context.ShoppingCarts.FindAsync(id);
        }

        public async Task<ShoppingCart> GetShoppingCartByCustomerAsync(ApplicationUser customer)
        {
            return await _context.ShoppingCarts
                .Include(sc => sc.Games)
                .FirstOrDefaultAsync(sc => sc.Customer.Id == customer.Id);
        }

        public async Task SaveShoppingCart(ShoppingCart shoppingCart)
        {
            await _context.ShoppingCarts.AddAsync(shoppingCart);
            await _context.SaveChangesAsync();
        }
    }
}
