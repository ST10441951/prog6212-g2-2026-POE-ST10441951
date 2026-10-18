namespace RaceDay.Api.Authentication;

// Anonymous actions can still use an authenticated identity: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Checks an optional session on a public action without requiring login.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OptionalSessionAttribute : Attribute { }
