using System.ComponentModel.DataAnnotations;

namespace MyChatApp.Models
{
    public class Group
    {
        public Guid GroupId { get; set; }
        [Required]
        public string GroupName { get; set; }
        public ICollection<GroupMember> GroupMembers { get; set; } = new List<GroupMember>();
    }
}
