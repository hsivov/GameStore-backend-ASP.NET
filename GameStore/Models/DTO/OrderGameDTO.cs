namespace GameStore.Models.DTO
{
    public class OrderGameDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
    }
}
