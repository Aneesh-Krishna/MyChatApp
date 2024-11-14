using MyChatApp.Models;
namespace MyChatApp.Services
{
    public interface IGroupService
    {
        Task<bool> AddUserToGroup(string UserId, Guid GroupId);
        Task<bool> RemoveUserFromGroup(string UserId, Guid GroupId);
        Task<Group> CreateGroup(string groupName);
        Task<List<ApplicationUser?>> GetGroupMembers(Guid GroupId);
        Task<List<Group?>> GetGroups(string UserId);
        Task<bool> DeleteGroup(Guid GroupId);
    }
}
