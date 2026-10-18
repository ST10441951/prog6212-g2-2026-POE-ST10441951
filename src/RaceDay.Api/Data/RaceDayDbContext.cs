using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// DbContext and explicit model configuration: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
// https://learn.microsoft.com/en-us/ef/core/modeling/relationships/conventions
// Accessed: 18 October 2026.
/// <summary>SQL Server context for the seven RaceDay tables.</summary>
public sealed class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    /// <summary>Permitted account roles.</summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>Organiser and Participant accounts.</summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>Events managed by Organisers.</summary>
    public DbSet<Event> Events => Set<Event>();

    /// <summary>Entry categories for events.</summary>
    public DbSet<EventCategory> EventCategories => Set<EventCategory>();

    /// <summary>Routes belonging to categories.</summary>
    public DbSet<EventRoute> EventRoutes => Set<EventRoute>();

    /// <summary>Participant event entries.</summary>
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();

    /// <summary>Official results for event entries.</summary>
    public DbSet<Result> Results => Set<Result>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Configure the nine Part 1 indexes explicitly instead of adding extra FK indexes.
        configurationBuilder.Conventions.Remove(typeof(ForeignKeyIndexConvention));
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RaceDayDbContext).Assembly);
        SeedData.Configure(modelBuilder);
    }
}
