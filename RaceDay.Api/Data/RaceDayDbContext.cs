using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<OrganiserProfile> OrganiserProfiles => Set<OrganiserProfile>();
    public DbSet<ParticipantProfile> ParticipantProfiles => Set<ParticipantProfile>();
    public DbSet<RaceEvent> Events => Set<RaceEvent>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<UserAccount>().HasIndex(x => x.Email).IsUnique();
        b.Entity<UserAccount>().Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        b.Entity<OrganiserProfile>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<OrganiserProfile>().HasOne(x => x.UserAccount).WithOne(x => x.OrganiserProfile)
            .HasForeignKey<OrganiserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<ParticipantProfile>().HasIndex(x => x.UserId).IsUnique();
        b.Entity<ParticipantProfile>().HasOne(x => x.UserAccount).WithOne(x => x.ParticipantProfile)
            .HasForeignKey<ParticipantProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<RaceEvent>().HasOne(x => x.Organiser).WithMany(x => x.Events)
            .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Category>().HasIndex(x => new { x.EventId, x.CategoryId }).IsUnique();
        b.Entity<Category>().HasIndex(x => new { x.EventId, x.CategoryName }).IsUnique();
        b.Entity<Category>().HasOne(x => x.Event).WithMany(x => x.Categories)
            .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Enrollment>().HasIndex(x => x.RaceNumber).IsUnique();
        b.Entity<Enrollment>().HasIndex(x => new { x.ParticipantId, x.EventId }).IsUnique();
        b.Entity<Enrollment>().HasOne(x => x.Participant).WithMany(x => x.Enrollments)
            .HasForeignKey(x => x.ParticipantId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrollment>().HasOne(x => x.Event).WithMany(x => x.Enrollments)
            .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Enrollment>().HasOne(x => x.Category).WithMany(x => x.Enrollments)
            .HasForeignKey(x => new { x.EventId, x.CategoryId })
            .HasPrincipalKey(x => new { x.EventId, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Result>().HasIndex(x => x.EnrollmentId).IsUnique();
        b.Entity<Result>().HasOne(x => x.Enrollment).WithOne(x => x.Result)
            .HasForeignKey<Result>(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
        base.OnModelCreating(b);
    }
}
