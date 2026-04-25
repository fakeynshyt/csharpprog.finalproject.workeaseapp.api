using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Models;

namespace WorkeaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChildrenController : ControllerBase
    {
        private readonly IChildService _childService;

        public ChildrenController(IChildService childService)
            => _childService = childService;

        // GET api/children
        // Admin: all children. CDW: own center only.
        [HttpGet]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetAllChildren()
        {
            var role = GetUserType();   // ✅ returns "Admin" or "CDW"
            var userId = GetUserId();     // ✅ returns int

            var children = role == "CDW"
                ? await _childService.GetAllChildByCdwUserAsync(userId)
                : await _childService.GetAllChildrenAsync();

            return Ok(children);
        }

        // GET api/children/mine
        // Parent only — their child with full health and fee history
        [HttpGet("mine")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChild()
        {
            var parentUserId = GetUserId();
            var child = await _childService.GetGuardianChildByIdAsync(parentUserId);

            return child is null
                ? NotFound(new { message = "No child is linked to your account yet." })
                : Ok(child);
        }

        // GET api/children/{id}
        // Admin / CDW — get single child by ID
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetById(int id)
        {
            var child = await _childService.GetChildByIdAsync(id);
            return child is null ? NotFound() : Ok(child);
        }

        // PUT api/children/{id}
        // Admin only — update child info
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, Child child)
        {
            var result = await _childService.UpdateChildAsync(id, child);
            return result ? NoContent() : NotFound();
        }

        // PUT api/children/{id}/link-parent/{parentUserId}
        // Admin only — link parent to child separately
        [HttpPut("{id}/link-parent/{parentUserId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> LinkParent(int id, int parentUserId)
        {
            try
            {
                var result = await _childService.LinkParentAsync(id, parentUserId);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE api/children/{id}
        // Admin only — soft delete
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id) =>
            await _childService.DeleteChildAsync(id) ? NoContent() : NotFound();

        // ── Helpers ───────────────────────────────────────────────
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        private string GetUserType() =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
