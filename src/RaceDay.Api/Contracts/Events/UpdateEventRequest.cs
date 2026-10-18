using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Contracts.Events;

// Request validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Replacement event details and the requested status.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateEventRequest : CreateEventRequest
{
    /// <summary>Draft, Open, Closed, Cancelled or Completed. Business transition rules also apply.</summary>
    [Required, RegularExpression("Draft|Open|Closed|Cancelled|Completed")]
    public string Status { get; init; } = string.Empty;
}
