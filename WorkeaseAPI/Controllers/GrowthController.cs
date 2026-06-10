// Controllers/GrowthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WorkeaseAPI.Data;
using WorkeaseAPI.DTOs;
using WorkeaseAPI.Interfaces;
using WorkeaseAPI.Services;

namespace WorkeaseAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GrowthController : ControllerBase
    {
        private readonly IGrowthService _growthService;
        private readonly AppDbContext _db;

        public GrowthController(IGrowthService growthService, AppDbContext db)
        {
            _growthService = growthService;
            _db = db;
        }

        // GET api/growth/myChild
        // Parent — get their child's growth tracking
        [HttpGet("myChild")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChildGrowth()
        {
            try
            {
                var growth = await _growthService
                                   .GetByParentUserIdAsync(GetUserId());

                return growth is null
                    ? NotFound(new { message = "No child linked to your account." })
                    : Ok(growth);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // GET api/growth/myChildren
        // ✅ Returns growth for ALL children of parent
        [HttpGet("myChildren")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> GetMyChildrenGrowth()
        {
            try
            {
                var growths = await _growthService
                                   .GetAllByParentUserIdAsync(GetUserId());
                return Ok(growths);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // GET api/growth/{childId}
        // Admin / CDW — view any child's growth
        [HttpGet("{childId}")]
        [Authorize(Policy = "AdminAndCDW")]
        public async Task<IActionResult> GetByChildId(int childId)
        {
            try
            {
                // Ensure record exists
                var growth = await _growthService.EnsureGrowthExistsAsync(childId);
                return Ok(growth);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // PUT api/growth/child/{childId}
        // ✅ Update specific child's growth — parent must own this child
        [HttpPut("child/{childId}")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> UpdateChildGrowth(int childId, UpdateGrowthDto dto)
        {
            try
            {
                var parentUserId = GetUserId();

                // ✅ Verify this child belongs to this parent
                var child = await _db.Children
                                     .FirstOrDefaultAsync(c => c.ChildId == childId
                                                            && c.GuardianId == parentUserId
                                                            && c.ChildIsActive == true);
                if (child is null)
                    return Forbid();

                var updated = await _growthService.UpdateGrowthAsync(childId, dto);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // PUT api/growth/myChild
        // Parent — update their child's growth allocations
        [HttpPut("myChild")]
        [Authorize(Policy = "ParentOnly")]
        public async Task<IActionResult> UpdateMyChildGrowth(UpdateGrowthDto dto)
        {
            try
            {
                var parentUserId = GetUserId();

                // Get child linked to this parent
                var growth = await _growthService
                                   .GetByParentUserIdAsync(parentUserId);

                if (growth is null)
                    return NotFound(new
                    {
                        message = "No child linked to your account."
                    });

                var updated = await _growthService
                                    .UpdateGrowthAsync(growth.ChildId, dto);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        // POST api/growth/recalculate/{childId}
        // Admin — manually recalculate points for a child
        [HttpPost("recalculate/{childId}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RecalculatePoints(int childId)
        {
            try
            {
                await _growthService.RecalculatePointsAsync(childId);
                var growth = await _growthService.GetByChildIdAsync(childId);
                return Ok(growth);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { message = inner });
            }
        }

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string GetUserType() =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }
}