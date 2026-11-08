using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
namespace RaceDay.Api.Controllers;
[ApiController,Authorize(Roles=Roles.Organiser),Route("api/events")]
public class EventBannersController(RaceDayDbContext db,BlobStorageService blobs):ControllerBase
{
    [HttpPost("{eventId:int}/banner")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> Upload(int eventId,IFormFile file,CancellationToken ct)
    {
        if(file is null||file.Length==0)return BadRequest(new{message="Select an image to upload."});
        if(file.Length>5_000_000)return BadRequest(new{message="Image must be 5 MB or smaller."});
        var allowed=new[]{"image/jpeg","image/png","image/webp"};
        if(!allowed.Contains(file.ContentType,StringComparer.OrdinalIgnoreCase))return BadRequest(new{message="Only JPG, PNG and WEBP images are allowed."});
        int userId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var e=await db.Events.Include(x=>x.Organiser).SingleOrDefaultAsync(x=>x.EventId==eventId,ct);
        if(e is null)return NotFound();
        if(e.Organiser?.UserId!=userId)return Forbid();
        try
        {
            var ext=file.ContentType.ToLowerInvariant() switch{"image/png"=>".png","image/webp"=>".webp",_=>".jpg"};
            e.BannerUrl=await blobs.UploadAsync(file,$"events/{eventId}/{Guid.NewGuid():N}{ext}",ct);
            await db.SaveChangesAsync(ct);
            return Ok(new{e.EventId,e.BannerUrl});
        }
        catch(InvalidOperationException ex){return StatusCode(503,new{message=ex.Message});}
    }
}
