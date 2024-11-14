using System.ComponentModel.DataAnnotations;

namespace MyChatApp.Models
{
    public class Message
    {
        //Guid for globally unique identifier
        public Guid MessageId { get; set; } = Guid.NewGuid();
        public string SenderId { get; set; }
        public string RecipientId { get; set; }
        public Guid? GroupId { get; set; }
        [Required]
        public string Content { get; set; }
        public string? FileUrl { get; set; }
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; }
    }
}
