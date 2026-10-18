using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using RaceDay.Api.Contracts;
using Xunit;

namespace RaceDay.Api.Tests;

// In-memory hosting: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// xUnit tests: xUnit.net (2026).
// https://xunit.net/docs/getting-started/v2/getting-started
// Accessed: 17 October 2026.
public sealed class StartupTests
{
    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Health_ReturnsHostStatusWithoutDatabase(string environment)
    {
        using var factory = CreateFactory(environment);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("Healthy", body.Status);
    }

    [Fact]
    public async Task Swagger_InDevelopment_DescribesHostEndpoint()
    {
        using var factory = CreateFactory(Environments.Development);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("RaceDay API", document.RootElement.GetProperty("info").GetProperty("title").GetString());
        var operation = document.RootElement.GetProperty("paths").GetProperty("/health").GetProperty("get");
        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
        Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
    }

    [Fact]
    public async Task SwaggerUi_InDevelopment_LoadsWithLocalAssets()
    {
        using var factory = CreateFactory(Environments.Development);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/swagger/index.html");
        using var script = await client.GetAsync("/swagger/swagger-ui-bundle.js");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
    }

    [Theory]
    [InlineData("/swagger/index.html")]
    [InlineData("/swagger/v1/swagger.json")]
    public async Task Swagger_InProduction_IsUnavailable(string path)
    {
        using var factory = CreateFactory(Environments.Production);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_ReturnsJsonProblemDetails()
    {
        using var factory = CreateFactory(Environments.Development);
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/api/not-a-route");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment(environment));

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
}
