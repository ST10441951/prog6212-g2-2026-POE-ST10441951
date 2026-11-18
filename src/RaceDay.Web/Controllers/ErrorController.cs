using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

// Exception handling and status-code re-execution: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[IgnoreAntiforgeryToken]
public sealed class ErrorController : Controller
{
    [Route("/error")]
    public IActionResult Error() => Page(500);

    [Route("/errors/{statusCode:int}")]
    public IActionResult Status(int statusCode) => Page(statusCode is >= 400 and <= 599 ? statusCode : 404);

    private IActionResult Page(int statusCode)
    {
        Response.StatusCode = statusCode;
        return View("~/Views/Shared/Error.cshtml", new ErrorViewModel
        {
            StatusCode = statusCode,
            Heading = statusCode == 404 ? "Page not found" : "Something went wrong",
            Message = statusCode == 404
                ? "This page may have moved or the address may be incorrect."
                : "We could not complete that request. Please try again.",
            RequestId = statusCode >= 500 ? Activity.Current?.Id ?? HttpContext.TraceIdentifier : null
        });
    }
}
