namespace GameStore.Models.DTO
{
    public class OrderDTO
    {
        public int Id {  get; set; }
        public List<OrderGameDTO> BoughtGames { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public string OrderDate { get; set; }
        public string CustomerName { get; set; }
    }
}
