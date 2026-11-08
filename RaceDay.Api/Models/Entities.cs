using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

[Table("UserAccount")]
public class UserAccount
{
    [Key] public int UserId { get; set; }
    [Required, MaxLength(50)] public string FirstName { get; set; } = "";
    [Required, MaxLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MaxLength(255)] public string PasswordHash { get; set; } = "";
    [Required, MaxLength(20)] public string Role { get; set; } = Roles.Participant;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public OrganiserProfile? OrganiserProfile { get; set; }
    public ParticipantProfile? ParticipantProfile { get; set; }
}

[Table("OrganiserProfile")]
public class OrganiserProfile
{
    [Key] public int OrganiserId { get; set; }
    public int UserId { get; set; }
    [Required, MaxLength(120)] public string OrganisationName { get; set; } = "";
    [Required, MaxLength(30)] public string ContactNumber { get; set; } = "";
    public UserAccount? UserAccount { get; set; }
    public ICollection<RaceEvent> Events { get; set; } = new List<RaceEvent>();
}

[Table("ParticipantProfile")]
public class ParticipantProfile
{
    [Key] public int ParticipantId { get; set; }
    public int UserId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [MaxLength(100)] public string? EmergencyContactName { get; set; }
    [MaxLength(30)] public string? EmergencyContactNumber { get; set; }
    [MaxLength(1000)] public string? ProfilePictureUrl { get; set; }
    public UserAccount? UserAccount { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

[Table("Event")]
public class RaceEvent
{
    [Key] public int EventId { get; set; }
    public int OrganiserId { get; set; }
    [Required, MaxLength(150)] public string EventName { get; set; } = "";
    [MaxLength(500)] public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    [Required, MaxLength(150)] public string Venue { get; set; } = "";
    [Required, MaxLength(80)] public string City { get; set; } = "";
    [Required, MaxLength(20)] public string Status { get; set; } = "Planned";
    [MaxLength(1000)] public string? BannerUrl { get; set; }
    [Required, MaxLength(50)] public string EventType { get; set; } = "Road Race";
    public OrganiserProfile? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

[Table("Category")]
public class Category
{
    [Key] public int CategoryId { get; set; }
    public int EventId { get; set; }
    [Required, MaxLength(100)] public string CategoryName { get; set; } = "";
    [Column(TypeName="decimal(6,2)")] public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    [Column(TypeName="decimal(10,2)")] public decimal EntryFee { get; set; }
    public RaceEvent? Event { get; set; }
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}

[Table("Enrollment")]
public class Enrollment
{
    [Key] public int EnrollmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public int ParticipantId { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public int RaceNumber { get; set; }
    [Required, MaxLength(20)] public string Status { get; set; } = "Confirmed";
    public RaceEvent? Event { get; set; }
    public Category? Category { get; set; }
    public ParticipantProfile? Participant { get; set; }
    public Result? Result { get; set; }
}

[Table("Result")]
public class Result
{
    [Key] public int ResultId { get; set; }
    public int EnrollmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    [Required, MaxLength(20)] public string ResultStatus { get; set; } = "Pending";
    public DateTime? RecordedAt { get; set; }
    public Enrollment? Enrollment { get; set; }
}
