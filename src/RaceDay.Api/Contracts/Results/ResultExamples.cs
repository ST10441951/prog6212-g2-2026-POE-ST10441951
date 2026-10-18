using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Contracts.Results;

// Swagger schema examples: Swashbuckle (2026).
// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
// Accessed: 18 October 2026.
/// <summary>Usable official result creation and correction example.</summary>
public sealed class ResultExamples : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is OpenApiSchema example && context.Type == typeof(ResultWriteRequest))
            example.Example = JsonSerializer.SerializeToNode(new
            {
                resultStatus = "Completed", finishTimeSeconds = 3600, overallPosition = 1,
                categoryPosition = 1, notes = (string?)null
            });
    }
}
