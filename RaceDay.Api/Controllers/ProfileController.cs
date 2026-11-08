using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

[ApiController, Authorize, Route("api/profile")]
public class ProfileController(RaceDayDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.UserAccounts.Include(x => x.OrganiserProfile).Include(x => x.ParticipantProfile)
            .Where(x => x.UserId == id)
            .Select(x => new { x.UserId, x.FirstName, x.LastName, x.Email, x.Role,
                Organiser = x.OrganiserProfile == null ? null : x.OrganiserProfile.OrganisationName,
                ParticipantDateOfBirth = x.ParticipantProfile == null ? null : x.ParticipantProfile.DateOfBirth,
                ProfilePictureUrl = x.ParticipantProfile == null ? null : x.ParticipantProfile.ProfilePictureUrl })
            .SingleOrDefaultAsync();
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut]
    public async Task<IActionResult> Update(ProfileUpdateRequest request)
    {
        int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.UserAccounts.FindAsync(id);
        if (user is null) return NotFound();
        user.FirstName = request.FirstName.Trim(); user.LastName = request.LastName.Trim();
        await db.SaveChangesAsync();
        return Ok(new { user.UserId, user.FirstName, user.LastName, user.Email, user.Role });
    }

    [HttpPost("picture")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadPicture(IFormFile file,[FromServices] RaceDay.Api.Services.BlobStorageService blobs,CancellationToken ct)
    {
        if(file is null||file.Length==0)return BadRequest(new{message="Select a profile image."});
        if(file.Length>5_000_000)return BadRequest(new{message="Image must be 5 MB or smaller."});
        if(!new[]{"image/jpeg","image/png","image/webp"}.Contains(file.ContentType,StringComparer.OrdinalIgnoreCase))
            return BadRequest(new{message="Only JPG, PNG and WEBP images are allowed."});
        int id=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile=await db.ParticipantProfiles.SingleOrDefaultAsync(x=>x.UserId==id,ct);
        if(profile is null)return BadRequest(new{message="Profile pictures are available for participant accounts."});
        try
        {
            var ext=file.ContentType.ToLowerInvariant() switch{"image/png"=>".png","image/webp"=>".webp",_=>".jpg"};
            profile.ProfilePictureUrl=await blobs.UploadAsync(file,$"profiles/{id}/{Guid.NewGuid():N}{ext}",ct);
            await db.SaveChangesAsync(ct);
            return Ok(new{profile.ProfilePictureUrl});
        }
        catch(InvalidOperationException ex){return StatusCode(503,new{message=ex.Message});}
    }
}
