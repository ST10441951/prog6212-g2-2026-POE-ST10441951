using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Contracts.Authentication;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

// Controller validation, password hashing and persistence: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/saving/basic
// Accessed: 18 October 2026.
/// <summary>Register accounts and manage login sessions.</summary>
[ApiController]
[Route("api/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthController(
    RaceDayDbContext database, IPasswordHasher<User> passwords, SessionStore sessions, TimeProvider clock) : ControllerBase
{
    /// <summary>Register an Organiser or Participant account.</summary>
    /// <remarks>
    /// Send firstName, lastName, email, password and role as JSON. Phone number and date of birth are optional.
    /// Choose Organiser or Participant; database IDs and account status are not accepted.
    /// Use 8 to 128 password characters with an upper-case letter, lower-case letter, digit and symbol.
    /// Registration does not sign in the new account.
    /// </remarks>
    /// <response code="201">The account is saved. Returns a safe summary and the profile resource Location.</response>
    /// <response code="400">Account fields, role or password are invalid, or an unexpected field was supplied.</response>
    /// <response code="409">An account already uses this email.</response>
    [AllowAnonymous]
    [HttpPost("register")]
    [Consumes("application/json")]
    [ProducesResponseType<AccountResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<AccountResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (request.DateOfBirth >= DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
        {
            ModelState.AddModelError(nameof(request.DateOfBirth), "Date of birth must be earlier than today.");
            return ValidationProblem(ModelState);
        }

        if (await database.Users.AnyAsync(x => x.Email == request.Email, cancellationToken))
            return Problem(statusCode: 409, title: "Email already registered");

        var role = await database.Roles.SingleAsync(x => x.RoleName == request.Role, cancellationToken);
        var user = new User
        {
            RoleId = role.RoleId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            IsActive = true,
            CreatedAt = clock.GetUtcNow().UtcDateTime
        };
        user.PasswordHash = passwords.HashPassword(user, request.Password);
        database.Users.Add(user);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // The unique email constraint also handles registrations arriving together.
            return Problem(statusCode: 409, title: "Email already registered");
        }

        return Created("/api/profile", Account(user, role.RoleName));
    }

    /// <summary>Log in and start a server-side session.</summary>
    /// <remarks>
    /// Send the registered email and password as JSON. Both roles use this endpoint.
    /// Returns safe account details and a fixed UTC expiry 20 minutes from login.
    /// The browser keeps the HttpOnly session cookie and sends it on later Swagger requests.
    /// Logging in again replaces this browser's previous session.
    /// </remarks>
    /// <response code="200">Credentials are valid and a new session is established.</response>
    /// <response code="400">Required fields are missing or invalid.</response>
    /// <response code="401">The email or password is incorrect.</response>
    /// <response code="403">Credentials are correct but the account is inactive.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [Consumes("application/json")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await database.Users.Include(x => x.Role)
            .SingleOrDefaultAsync(x => x.Email == request.Email, cancellationToken);
        if (user is null)
            return Problem(statusCode: 401, title: "Invalid email or password");

        PasswordVerificationResult verification;
        try
        {
            verification = passwords.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        }
        catch (FormatException)
        {
            verification = PasswordVerificationResult.Failed;
        }
        if (verification == PasswordVerificationResult.Failed)
            return Problem(statusCode: 401, title: "Invalid email or password");
        if (!user.IsActive)
            return Problem(statusCode: 403, title: "Account inactive");

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwords.HashPassword(user, request.Password);
            await database.SaveChangesAsync(cancellationToken);
        }

        var session = await sessions.CreateAsync(HttpContext, user.UserId, user.Role.RoleName);
        return Ok(new LoginResponse
        {
            SessionExpiresAt = session.ExpiresAt,
            User = Account(user, user.Role.RoleName)
        });
    }

    /// <summary>Log out and invalidate the current session.</summary>
    /// <remarks>Takes no request body. Removes the server-side session and clears its cookie.</remarks>
    /// <response code="204">The session ended. No response body is returned.</response>
    /// <response code="401">The session is missing, invalid, expired or already ended.</response>
    /// <response code="403">The signed-in account has become inactive.</response>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<IActionResult> Logout()
    {
        await sessions.RemoveAsync(HttpContext);
        return NoContent();
    }

    private static AccountResponse Account(User user, string role) => new()
    {
        UserId = user.UserId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = role,
        IsActive = user.IsActive
    };
}
