using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Contracts.Events;

// Schema examples: Swashbuckle (2026).
// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
// Accessed: 18 October 2026.
/// <summary>Provides future-dated event examples for local Swagger demonstrations.</summary>
public sealed class EventWriteExamples(TimeProvider clock) : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema example ||
            (context.Type != typeof(CreateEventRequest) && context.Type != typeof(UpdateEventRequest)))
            return;

        DateTime start = clock.GetUtcNow().UtcDateTime.Date.AddDays(30).AddHours(6);
        var values = new Dictionary<string, object?>
        {
            ["eventName"] = "Community Sunrise Run", ["eventType"] = "Running",
            ["description"] = "A local community road run.", ["distanceKm"] = 10.00m,
            ["eventDateTime"] = start, ["entryClosingDateTime"] = start.AddDays(-7),
            ["venueName"] = "Community Stadium", ["addressLine1"] = "1 Main Road",
            ["city"] = "Johannesburg", ["province"] = "Gauteng", ["latitude"] = -26.2041m, ["longitude"] = 28.0473m
        };
        if (context.Type == typeof(UpdateEventRequest))
            values["status"] = "Draft";
        example.Example = JsonSerializer.SerializeToNode(values);
    }
}
