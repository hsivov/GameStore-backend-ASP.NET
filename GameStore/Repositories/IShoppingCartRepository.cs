using GameStore.Models.Entities;

namespace GameStore.Repositories
{
    public interface IShoppingCartRepository
    {
        Task<ShoppingCart> GetShoppingCartAsync(Guid id);
        Task<ShoppingCart> GetShoppingCartByCustomerAsync(ApplicationUser customer);
        Task SaveShoppingCart(ShoppingCart shoppingCart);
        Task UpdateShoppingCart(ShoppingCart shoppingCart);
    }
}
