using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models.Entities
{
    [Table("games")]
    public class Game
    {
        public Guid Id { get; set; }

        public required string Title { get; set; }

        [Column(TypeName = "text")]
        public required string Description { get; set; }

        public required string ImageUrl { get; set; }

        public string? VideoUrl { get; set; }

        public DateTime ReleaseDate { get; set; }

        public required string Publisher { get; set; }

        [ForeignKey("GenreId")]
        public required Genre Genre { get; set; }

        public decimal Price { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
