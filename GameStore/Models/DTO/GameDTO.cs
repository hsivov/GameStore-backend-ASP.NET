namespace GameStore.Models.DTO
{
    public class GameDTO
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public string ReleaseDate { get; set; }
        public string Publisher { get; set; }
        public string Genre { get; set; }
        public decimal Price { get; set; }
    }
}
