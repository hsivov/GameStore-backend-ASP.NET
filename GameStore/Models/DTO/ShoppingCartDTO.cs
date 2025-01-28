namespace GameStore.Models.DTO
{
    public class ShoppingCartDTO
    {
        public Guid Id { get; set; }
        public List<GameDTO> Games { get; set; }
        public decimal TotalPrice { get; set; }
        public int ItemCount { get; set; }
    }
}
