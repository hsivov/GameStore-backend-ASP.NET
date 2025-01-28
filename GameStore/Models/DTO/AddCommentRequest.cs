namespace GameStore.Models.DTO
{
    public class AddCommentRequest
    {
        public Guid GameId { get; set; }
        public required string Content { get; set; }
    }
}
