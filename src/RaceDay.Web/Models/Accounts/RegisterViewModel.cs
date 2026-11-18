using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.Accounts;

// MVC input models and field validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// API fields: docs/API-Endpoint-Plan.md, section 5.
// Accessed: 18 November 2026.
public sealed class RegisterViewModel
{
    [Required, StringLength(50), Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50), Display(Name = "Surname")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254), Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password)), Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, Display(Name = "Account type")]
    public string Role { get; set; } = "Participant";

    [StringLength(20), Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    [DataType(DataType.Date), Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }
}
