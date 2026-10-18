using System.Text.Json;
using Microsoft.OpenApi;
using RaceDay.Api.Contracts.Authentication;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Authentication;

// Schema examples: Swashbuckle (2026).
// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
// Accessed: 18 October 2026.
/// <summary>Provides usable local demonstration requests in Swagger.</summary>
public sealed class AuthenticationExamples : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema example)
            return;

        if (context.Type == typeof(RegisterRequest))
            example.Example = JsonSerializer.SerializeToNode(new
            {
                firstName = "Naledi", lastName = "Mokoena", email = "new.participant@example.com",
                password = "RaceDayDemo!2026", role = "Participant",
                phoneNumber = "0825550100", dateOfBirth = "1995-06-12"
            });
        else if (context.Type == typeof(LoginRequest))
            example.Example = JsonSerializer.SerializeToNode(new
            {
                email = "naledi.mokoena@example.com", password = "RaceDayDemo!2026"
            });
    }
}
