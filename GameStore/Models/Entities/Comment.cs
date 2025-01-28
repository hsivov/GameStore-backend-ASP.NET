using System.ComponentModel.DataAnnotations.Schema;

namespace GameStore.Models.Entities
{
    public class Comment
    {
        public int Id { get; set; }

        [Column(TypeName = "text")]
        public required string Content { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("AuthorId")]
        public required ApplicationUser Author { get; set; }

        [ForeignKey("GameId")]
        public required Game Game { get; set; }
    }
}
