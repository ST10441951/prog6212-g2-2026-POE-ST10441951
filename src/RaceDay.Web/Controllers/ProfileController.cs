using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RaceDay.Web.Models.Profiles;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Allowed form fields, validation and private session handling: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[Authorize]
[Route("profile")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProfileController(ProfileApiClient api, WebSessionStore sessions, ILogger<ProfileController> logger) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try { return View(await api.Get(await Login(), cancellationToken)); }
        catch (ApiSessionEndedException) { return await SignInAgain(); }
        catch (ApiUnavailableException error) { return Unavailable(error); }
    }

    [HttpGet("edit")]
    public async Task<IActionResult> Edit(CancellationToken cancellationToken)
    {
        try
        {
            var profile = await api.Get(await Login(), cancellationToken);
            return View(new EditProfileForm
            {
                FirstName = profile.FirstName, LastName = profile.LastName,
                PhoneNumber = profile.PhoneNumber, DateOfBirth = profile.DateOfBirth
            });
        }
        catch (ApiSessionEndedException) { return await SignInAgain(); }
        catch (ApiUnavailableException error) { return Unavailable(error); }
    }

    [HttpPost("edit")]
    public async Task<IActionResult> Edit([Bind("FirstName,LastName,PhoneNumber,DateOfBirth")] EditProfileForm model,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await api.Update(model, await Login(), cancellationToken);
                TempData["ProfileSaved"] = true;
                return RedirectToAction(nameof(Index));
            }
            catch (AccountFormException error) { AddErrors(error); }
            catch (ApiSessionEndedException) { return await SignInAgain(); }
            catch (ApiUnavailableException error)
            {
                ViewData["UnconfirmedProfile"] = true;
                return Unavailable(error);
            }
        }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        return View(model);
    }

    [HttpGet("password")]
    public IActionResult Password() => View(new ChangePasswordForm());

    [HttpPost("password")]
    public async Task<IActionResult> Password([Bind("CurrentPassword,NewPassword,ConfirmPassword")] ChangePasswordForm model,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await api.ChangePassword(model, await Login(), cancellationToken);
                await EndSession();
                TempData["PasswordChanged"] = true;
                return RedirectToAction("Login", "Account");
            }
            catch (AccountFormException error) { AddErrors(error); }
            catch (ApiSessionEndedException) { return await SignInAgain(); }
            catch (ApiUnavailableException)
            {
                logger.LogWarning("The password change could not be confirmed; the user has been signed out.");
                await EndSession();
                Response.StatusCode = 503;
                return View("PasswordUnconfirmed");
            }
        }
        if (Response.StatusCode == 200) Response.StatusCode = 400;
        model.CurrentPassword = model.NewPassword = model.ConfirmPassword = string.Empty;
        foreach (string field in new[] { "CurrentPassword", "NewPassword", "ConfirmPassword" })
            ModelState.SetModelValue(field, new ValueProviderResult(string.Empty));
        return View(model);
    }

    private async Task<WebLogin> Login() => await sessions.ReadAsync(HttpContext) ?? throw new ApiSessionEndedException();

    private async Task<IActionResult> SignInAgain()
    {
        await EndSession();
        return RedirectToAction("Login", "Account", new { returnUrl = Request.PathBase + Request.Path });
    }

    private async Task EndSession()
    {
        await sessions.RemoveAsync(HttpContext);
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
    }

    private IActionResult Unavailable(ApiUnavailableException error)
    {
        logger.LogWarning(error, "The profile request could not be completed.");
        Response.StatusCode = 503;
        return View("Unavailable");
    }

    private void AddErrors(AccountFormException error)
    {
        string[] fields = ["FirstName", "LastName", "PhoneNumber", "DateOfBirth", "CurrentPassword", "NewPassword", "ConfirmPassword"];
        foreach (var entry in error.Errors)
        {
            string field = fields.FirstOrDefault(name => string.Equals(name, entry.Key, StringComparison.OrdinalIgnoreCase)) ?? "";
            foreach (string message in entry.Value ?? [])
                if (!string.IsNullOrWhiteSpace(message)) ModelState.AddModelError(field, message);
        }
        if (ModelState.IsValid) ModelState.AddModelError("", "Check the details and try again.");
        Response.StatusCode = error.StatusCode;
    }
}
