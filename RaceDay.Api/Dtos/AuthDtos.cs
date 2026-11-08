using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public record RegisterRequest(
    [Required, MaxLength(50)] string FirstName,
    [Required, MaxLength(50)] string LastName,
    [Required, EmailAddress, MaxLength(150)] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string Role,
    string? OrganisationName,
    string? ContactNumber,
    DateTime? DateOfBirth,
    string? EmergencyContactName,
    string? EmergencyContactNumber);

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
public record ProfileUpdateRequest([Required, MaxLength(50)] string FirstName, [Required, MaxLength(50)] string LastName);
public record EventRequest([Required, MaxLength(150)] string EventName, [MaxLength(500)] string? Description,
    DateTime EventDate, [Required, MaxLength(150)] string Venue, [Required, MaxLength(80)] string City,
    [Required] string Status, [Required, MaxLength(50)] string EventType);
public record CategoryRequest([Required, MaxLength(100)] string CategoryName, decimal DistanceKm,
    int MaxParticipants, decimal EntryFee);
public record EnrollmentRequest(int CategoryId);
public record ResultRequest(TimeSpan? FinishTime, int? Position, string ResultStatus);
