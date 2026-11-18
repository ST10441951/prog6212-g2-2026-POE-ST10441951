using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.Profiles;

// Separate edit forms and read-only response fields: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Profiles/.
// Accessed: 18 November 2026.
public sealed class ProfileDetails
{
    public required int UserId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? PhoneNumber { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? ProfilePictureUrl { get; init; }
    public required string Role { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class EditProfileForm
{
    [Required, StringLength(50), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50), Display(Name = "Surname")]
    public string LastName { get; set; } = string.Empty;

    [Phone, StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }
}

public sealed class ChangePasswordForm
{
    [Required, StringLength(128), DataType(DataType.Password), Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 8), DataType(DataType.Password), Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, StringLength(128), Compare(nameof(NewPassword)), DataType(DataType.Password), Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
