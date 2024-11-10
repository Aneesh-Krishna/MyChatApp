using Microsoft.EntityFrameworkCore;
using MyChatApp.Data;
using MyChatApp.Models;

namespace MyChatApp.Services
{
    public class MessageService : IMessageService
    {
        private readonly ChatDbContext _context;
        public MessageService(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<Message> SendMessageToUser(string SenderId, string RecipientId, string Content, string? FileUrl = null)
        {
            var message = new Message
            {
                MessageId = Guid.NewGuid(),
                SenderId = SenderId,
                RecipientId = RecipientId,
                Content = Content,
                FileUrl = FileUrl,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }

        public async Task<List<Message>> SendMessageToGroup(string SenderId, Guid GroupId, string Content, string? FileUrl = null)
        {
            bool IsMember = await _context.GroupMembers.AnyAsync(gm => gm.GroupId == GroupId && gm.UserId == SenderId);
            if(!IsMember)
            {
                throw new UnauthorizedAccessException("Sender is not a member of the group");
            }

            var groupMembes = await _context.GroupMembers
                .Where(gm => gm.GroupId == GroupId)
                .Select(gm => gm.UserId)
                .ToListAsync();

            var messages = new List<Message>();

            foreach(var memberId in groupMembes)
            {
                var message = new Message
                {
                    MessageId = Guid.NewGuid(),
                    SenderId = SenderId,
                    RecipientId = memberId,
                    GroupId = GroupId,
                    Content = Content,
                    FileUrl = FileUrl,
                    SentAt = DateTime.UtcNow
                };
                _context.Messages.Add(message);
                messages.Add(message);
            }
            await _context.SaveChangesAsync();
            return messages;
        }

        public async Task<bool> DeleteMessage(string UserId, Guid MessageId)
        {
            var message = await _context.Messages.FindAsync(MessageId);
            if(message == null || message.SenderId != UserId)
            {
                return false;
            }

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Message>> GetMessagesBetweenUsers(string UserId1, string UserId2)
        {
            return await _context.Messages
                .Where(m => (m.SenderId == UserId1 && m.RecipientId == UserId2) ||
                            (m.SenderId == UserId2 && m.RecipientId == UserId1))
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<List<Message>> GetMessagesForGroup(Guid GroupId)
        {
            return await _context.Messages
                .Where(m => m.GroupId == GroupId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();
        }
    }
}
