using GameStore.Models.DTO;
using GameStore.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace GameStore.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Age { get; set; }
        public RoleName Role { get; set; } = RoleName.User;
        public List<Game> OwnedGames { get; set; } = [];
        public string ProfilePictureUrl { get; set; } = string.Empty;
    }
}
