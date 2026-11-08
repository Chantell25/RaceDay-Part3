using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Authorize(Roles = Roles.Organiser), Route("api/organiser")]
public class OrganiserController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet("events")]
    public async Task<IActionResult> MyEvents()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var organiser = await db.OrganiserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId);
        if (organiser is null) return Forbid();
        var events = await db.Events.AsNoTracking().Where(e => e.OrganiserId == organiser.OrganiserId)
            .OrderBy(e => e.EventDate)
            .Select(e => new { e.EventId, e.EventName, e.EventDate, e.City, e.Status,
                CategoryCount = e.Categories.Count,
                EnrolmentCount = e.Enrollments.Count(x => x.Status != "Cancelled"),
                ResultCount = e.Enrollments.Count(x => x.Result != null) })
            .ToListAsync();
        return Ok(events);
    }

    [HttpGet("events/{id:int}/summary")]
    public async Task<IActionResult> Summary(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var e = await db.Events.AsNoTracking().Where(x => x.EventId == id && x.Organiser!.UserId == userId)
            .Select(x => new { x.EventId, x.EventName, x.EventDate, x.City, x.Status,
                Categories = x.Categories.Select(c => new { c.CategoryId, c.CategoryName, c.DistanceKm, c.MaxParticipants,
                    Enrolments = c.Enrollments.Count(en => en.Status != "Cancelled"),
                    Results = c.Enrollments.Count(en => en.Result != null) }),
                TotalEnrolments = x.Enrollments.Count(en => en.Status != "Cancelled"),
                TotalResults = x.Enrollments.Count(en => en.Result != null) })
            .SingleOrDefaultAsync();
        return e is null ? NotFound(new { message = "Event not found or not owned by this organiser." }) : Ok(e);
    }
}
