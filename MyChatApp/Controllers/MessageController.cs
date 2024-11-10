using Microsoft.AspNetCore.Mvc;
using MyChatApp.Services;
using MyChatApp.Models;
using MyChatApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace MyChatApp.Controllers
{
    [ApiController]
    [Authorize(Policy = "CanDeleteMessage")]
    [Route("api/[controller]")]
    public class MessageController : Controller
    {
        private readonly IMessageService _messageService;
        private readonly FileService _fileService;
        private readonly IHubContext<ChatHub> _chatHub;

        public MessageController(IMessageService messageService, FileService fileService, IHubContext<ChatHub> chatHub)
        {
            _messageService = messageService;
            _fileService = fileService;
            _chatHub = chatHub;
        }

        [HttpPost("SendToUser")]
        public async Task<IActionResult> SendPrivateMessage([FromBody] Message message, IFormFile? file = null)
        {
            string? fileUrl = null;
            if (file != null)
            {
                fileUrl = await _fileService.UploadFileAsync(file);
                if (fileUrl == null) return BadRequest("File upload failed");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User ID not found.");

            var msg = await _messageService.SendMessageToUser(userId, message.RecipientId, message.Content, fileUrl);
            if (msg == null) return BadRequest("Failed to send message");

            await _chatHub.Clients.User(message.RecipientId).SendAsync("ReceiveMessage", msg);

            return Ok(msg);
        }

        [HttpPost("SendToGroup")]
        public async Task<IActionResult> SendGroupMessage([FromBody] Message message, IFormFile? file = null)
        {
            if (message.GroupId == null) return BadRequest("No group selected.");

            string? fileUrl = null;
            if (file != null)
            {
                fileUrl = await _fileService.UploadFileAsync(file);
                if (fileUrl == null) return BadRequest("File upload failed");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User ID not found.");

            var msg = await _messageService.SendMessageToGroup(userId, message.GroupId.Value, message.Content, fileUrl);
            if (msg == null) return BadRequest("Failed to send message");

            await _chatHub.Clients.Group(message.GroupId.ToString()).SendAsync("ReceiveGroupMessage", msg);

            return Ok(msg);
        }

        [HttpDelete("{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User ID not found.");

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
        public async Task<IActionResult> GetMessagesWithUser(string recipientId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("User ID not found.");

            var messages = await _messageService.GetMessagesBetweenUsers(userId, recipientId);
            return Ok(messages);
        }

        [HttpGet("GetGroupMessages")]
        public async Task<IActionResult> GetGroupMessages(Guid groupId)
        {
            var messages = await _messageService.GetMessagesForGroup(groupId);
            return Ok(messages);
        }
    }
}
