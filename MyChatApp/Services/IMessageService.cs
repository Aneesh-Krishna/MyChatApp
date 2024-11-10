
using MyChatApp.Models;

namespace MyChatApp.Services
{
    public interface IMessageService
    {
        Task<Message> SendMessageToUser(string SenderId, string RecipientId, string Content, string? FileUrl = null);
        Task<List<Message>> SendMessageToGroup(string SenderId, Guid GroupId, string Content, string? FileUrl = null);
        Task<bool> DeleteMessage(string UserId, Guid MessageId);
        Task<List<Message>> GetMessagesBetweenUsers(string UserId1, string UserId2);
        Task<List<Message>> GetMessagesForGroup(Guid GroupId);
    }
}
