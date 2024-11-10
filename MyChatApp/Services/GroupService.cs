using Microsoft.EntityFrameworkCore;
using MyChatApp.Data;
using MyChatApp.Models;

namespace MyChatApp.Services
{
    public class GroupService : IGroupService
    {
        private readonly ChatDbContext _context;
        public GroupService(ChatDbContext context)
        {
            _context = context;
        }

        //Add a user to a group
        public async Task<bool> AddUserToGroup(string UserId, Guid GroupId)
        {
            var group = await _context.Groups
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.GroupId == GroupId);

            if (group == null)
            {
                return false;
            }

            if(! group.GroupMembers.Any(gm => gm.UserId == UserId))
            {
                var groupMember = new GroupMember
                {
                    GroupId = GroupId,
                    UserId = UserId
                };

                _context.GroupMembers.Add(groupMember);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        //Remove a user from a group
        public async Task<bool> RemoveUserFromGroup(string UserId, Guid GroupId)
        {
            var groupMember = await _context.GroupMembers
                .FirstOrDefaultAsync(gm => gm .UserId == UserId && gm.GroupId == GroupId);

            if(groupMember != null)
            {
                _context.GroupMembers.Remove(groupMember);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        //Create a group
        public async Task<Group> CreateGroup(string groupName)
        {
            var group = new Group { GroupId = Guid.NewGuid(), GroupName = groupName };
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
            return group;
        }

        //Fetch all the Group-Members
        public async Task<List<ApplicationUser?>> GetGroupMembers(Guid GroupId)
        {
            return await _context.GroupMembers
                .Where(gm => gm.GroupId == GroupId)
                .Select(gm => gm.User)
                .ToListAsync();
        }

        //Delete a group
        public async Task<bool> DeleteGroup(Guid GroupId)
        {
            var group = await  _context.Groups
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.GroupId == GroupId);

            if(group == null)
            {
                return false;
            }

            _context.GroupMembers.RemoveRange(group.GroupMembers);
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
