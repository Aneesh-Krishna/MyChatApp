using MyChatApp.Models;
namespace MyChatApp.Services
{
    public interface IGroupService
    {
        Task<bool> AddUserToGroup(string UserId, Guid GroupId);
        Task<bool> RemoveUserFromGroup(string UserId, Guid GroupId);
        Task<Group> CreateGroup(string groupName);
        Task<List<ApplicationUser?>> GetGroupMembers(Guid GroupId);
        Task<bool> DeleteGroup(Guid GroupId);
    }
}
