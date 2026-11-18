using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.Accounts;

// Form validation and password inputs: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class LoginViewModel
{
    [Required, EmailAddress, StringLength(254), Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(128), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
