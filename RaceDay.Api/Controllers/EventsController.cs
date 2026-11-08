using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/events")]
public class EventsController(RaceDayDbContext db) : ControllerBase
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> List([FromQuery] string? city, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var q = db.Events.AsNoTracking().Include(e => e.Categories).AsQueryable();
        if (!string.IsNullOrWhiteSpace(city)) q = q.Where(e => e.City.Contains(city));
        if (fromDate.HasValue) q = q.Where(e => e.EventDate >= fromDate.Value);
        if (toDate.HasValue) q = q.Where(e => e.EventDate <= toDate.Value);
        return Ok(await q.OrderBy(e => e.EventDate).Select(e => new {
            e.EventId, e.EventName, e.Description, e.EventDate, e.Venue, e.City, e.Status, e.EventType, e.BannerUrl,
            Categories = e.Categories.Select(c => new { c.CategoryId, c.CategoryName, c.DistanceKm, c.EntryFee, c.MaxParticipants })
        }).ToListAsync());
    }

    [AllowAnonymous, HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var e = await db.Events.AsNoTracking().Include(x => x.Categories).Where(x => x.EventId == id)
            .Select(x => new { x.EventId, x.EventName, x.Description, x.EventDate, x.Venue, x.City, x.Status, x.EventType, x.BannerUrl,
                Categories = x.Categories.Select(c => new { c.CategoryId, c.CategoryName, c.DistanceKm, c.MaxParticipants, c.EntryFee }) }).SingleOrDefaultAsync();
        return e is null ? NotFound() : Ok(e);
    }

    [Authorize(Roles = Roles.Organiser), HttpPost]
    public async Task<IActionResult> Create(EventRequest request)
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var organiser = await db.OrganiserProfiles.SingleAsync(x => x.UserId == uid);
        var e = new RaceEvent { OrganiserId = organiser.OrganiserId, EventName = request.EventName.Trim(),
            Description = request.Description, EventDate = request.EventDate, Venue = request.Venue.Trim(),
            City = request.City.Trim(), Status = request.Status, EventType = request.EventType.Trim() };
        db.Events.Add(e); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = e.EventId }, e);
    }

    [Authorize(Roles = Roles.Organiser), HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var e = await OwnedEvent(id); if (e is null) return NotFound(new { message = "Event not found or not owned by this organiser." });
        e.EventName = request.EventName.Trim(); e.Description = request.Description; e.EventDate = request.EventDate;
        e.Venue = request.Venue.Trim(); e.City = request.City.Trim(); e.Status = request.Status; e.EventType = request.EventType.Trim();
        await db.SaveChangesAsync(); return Ok(e);
    }

    [Authorize(Roles = Roles.Organiser), HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await OwnedEvent(id); if (e is null) return NotFound(new { message = "Event not found or not owned by this organiser." });
        if (await db.Enrollments.AnyAsync(x => x.EventId == id))
            return Conflict(new { message = "Cannot delete an event that already has enrolments." });
        db.Events.Remove(e); await db.SaveChangesAsync(); return NoContent();
    }

    [AllowAnonymous, HttpGet("{eventId:int}/categories")]
    public async Task<IActionResult> Categories(int eventId)
    {
        if (!await db.Events.AnyAsync(x => x.EventId == eventId)) return NotFound();
        return Ok(await db.Categories.AsNoTracking().Where(x => x.EventId == eventId).ToListAsync());
    }

    [Authorize(Roles = Roles.Organiser), HttpPost("{eventId:int}/categories")]
    public async Task<IActionResult> AddCategory(int eventId, CategoryRequest request)
    {
        var e = await OwnedEvent(eventId); if (e is null) return NotFound(new { message = "Event not found or not owned by this organiser." });
        if (request.DistanceKm <= 0 || request.MaxParticipants <= 0 || request.EntryFee < 0)
            return BadRequest(new { message = "Distance and capacity must be positive; fee cannot be negative." });
        if (await db.Categories.AnyAsync(x => x.EventId == eventId && x.CategoryName == request.CategoryName))
            return Conflict(new { message = "This category name already exists for the event." });
        var c = new Category { EventId = eventId, CategoryName = request.CategoryName.Trim(), DistanceKm = request.DistanceKm,
            MaxParticipants = request.MaxParticipants, EntryFee = request.EntryFee };
        db.Categories.Add(c); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Categories), new { eventId }, c);
    }

    [Authorize(Roles = Roles.Organiser), HttpPut("/api/categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var c = await db.Categories.Include(x => x.Event).ThenInclude(x => x!.Organiser).ThenInclude(x => x!.UserAccount)
            .SingleOrDefaultAsync(x => x.CategoryId == id);
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (c is null || c.Event?.Organiser?.UserId != uid) return NotFound(new { message = "Category not found or not owned by this organiser." });
        if (request.DistanceKm <= 0 || request.MaxParticipants <= 0 || request.EntryFee < 0) return BadRequest();
        c.CategoryName = request.CategoryName.Trim(); c.DistanceKm = request.DistanceKm; c.MaxParticipants = request.MaxParticipants; c.EntryFee = request.EntryFee;
        await db.SaveChangesAsync(); return Ok(c);
    }

    [Authorize(Roles = Roles.Organiser), HttpDelete("/api/categories/{id:int}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var c = await db.Categories.Include(x => x.Event).ThenInclude(x => x!.Organiser).ThenInclude(x => x!.UserAccount).SingleOrDefaultAsync(x => x.CategoryId == id);
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (c is null || c.Event?.Organiser?.UserId != uid) return NotFound();
        if (await db.Enrollments.AnyAsync(x => x.CategoryId == id)) return Conflict(new { message = "Category has enrolments and cannot be deleted." });
        db.Categories.Remove(c); await db.SaveChangesAsync(); return NoContent();
    }

    private async Task<RaceEvent?> OwnedEvent(int id)
    {
        int uid = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await db.Events.Include(x => x.Organiser).ThenInclude(x => x!.UserAccount)
            .SingleOrDefaultAsync(x => x.EventId == id && x.Organiser!.UserId == uid);
    }
}
