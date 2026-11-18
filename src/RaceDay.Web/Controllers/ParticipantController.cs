using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Web.Controllers;

// Role-protected MVC actions: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[Authorize(Roles = "Participant")]
[Route("participant")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ParticipantController : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();
}
