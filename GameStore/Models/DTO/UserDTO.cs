namespace GameStore.Models.DTO
{
    public class UserDTO
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Role { get; set; }
        public string ProfilePictureUrl { get; set; } = string.Empty;
        public List<OwnedGameDTO> OwnedGames { get; set; } = [];
    }
}
