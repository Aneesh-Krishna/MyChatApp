using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyChatApp.Data;
using MyChatApp.Models;
using MyChatApp.Services;
using System.Security.Claims;

namespace MyChatApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;
        private readonly ChatDbContext _context;

        public GroupController(IGroupService groupService, ChatDbContext context)
        {
            _groupService = groupService;
            _context = context;
        }

        [HttpPost("AddUserToGroup")]
        public async Task<IActionResult> AddUserToGroup([FromQuery] Guid groupId, [FromBody] AddUserToGroupRequest request)
        {
            if(string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest("UserId is required!");
            }

            var success = await _groupService.AddUserToGroup(request.UserId, groupId);
            return success ? Ok("User added to group") : BadRequest("Failed to add the user");
        }

        [HttpPost("RemoveUserFromGroup")]
        public async Task<IActionResult> RemoveUserFromGroup([FromQuery] Guid groupId, [FromBody] AddUserToGroupRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId))
            {
                return BadRequest("UserId is required!");
            }

            var success = await _groupService.RemoveUserFromGroup(request.UserId, groupId);
            return success ? Ok("User removed from group") : BadRequest("Failed to remove the user");
        }

        [HttpPost("CreateGroup")]
        public async Task<IActionResult> CreateGroup([FromBody] Group group)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var grp = await _groupService.CreateGroup(group.GroupName);
            return Ok(grp);
        }

        [HttpGet("GetGroupMembers")]
        public async Task<IActionResult> GetGroupMembers([FromQuery] Guid groupId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(userId == null)
            {
                return Unauthorized("UserId is not found.");
            }

            var IsMember = await _context.GroupMembers
                .Where(gm => (gm.GroupId == groupId && gm.UserId == userId))
                .FirstOrDefaultAsync() != null;
            if(!IsMember)
            {
                return Unauthorized("You're not a member of the group.");
            }

            var groupMembers = await _groupService.GetGroupMembers(groupId);
            return Ok(groupMembers);
        }

        [HttpGet("GetGroups")]
        public async Task<IActionResult> GetGroups()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User Id not found.");

            var hasGroups = await _context.GroupMembers
                .Where(gm => (gm.UserId == userId))
                .FirstOrDefaultAsync() != null;
            if (!hasGroups)
                return BadRequest("No groups found.");

            var groups = await _groupService.GetGroups(userId);
            return Ok(groups);
        }

        [HttpDelete("{groupId}")]
        public async Task<IActionResult> DeleteGroup([FromQuery] Guid groupId)
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

            var result = await _groupService.DeleteGroup(groupId);
            return result ? Ok("Group deleted.") : NotFound("Group not found.");
        }
    }

    public class AddUserToGroupRequest
    {
        public string UserId { get; set; }
    }
}
