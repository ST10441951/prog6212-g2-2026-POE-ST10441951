using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Contracts.Profiles;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

// Claims, validation, password hashing and SQL persistence: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/saving/basic
// Accessed: 18 October 2026.
/// <summary>View and update the signed-in user's own account.</summary>
[ApiController]
[Authorize]
[Route("api/profile")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
public sealed class ProfileController(
    RaceDayDbContext database, IPasswordHasher<User> passwords, SessionStore sessions, TimeProvider clock) : ControllerBase
{
    /// <summary>View your own profile.</summary>
    /// <remarks>
    /// Both roles can use this endpoint after login. No user ID is accepted; identity comes from the session.
    /// Returns personal details, role and account status without passwords, hashes or storage object names.
    /// ProfilePictureUrl is currently null; image storage will be added separately.
    /// </remarks>
    /// <response code="200">The signed-in user's profile.</response>
    /// <response code="401">The session is missing, invalid, expired or revoked.</response>
    /// <response code="403">The account is inactive.</response>
    /// <response code="404">The account was removed after authentication.</response>
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken cancellationToken)
    {
        var user = await OwnAccount(cancellationToken);
        return user is null ? Problem(statusCode: 404, title: "Account not found") : Ok(Profile(user));
    }

    /// <summary>Update your own personal details.</summary>
    /// <remarks>
    /// Send firstName and lastName as JSON. Phone number and date of birth are optional and are cleared
    /// when omitted or null. A blank phone number also clears it. Names and phone numbers are trimmed.
    /// Date of birth must be earlier than today's UTC date. Email, role, status, identity and password
    /// fields are rejected. Both roles can update only the account identified by their session.
    /// </remarks>
    /// <response code="200">The saved profile.</response>
    /// <response code="400">Personal details are invalid or an unexpected field was supplied.</response>
    /// <response code="401">The session is missing, invalid, expired or revoked.</response>
    /// <response code="403">The account is inactive.</response>
    /// <response code="404">The account was removed after authentication.</response>
    /// <response code="415">Send a JSON request body.</response>
    [HttpPut]
    [Consumes("application/json")]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<ProfileResponse>> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (request.DateOfBirth >= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
        {
            ModelState.AddModelError(nameof(request.DateOfBirth), "Date of birth must be earlier than today.");
            return ValidationProblem(ModelState);
        }

        var user = await OwnAccount(cancellationToken);
        if (user is null)
            return Problem(statusCode: 404, title: "Account not found");

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.DateOfBirth = request.DateOfBirth;
        user.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: 404, title: "Account not found");
        }
        return Ok(Profile(user));
    }

    /// <summary>Change your own password.</summary>
    /// <remarks>
    /// Send currentPassword, newPassword and confirmPassword as JSON. The current password must be correct.
    /// Use 8 to 128 new-password characters with an upper-case letter, lower-case letter, digit and symbol.
    /// Passwords are compared exactly, without trimming. The replacement is hashed before saving.
    /// Success ends this browser's session. Log in again with the new password. Other browsers retain
    /// their existing sessions until logout or their normal expiry. No credentials are returned.
    /// </remarks>
    /// <response code="204">The password was changed and this session ended. No body is returned.</response>
    /// <response code="400">The current password is incorrect, the new password is invalid or confirmation differs.</response>
    /// <response code="401">The session is missing, invalid, expired or revoked.</response>
    /// <response code="403">The account is inactive.</response>
    /// <response code="404">The account was removed after authentication.</response>
    /// <response code="415">Send a JSON request body.</response>
    [HttpPut("password")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await OwnAccount(cancellationToken);
        if (user is null)
            return Problem(statusCode: 404, title: "Account not found");

        PasswordVerificationResult verification;
        try
        {
            verification = passwords.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        }
        catch (FormatException)
        {
            verification = PasswordVerificationResult.Failed;
        }
        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(request.CurrentPassword), "The current password is incorrect.");
            return ValidationProblem(ModelState);
        }

        user.PasswordHash = passwords.HashPassword(user, request.NewPassword);
        user.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: 404, title: "Account not found");
        }
        await sessions.RemoveAsync(HttpContext);
        return NoContent();
    }

    private Task<User?> OwnAccount(CancellationToken cancellationToken)
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            System.Globalization.CultureInfo.InvariantCulture);
        return database.Users.Include(x => x.Role).SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    private static ProfileResponse Profile(User user) => new()
    {
        UserId = user.UserId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        DateOfBirth = user.DateOfBirth,
        Role = user.Role.RoleName,
        IsActive = user.IsActive,
        CreatedAt = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc)
    };
}
