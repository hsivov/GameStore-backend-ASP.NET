using GameStore.Models.Entities;

namespace GameStore.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> GetOrderByIdAsync(int id);
        Task<IEnumerable<Order>> GetOrdersAsync();
    }
}
