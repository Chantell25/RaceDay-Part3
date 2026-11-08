using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDay"), sql => sql.EnableRetryOnFailure(3)));
builder.Services.AddScoped<PasswordService>();
builder.Services.AddSingleton<BlobStorageService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.Name = "RaceDay.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; };
    options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "RaceDay REST API", Version = "v1", Description = "Part 2 API built to align with the Part 1 RaceDayDB schema." });
    options.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Name = "RaceDay.Auth", Type = SecuritySchemeType.ApiKey, In = ParameterLocation.Cookie,
        Description = "Log in using POST /api/auth/login; the HTTP-only cookie authenticates protected endpoints."
    });
});
var app = builder.Build();

// Create the database and tables on first run if they do not exist yet (no manual SQL script needed).
// If RaceDayDB was already created by docs/RaceDayDB.sql, this does nothing. Retries let SQL Server in Docker finish starting.
if (builder.Configuration.GetValue("Database:AutoCreate", true))
{
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<RaceDayDbContext>().Database.EnsureCreated();
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            app.Logger.LogWarning("Database not ready (attempt {Attempt}/10): {Message}", attempt, ex.Message);
            Thread.Sleep(3000);
        }
    }
}
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
if (!builder.Configuration.GetValue<bool>("DisableHttpsRedirection")) app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
public partial class Program { }
