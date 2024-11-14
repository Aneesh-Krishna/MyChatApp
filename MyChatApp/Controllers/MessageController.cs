using Microsoft.AspNetCore.Mvc;
using MyChatApp.Services;
using MyChatApp.Models;
using MyChatApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using MyChatApp.Data;
using Microsoft.EntityFrameworkCore;

namespace MyChatApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly FileService _fileService;
        private readonly IHubContext<ChatHub> _chatHub;
        private readonly ChatDbContext _context;

        public MessageController(IMessageService messageService, FileService fileService, IHubContext<ChatHub> chatHub, ChatDbContext context)
        {
            _messageService = messageService;
            _fileService = fileService;
            _chatHub = chatHub;
            _context = context;
        }

        [HttpPost("SendMessageToUser")]
        public async Task<IActionResult> SendPrivateMessage([FromForm] string RecipientId, [FromForm] string Content,[FromForm] IFormFile? file = null)
        {
            if (string.IsNullOrWhiteSpace(Content) || string.IsNullOrWhiteSpace(RecipientId))
            {
                return BadRequest("RecipientId and Content are required.");
            }

            string? fileUrl = null;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User ID not found.");

            if (file != null)
            {
                fileUrl = await _fileService.UploadFileAsync(file);
                if (fileUrl == null) return BadRequest("File upload failed");
            }

            var msg = await _messageService.SendMessageToUser(userId, RecipientId, Content, fileUrl);
            if (msg == null) return BadRequest("Failed to send message");

            await _chatHub.Clients.User(RecipientId).SendAsync("ReceiveMessage", msg);

            return Ok(msg);
        }

        [HttpPost("SendMessageToGroup")]
        public async Task<IActionResult> SendGroupMessage([FromForm] Guid GroupId, [FromForm] string Content,[FromForm] IFormFile? file = null)
        {
            if (string.IsNullOrWhiteSpace(GroupId.ToString()) || string.IsNullOrWhiteSpace(Content)) return BadRequest("No group selected/No content.");

            string? fileUrl = null;
            if (file != null)
            {
                fileUrl = await _fileService.UploadFileAsync(file);
                if (fileUrl == null) return BadRequest("File upload failed");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User ID not found.");
            Console.WriteLine($"Retrieved UserId: {userId}");

            var IsMember = await _context.GroupMembers
                .Where(gm => (gm.GroupId == GroupId && gm.UserId == userId))
                .FirstOrDefaultAsync() != null;
            if (!IsMember)
            {
                throw new UnauthorizedAccessException("Not a member of the group");
            }

            var msg = await _messageService.SendMessageToGroup(userId, GroupId, Content, fileUrl);
            if (msg == null) return BadRequest("Failed to send message");

            await _chatHub.Clients.Group(GroupId.ToString()).SendAsync("ReceiveGroupMessage", msg);

            return Ok(msg);
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User ID not found.");

            var hasGroupId = await _context.Messages
                .Where(m => (m.MessageId == messageId && m.GroupId != null))
                .Select(m => m.GroupId)
                .FirstOrDefaultAsync();
            if(hasGroupId != null)
            {
                var IsMember = await _context.GroupMembers
                    .Where(gm => (gm.GroupId == hasGroupId && gm.UserId == userId))
                    .FirstOrDefaultAsync() != null;
                if (!IsMember)
                    return Unauthorized("Not a member of the group.");
            }

            var success = await _messageService.DeleteMessage(userId, messageId);
            if (!success) return Forbid("You are not authorized to delete this message.");

            return Ok(new { message = "Message deleted successfully." });
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("File is empty.");

            var fileUrl = await _fileService.UploadFileAsync(file);
            if (fileUrl == null) return BadRequest("File upload failed.");

            return Ok(new { Url = fileUrl });
        }

        [HttpGet("GetMessagesWithUser")]
        public async Task<IActionResult> GetMessagesWithUser([FromQuery] string recipientId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User ID not found.");

            var messages = await _messageService.GetMessagesBetweenUsers(userId, recipientId);
            return Ok(messages);
        }

        [HttpGet("GetGroupMessages")]
        public async Task<IActionResult> GetGroupMessages([FromQuery] Guid groupId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("UserId is not found.");
            }

            var IsMember = await _context.GroupMembers
                .Where(gm => (gm.GroupId == groupId && gm.UserId == userId))
                .FirstOrDefaultAsync() != null;
            if (!IsMember)
            {
                return Unauthorized("You're not a member of the group.");
            }

            var messages = await _messageService.GetMessagesForGroup(groupId);
            return Ok(messages);
        }
    }
}
