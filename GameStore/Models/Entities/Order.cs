using GameStore.Models.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public ICollection<Game> BoughtGames { get; set; } = new List<Game>();
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime OrderDate { get; set; }

        [ForeignKey("UserId")]
        public required ApplicationUser Customer { get; set; }
    }
}
