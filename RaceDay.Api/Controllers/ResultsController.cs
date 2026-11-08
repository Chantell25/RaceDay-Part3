using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api")]
public class ResultsController(RaceDayDbContext db) : ControllerBase
{
    [Authorize(Roles = Roles.Organiser), HttpPost("events/{eventId:int}/results/{enrollmentId:int}")]
    public async Task<IActionResult> Create(int eventId, int enrollmentId, ResultRequest request)
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var enrol = await db.Enrollments.Include(x => x.Event).ThenInclude(x => x!.Organiser).ThenInclude(x => x!.UserAccount)
            .SingleOrDefaultAsync(x => x.EnrollmentId == enrollmentId && x.EventId == eventId);
        if (enrol is null) return NotFound();
        if (enrol.Event?.Organiser?.UserId != uid) return Forbid();
        if (request.Position is <= 0) return BadRequest(new { message = "Position must be greater than zero." });
        if (await db.Results.AnyAsync(x => x.EnrollmentId == enrollmentId)) return Conflict(new { message = "A result already exists for this enrolment." });
        var r = new Result { EnrollmentId = enrollmentId, FinishTime = request.FinishTime, Position = request.Position,
            ResultStatus = request.ResultStatus, RecordedAt = DateTime.UtcNow };
        db.Results.Add(r); await db.SaveChangesAsync(); return Created($"/api/results/mine", r);
    }

    [Authorize(Roles = Roles.Organiser), HttpPut("results/{id:int}")]
    public async Task<IActionResult> Update(int id, ResultRequest request)
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await db.Results.Include(x => x.Enrollment).ThenInclude(x => x!.Event).ThenInclude(x => x!.Organiser).ThenInclude(x => x!.UserAccount).SingleOrDefaultAsync(x => x.ResultId == id);
        if (r is null) return NotFound();
        if (r.Enrollment?.Event?.Organiser?.UserId != uid) return Forbid();
        if (request.Position is <= 0) return BadRequest();
        r.FinishTime = request.FinishTime; r.Position = request.Position; r.ResultStatus = request.ResultStatus; r.RecordedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return Ok(r);
    }

    [Authorize(Roles = Roles.Organiser), HttpDelete("results/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var r = await db.Results.Include(x => x.Enrollment).ThenInclude(x => x!.Event).ThenInclude(x => x!.Organiser).ThenInclude(x => x!.UserAccount).SingleOrDefaultAsync(x => x.ResultId == id);
        if (r is null) return NotFound();
        if (r.Enrollment?.Event?.Organiser?.UserId != uid) return Forbid();
        db.Results.Remove(r); await db.SaveChangesAsync(); return NoContent();
    }

    [Authorize(Roles = Roles.Participant), HttpGet("results/mine")]
    public async Task<IActionResult> Mine()
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var p = await db.ParticipantProfiles.SingleAsync(x => x.UserId == uid);
        return Ok(await db.Results.AsNoTracking().Include(x => x.Enrollment).ThenInclude(x => x!.Event)
            .Include(x => x.Enrollment).ThenInclude(x => x!.Category)
            .Where(x => x.Enrollment!.ParticipantId == p.ParticipantId)
            .Select(x => new { x.ResultId, EventName = x.Enrollment!.Event!.EventName, Category = x.Enrollment.Category!.CategoryName,
                x.FinishTime, x.Position, x.ResultStatus, x.RecordedAt }).ToListAsync());
    }
}
