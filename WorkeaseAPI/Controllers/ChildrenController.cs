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
                var children = role == "CDW"
                    ? await _childService.GetChildByCdwUserAsync(userId)
                    : await _childService.GetAllChildAsync();
                return Ok(children);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        [HttpGet("mine")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChild()
        {
            try
            {
                var child = await _childService.GetChildByGuardianUserIdAsync(GetUserId());
                return child is null
                    ? NotFound(new { message = "No child linked to your account yet." })
                    : Ok(child);
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
