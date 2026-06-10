using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkeaseAPI.DTOs;
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

        [HttpGet]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var role = GetUserType();
                var userId = GetUserId();

                if (role == "CDW")
                {
                    var centerId = await _childService.GetCenterIdByUserAsync(userId);

                    if (centerId is null)
                        return Ok(Enumerable.Empty<ChildSummaryDto>());

                    var children = await _childService
                                         .GetChildrenByCenterAsync(centerId.Value);
                    return Ok(children);
                }

                // Admin sees all
                return Ok(await _childService.GetAllChildAsync());
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // GET api/children/mine
        // ✅ Returns ALL children of parent — not just one
        [HttpGet("mine")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChildren()
        {
            try
            {
                var parentUserId = GetUserId();
                var children = await _childService
                                         .GetChildByGuardianUserIdAsync(parentUserId);

                return Ok(children);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // Controllers/ChildrenController.cs

        // PUT api/children/{childId}/link-parent/{parentId}
        // Admin links a child to a parent account
        [HttpPut("{childId}/link-parent/{parentId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> LinkParent(int childId, int parentId)
        {
            try
            {
                var result = await _childService.LinkParentAsync(childId, parentId);
                return Ok(new
                {
                    message = "Child linked to parent successfully.",
                    childId = childId,
                    parentId = parentId
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // PUT api/children/{childId}/unlink-parent
        // Admin removes the parent link from a child
        [HttpPut("{childId}/unlink-parent")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> UnlinkParent(int childId)
        {
            try
            {
                var result = await _childService.UnlinkParentAsync(childId);
                return result
                    ? Ok(new { message = "Parent unlinked from child successfully." })
                    : NotFound(new { message = "Child not found." });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var child = await _childService.GetChildByIdAsync(id);
                return child is null ? NotFound() : Ok(child);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create(CreateChildDto dto)
        {
            try
            {
                // ✅ Pass admin userId so fee is recorded under admin
                var created = await _childService.CreateChildWithGuardianAsync(dto, GetUserId());
                return CreatedAtAction(nameof(GetById),
                    new { id = created.ChildId }, created);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, UpdateChildDto dto)  // ✅ DTO
        {
            try
            {
                var result = await _childService.UpdateChildAsync(id, dto);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _childService.DeleteChildAsync(id);
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string GetUserType() => User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}
