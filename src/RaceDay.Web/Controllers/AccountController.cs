using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Authorization;
using RaceDay.Web.Models.Accounts;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Form posts, ModelState errors, antiforgery and redirects: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/views/working-with-forms?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[Route("account")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountController(AccountApiClient api, WebSessionStore sessions, ILogger<AccountController> logger) : Controller
{
    [HttpGet("register")]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await api.RegisterAsync(model, cancellationToken);
                TempData["AccountCreated"] = true;
                return RedirectToAction(nameof(Login));
            }
            catch (AccountFormException error) { AddErrors(error); }
            catch (ApiUnavailableException error) { Unavailable(error); }
        }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        model.Password = model.ConfirmPassword = string.Empty;
        ClearPassword(nameof(model.Password));
        ClearPassword(nameof(model.ConfirmPassword));
        return View(model);
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string? returnUrl)
    {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return View(new LoginViewModel());
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginViewModel model, [FromForm] string? returnUrl, CancellationToken cancellationToken)
    {
        // Validate the destination again on POST; hidden form fields can be changed.
        // https://learn.microsoft.com/en-us/aspnet/core/security/preventing-open-redirects?view=aspnetcore-10.0
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (ModelState.IsValid)
        {
            try
            {
                var previous = await sessions.ReadAsync(HttpContext);
                var login = await api.LoginAsync(model, previous?.ApiCookie, cancellationToken);
                await sessions.CreateAsync(HttpContext, login);
                if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl!);
                return RedirectToAction("Index", login.Role == "Organiser" ? "Organiser" : "Participant");
            }
            catch (AccountFormException error) { AddErrors(error); }
            catch (ApiUnavailableException error) { Unavailable(error); }
        }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        model.Password = string.Empty;
        ClearPassword(nameof(model.Password));
        return View(model);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var login = await sessions.ReadAsync(HttpContext);
        try
        {
            if (login is not null) await api.LogoutAsync(login.ApiCookie, cancellationToken);
        }
        catch (ApiUnavailableException) { logger.LogWarning("The API did not confirm logout; the local session has been cleared."); }
        finally { await sessions.RemoveAsync(HttpContext); }
        return RedirectToAction(nameof(Login));
    }

    private void AddErrors(AccountFormException error)
    {
        string[] fields = ["FirstName", "LastName", "Email", "Password", "Role", "PhoneNumber", "DateOfBirth"];
        foreach (var entry in error.Errors)
        {
            string field = fields.FirstOrDefault(name => string.Equals(name, entry.Key, StringComparison.OrdinalIgnoreCase)) ?? "";
            foreach (string message in entry.Value ?? [])
                if (!string.IsNullOrWhiteSpace(message)) ModelState.AddModelError(field, message);
        }
        if (ModelState.IsValid) ModelState.AddModelError("", "Check the account details and try again.");
        Response.StatusCode = error.StatusCode;
    }

    private void Unavailable(ApiUnavailableException error)
    {
        logger.LogWarning(error, "The account request could not be completed.");
        Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        ModelState.AddModelError("", "The account service is temporarily unavailable. Please try again shortly.");
    }

    private void ClearPassword(string field) => ModelState.SetModelValue(field, new ValueProviderResult(string.Empty));
}
