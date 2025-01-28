using System.ComponentModel.DataAnnotations;

namespace GameStore.Models.DTO
{
    public class RegisterUserRequest
    {
        [Required]
        [MinLength(3), MaxLength(20)]
        public required string Username { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "First name must be at least 4 characters long.")]
        [MaxLength(20, ErrorMessage = "First name cannot exceed 20 characters.")]
        public required string FirstName { get; set; }

        [Required]
        [MinLength(2, ErrorMessage = "First name must be at least 4 characters long.")]
        [MaxLength(20, ErrorMessage = "First name cannot exceed 20 characters.")]
        public required string LastName { get; set; }

        [Required]
        [Range(13, 120, ErrorMessage = "Age must be between 13 and 120.")]
        public int Age { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public required string ConfirmPassword { get; set; }
    }
}
