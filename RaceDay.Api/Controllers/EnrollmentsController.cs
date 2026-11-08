using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Authorize, Route("api")]
public class EnrollmentsController(RaceDayDbContext db) : ControllerBase
{
    [Authorize(Roles = Roles.Participant), HttpPost("enrolments/{eventId:int}")]
    public async Task<IActionResult> Enrol(int eventId, EnrollmentRequest request)
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var participant = await db.ParticipantProfiles.SingleAsync(x => x.UserId == uid);
        var category = await db.Categories.SingleOrDefaultAsync(x => x.EventId == eventId && x.CategoryId == request.CategoryId);
        if (category is null) return BadRequest(new { message = "The selected category does not belong to this event." });
        if (await db.Enrollments.AnyAsync(x => x.EventId == eventId && x.ParticipantId == participant.ParticipantId))
            return Conflict(new { message = "Participant is already enrolled in this event." });
        int count = await db.Enrollments.CountAsync(x => x.CategoryId == category.CategoryId && x.Status != "Cancelled");
        if (count >= category.MaxParticipants) return Conflict(new { message = "Category is full." });
        int raceNo;
        do { raceNo = Random.Shared.Next(10000, 999999); } while (await db.Enrollments.AnyAsync(x => x.RaceNumber == raceNo));
        var enrollment = new Enrollment { EventId = eventId, CategoryId = category.CategoryId,
            ParticipantId = participant.ParticipantId, RaceNumber = raceNo, EnrollmentDate = DateTime.UtcNow, Status = "Confirmed" };
        db.Enrollments.Add(enrollment); await db.SaveChangesAsync();
        return Created($"/api/enrolments/mine", new { enrollment.EnrollmentId, enrollment.EventId, enrollment.CategoryId, enrollment.RaceNumber, enrollment.Status });
    }

    [Authorize(Roles = Roles.Participant), HttpGet("enrolments/mine")]
    public async Task<IActionResult> Mine()
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var participant = await db.ParticipantProfiles.SingleAsync(x => x.UserId == uid);
        return Ok(await db.Enrollments.AsNoTracking().Include(x => x.Event).Include(x => x.Category)
            .Where(x => x.ParticipantId == participant.ParticipantId)
            .Select(x => new { x.EnrollmentId, x.RaceNumber, x.Status, x.EnrollmentDate, EventName = x.Event!.EventName,
                x.Event!.EventDate, CategoryName = x.Category!.CategoryName }).ToListAsync());
    }

    [Authorize(Roles = Roles.Organiser), HttpGet("events/{eventId:int}/enrolments")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var e = await db.Events.Include(x => x.Organiser).ThenInclude(x => x!.UserAccount).SingleOrDefaultAsync(x => x.EventId == eventId);
        if (e is null) return NotFound();
        if (e.Organiser?.UserId != uid) return Forbid();
        return Ok(await db.Enrollments.AsNoTracking().Include(x => x.Participant).ThenInclude(x => x!.UserAccount)
            .Include(x => x.Category).Where(x => x.EventId == eventId)
            .Select(x => new { x.EnrollmentId, x.RaceNumber, x.Status, Category = x.Category!.CategoryName,
                Participant = x.Participant!.UserAccount!.FirstName + " " + x.Participant.UserAccount.LastName,
                x.Participant.UserAccount.Email, ResultId = x.Result == null ? (int?)null : x.Result.ResultId,
                FinishTime = x.Result == null ? null : x.Result.FinishTime,
                Position = x.Result == null ? (int?)null : x.Result.Position,
                ResultStatus = x.Result == null ? null : x.Result.ResultStatus }).ToListAsync());
    }

    [Authorize(Roles = Roles.Participant), HttpDelete("enrolments/{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var p = await db.ParticipantProfiles.SingleAsync(x => x.UserId == uid);
        var e = await db.Enrollments.SingleOrDefaultAsync(x => x.EnrollmentId == id && x.ParticipantId == p.ParticipantId);
        if (e is null) return NotFound();
        e.Status = "Cancelled"; await db.SaveChangesAsync(); return NoContent();
    }
}
