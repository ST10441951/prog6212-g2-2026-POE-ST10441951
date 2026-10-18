namespace RaceDay.Api.Models;

// Fields and relationships follow docs/RaceDay-Data-Dictionary.md and docs/RaceDay.sql.
// EF Core entity properties: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties
// Accessed: 18 October 2026.
/// <summary>Database record in Users.</summary>
public sealed class User
{
    /// <summary>Unique User identifier.</summary>
    public int UserId { get; set; }

    /// <summary>Assigned account Role.</summary>
    public byte RoleId { get; set; }

    /// <summary>User's first name.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>User's surname.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Normalised sign-in email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Password hash produced by the future API.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Optional contact number.</summary>
    public string? PhoneNumber { get; set; }

    /// <summary>Participant date of birth when supplied.</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Azure Blob Storage object name for the Participant's profile picture.</summary>
    public string? ProfilePictureBlobName { get; set; }

    /// <summary>Indicates whether the account may be used.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Account creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Latest profile update timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Assigned account role.</summary>
    public Role Role { get; set; } = null!;

    /// <summary>Events managed by this Organiser.</summary>
    public ICollection<Event> Events { get; set; } = [];

    /// <summary>Entries made by this Participant.</summary>
    public ICollection<Enrolment> Enrolments { get; set; } = [];

    /// <summary>Results recorded by this Organiser.</summary>
    public ICollection<Result> RecordedResults { get; set; } = [];
}
