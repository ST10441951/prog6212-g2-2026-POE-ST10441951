using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Constraints and defaults match docs/RaceDay.sql.
// Fluent mappings: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/
// https://learn.microsoft.com/en-us/ef/core/modeling/relational/default-values
// Accessed: 18 October 2026.
internal sealed class EventRouteConfiguration : IEntityTypeConfiguration<EventRoute>
{
    public void Configure(EntityTypeBuilder<EventRoute> builder)
    {
        builder.ToTable("EventRoutes", "dbo", table =>
        {
            table.HasCheckConstraint("CK_EventRoutes_RouteName_NotBlank", "LEN(LTRIM(RTRIM(RouteName))) > 0");
            table.HasCheckConstraint("CK_EventRoutes_StartLocation_NotBlank", "LEN(LTRIM(RTRIM(StartLocation))) > 0");
            table.HasCheckConstraint("CK_EventRoutes_FinishLocation_NotBlank", "LEN(LTRIM(RTRIM(FinishLocation))) > 0");
            table.HasCheckConstraint("CK_EventRoutes_RouteDescription_NotBlank", "LEN(LTRIM(RTRIM(RouteDescription))) > 0");
            table.HasCheckConstraint("CK_EventRoutes_ElevationGainMetres", "ElevationGainMetres IS NULL OR ElevationGainMetres >= 0");
        });

        builder.HasKey(x => x.RouteId).HasName("PK_EventRoutes");

        builder.Property(x => x.RouteId).HasColumnType("int").IsRequired().UseIdentityColumn(1, 1);

        builder.Property(x => x.EventCategoryId).HasColumnType("int").IsRequired();

        builder.Property(x => x.RouteName).HasColumnType("nvarchar(120)").HasMaxLength(120).IsRequired();

        builder.Property(x => x.StartLocation).HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();

        builder.Property(x => x.FinishLocation).HasColumnType("nvarchar(200)").HasMaxLength(200).IsRequired();

        builder.Property(x => x.RouteDescription).HasColumnType("nvarchar(1000)").HasMaxLength(1000).IsRequired();

        builder.Property(x => x.RouteMapUrl).HasColumnType("nvarchar(500)").HasMaxLength(500);

        builder.Property(x => x.ElevationGainMetres).HasColumnType("int");

        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()", "DF_EventRoutes_CreatedAt");

        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasAlternateKey(x => x.EventCategoryId).HasName("UQ_EventRoutes_EventCategoryId");

        builder.HasOne(x => x.Category).WithOne(x => x.Route)
            .HasForeignKey<EventRoute>(x => x.EventCategoryId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_EventRoutes_EventCategories");
    }
}
