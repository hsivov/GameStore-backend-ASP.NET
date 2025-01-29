using System.ComponentModel.DataAnnotations;

namespace GameStore.Models.DTO
{
    public class EditUserRequest
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "First name must be at least 2 characters long.")]
        [MaxLength(20, ErrorMessage = "First name cannot exceed 20 characters.")]
        public required string FirstName { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "Last name must be at least 2 characters long.")]
        [MaxLength(20, ErrorMessage = "Last name cannot exceed 20 characters.")]
        public required string LastName { get; set; }

        [Required]
        [Range(13, 120, ErrorMessage = "Age must be between 13 and 120.")]
        public int Age { get; set; }
    }
}
