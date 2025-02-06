using GameStore.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace GameStore.Models.DTO
{
    public class AddGameRequest
    {
        private DateTime _releaseDate;

        [Required]
        [StringLength(30, MinimumLength = 5)]
        public required string Title { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 5)]
        public required string Description { get; set; }

        [Required]
        [Url]
        public required string ImageUrl { get; set; }

        public string? VideoUrl { get; set; }

        [Required]
        public DateTime ReleaseDate {
            get => _releaseDate;
            set => _releaseDate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        [Required]
        [StringLength(20, MinimumLength = 2)]
        public required string Publisher { get; set; }

        [Required]
        public required string Genre { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; }
    }
}
