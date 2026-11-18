using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Events;

namespace RaceDay.Web.Models.Organiser;

// Separate editable fields and read-only details: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// Field sizes follow src/RaceDay.Api/Contracts/Events/CreateEventRequest.cs.
// Accessed: 18 November 2026.
public class CreateEventForm
{
    [Required, StringLength(120), Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;
    [Required, Display(Name = "Event type")]
    public string EventType { get; set; } = string.Empty;
    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;
    [Required, Range(typeof(decimal), "0.01", "9999.99"), Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
    [Required, Display(Name = "Event start (SAST)")]
    public DateTime? EventDateTime { get; set; }
    [Required, Display(Name = "Entries close (SAST)")]
    public DateTime? EntryClosingDateTime { get; set; }
    [Required, StringLength(120), Display(Name = "Venue")]
    public string VenueName { get; set; } = string.Empty;
    [Required, StringLength(120), Display(Name = "Street address")]
    public string AddressLine1 { get; set; } = string.Empty;
    [Required, StringLength(80), Display(Name = "City or town")]
    public string City { get; set; } = string.Empty;
    [Required]
    public string Province { get; set; } = string.Empty;
    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; set; }
    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; set; }
}

public sealed class EditEventForm : CreateEventForm
{
    [Required, Display(Name = "Event status")]
    public string Status { get; set; } = string.Empty;

    // datetime-local has no offset. Display SAST explicitly, regardless of the server's time zone.
    // https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset.tooffset?view=net-10.0
    public static EditEventForm From(EventDetails item) => new()
    {
        EventName = item.EventName,
        EventType = item.EventType,
        Description = item.Description,
        DistanceKm = item.DistanceKm,
        EventDateTime = item.EventDateTime.ToOffset(TimeSpan.FromHours(2)).DateTime,
        EntryClosingDateTime = item.EntryClosingDateTime.ToOffset(TimeSpan.FromHours(2)).DateTime,
        VenueName = item.VenueName,
        AddressLine1 = item.AddressLine1,
        City = item.City,
        Province = item.Province,
        Latitude = item.Latitude,
        Longitude = item.Longitude,
        Status = item.Status
    };
}
