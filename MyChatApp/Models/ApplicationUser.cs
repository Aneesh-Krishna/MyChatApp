using Microsoft.AspNetCore.Identity;

namespace MyChatApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? ProfileImageUrl { get; set; }
        public DateTime LastActive { get; set; } = DateTime.Now;
    }
}
