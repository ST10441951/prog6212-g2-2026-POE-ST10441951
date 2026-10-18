using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using RaceDay.Api.Authentication;
using RaceDay.Api.Models;

// Swagger setup: Swashbuckle (2026).
// https://github.com/domaindrivendev/Swashbuckle.AspNetCore
// Error responses and HTTPS: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-10.0
// Accessed: 17 October 2026.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add(new AuthorizeFilter()));
builder.Services.AddProblemDetails();
// Session authentication and server-side cache: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed?view=aspnetcore-10.0
// Accessed: 18 October 2026.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDataProtection();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication(SessionStore.Scheme)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionStore.Scheme, _ => { });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Organiser", policy => policy.RequireAuthenticatedUser().RequireRole("Organiser"));
    options.AddPolicy("Participant", policy => policy.RequireAuthenticatedUser().RequireRole("Participant"));
});
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
    options.AddSecurityDefinition("SessionCookie", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Name = SessionStore.CookieName,
        Description = "Use POST /api/auth/login. The browser sends the HttpOnly session cookie automatically."
    });
    options.DocumentFilter<SessionSecurityFilter>();
    options.SchemaFilter<AuthenticationExamples>();
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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

/// <summary>
/// Entry point exposed for the in-memory API tests.
/// </summary>
public partial class Program { }
