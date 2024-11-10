using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyChatApp.Services;

namespace MyChatApp.Controllers
{
    [ApiController]
    [Authorize(Policy = "GroupAdminOnly")]
    [Route("api/[controller]")]
    public class GroupController : ControllerBase
    {
        private readonly IGroupService _groupService;

        public GroupController(IGroupService groupService)
        {
            _groupService = groupService;
        }

        [HttpPost("AddUserToGroup")]
        public async Task<IActionResult> AddUserToGroup(Guid groupId, [FromBody] string userId)
        {
            var success = await _groupService.AddUserToGroup(userId, groupId);
            return success ? Ok("User added to group") : BadRequest("Failed to add the user");
        }

        [HttpPost("RemoveUserFromGroup")]
        public async Task<IActionResult> RemoveUserFromGroup(Guid groupId, [FromBody] string userId)
        {
            var success = await _groupService.RemoveUserFromGroup(userId, groupId);
            return success ? Ok("User removed from group") : BadRequest("Failed to remove the user");
        }

        [HttpPost("CreateGroup")]
        public async Task<IActionResult> CreateGroup(string groupName)
        {
            var group = await _groupService.CreateGroup(groupName);
            return Ok(group);
        }

        [HttpGet("GetGroupMembers")]
        public async Task<IActionResult> GetGroupMembers(Guid groupId)
        {
            var groupMembers = await _groupService.GetGroupMembers(groupId);
            return Ok(groupMembers);
        }

        [HttpDelete("{groupId}")]
        public async Task<IActionResult> DeleteGroup(Guid groupId)
        {
            var result = await _groupService.DeleteGroup(groupId);
            return result ? Ok("Group deleted.") : NotFound("Group not found.");
        }
    }
}
