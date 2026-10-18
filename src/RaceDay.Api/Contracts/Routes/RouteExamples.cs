using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Contracts.Routes;

// Swagger schema examples: Swashbuckle (2026).
// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
// Accessed: 18 October 2026.
/// <summary>Provides a usable route creation and update example.</summary>
public sealed class RouteExamples : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema example && context.Type == typeof(RouteWriteRequest))
            example.Example = JsonSerializer.SerializeToNode(new
            {
                routeName = "Stadium Loop", startLocation = "Community Stadium, Gate A",
                finishLocation = "Community Stadium, finish line",
                routeDescription = "Follow the marked course and return to the stadium.",
                routeMapUrl = (string?)null, elevationGainMetres = 85
            });
    }
}
