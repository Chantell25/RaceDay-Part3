using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(RaceDayDbContext db, PasswordService passwords) : ControllerBase
{
    [AllowAnonymous, HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var role = request.Role?.Trim();
        if (role is not (Roles.Organiser or Roles.Participant))
            return BadRequest(new { message = "Role must be Organiser or Participant." });
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.UserAccounts.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "An account with this email already exists." });

        if (role == Roles.Organiser && (string.IsNullOrWhiteSpace(request.OrganisationName) || string.IsNullOrWhiteSpace(request.ContactNumber)))
            return BadRequest(new { message = "Organiser accounts require organisation name and contact number." });

        var user = new UserAccount { FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            Email = email, PasswordHash = passwords.Hash(request.Password), Role = role, CreatedAt = DateTime.UtcNow, IsActive = true };
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync();

        if (role == Roles.Organiser)
            db.OrganiserProfiles.Add(new OrganiserProfile { UserId = user.UserId, OrganisationName = request.OrganisationName!.Trim(), ContactNumber = request.ContactNumber!.Trim() });
        else
            db.ParticipantProfiles.Add(new ParticipantProfile { UserId = user.UserId, DateOfBirth = request.DateOfBirth,
                EmergencyContactName = request.EmergencyContactName, EmergencyContactNumber = request.EmergencyContactNumber });
        await db.SaveChangesAsync();
        return Created("api/auth/me", new { user.UserId, user.Email, user.Role });
    }

    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.UserAccounts.SingleOrDefaultAsync(x => x.Email == email && x.IsActive);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"), new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
        return Ok(new { user.UserId, user.Email, user.Role, message = "Login successful." });
    }

    [Authorize, HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        int id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await db.UserAccounts.AsNoTracking().Where(x => x.UserId == id)
            .Select(x => new { x.UserId, x.FirstName, x.LastName, x.Email, x.Role }).SingleAsync();
        return Ok(u);
    }

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
