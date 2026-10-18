using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;

// Swagger setup: Swashbuckle (2026).
// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
// Error responses and HTTPS: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-10.0
// Accessed: 17 October 2026.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
// SQL Server context registration: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
// Accessed: 18 October 2026.
builder.Services.AddDbContext<RaceDayDbContext>(options =>
{
    string connectionString = builder.Configuration.GetConnectionString("RaceDay")
        ?? throw new InvalidOperationException("Set the ConnectionStrings:RaceDay setting before using the database.");
    options.UseSqlServer(connectionString);
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "API for RaceDay event management. Business endpoints are being added."
    });
    string comments = Path.Combine(AppContext.BaseDirectory, "RaceDay.Api.xml");
    options.IncludeXmlComments(comments);
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("v1/swagger.json", "RaceDay API v1"));
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.MapControllers();
app.Run();

/// <summary>
/// Entry point exposed for the in-memory API tests.
/// </summary>
public partial class Program { }
