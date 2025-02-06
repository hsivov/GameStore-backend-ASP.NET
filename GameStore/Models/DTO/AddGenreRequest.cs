using System.ComponentModel.DataAnnotations;

namespace GameStore.Models.DTO
{
    public class AddGenreRequest
    {
        [Required]
        [StringLength(20, MinimumLength = 3)]
        public required string Name { get; set; }
        [Required]
        [StringLength(500, MinimumLength = 5)]
        public required string Description { get; set; }
    }
}
