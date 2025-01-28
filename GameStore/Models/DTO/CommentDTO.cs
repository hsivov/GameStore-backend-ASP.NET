namespace GameStore.Models.DTO
{
    public class CommentDTO
    {
        public int Id { get; set; }
        public required string Content { get; set; }
        public required string AuthorName { get; set; }
        public required string AuthorAvatarUrl { get; set; }
        public required string CreatedAt { get; set; }
    }
}
