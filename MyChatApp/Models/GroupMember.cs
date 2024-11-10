namespace MyChatApp.Models
{
    public class GroupMember
    {
        public Guid GroupMemberId { get; set; } = Guid.NewGuid();

        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public required Guid GroupId { get; set; }
        public Group? Group { get; set; }
    }
}
