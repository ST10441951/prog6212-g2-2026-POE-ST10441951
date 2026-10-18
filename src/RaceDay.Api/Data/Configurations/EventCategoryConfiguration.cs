using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Constraints and defaults match docs/RaceDay.sql.
// Fluent mappings: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/
// https://learn.microsoft.com/en-us/ef/core/modeling/relational/default-values
// Accessed: 18 October 2026.
internal sealed class EventCategoryConfiguration : IEntityTypeConfiguration<EventCategory>
{
    public void Configure(EntityTypeBuilder<EventCategory> builder)
    {
        builder.ToTable("EventCategories", "dbo", table =>
        {
            table.HasCheckConstraint("CK_EventCategories_DistanceKm", "DistanceKm > 0");
            table.HasCheckConstraint("CK_EventCategories_MinimumAge", "MinimumAge IS NULL OR MinimumAge BETWEEN 1 AND 120");
            table.HasCheckConstraint("CK_EventCategories_MaximumAge", "MaximumAge IS NULL OR MaximumAge BETWEEN 1 AND 120");
            table.HasCheckConstraint("CK_EventCategories_AgeRange", "MinimumAge IS NULL OR MaximumAge IS NULL OR MinimumAge <= MaximumAge");
            table.HasCheckConstraint("CK_EventCategories_EntryFee", "EntryFee >= 0");
            table.HasCheckConstraint("CK_EventCategories_Capacity", "Capacity IS NULL OR Capacity > 0");
        });

        builder.HasKey(x => x.EventCategoryId).HasName("PK_EventCategories");

        builder.Property(x => x.EventCategoryId).HasColumnType("int").IsRequired().UseIdentityColumn(1, 1);

        builder.Property(x => x.EventId).HasColumnType("int").IsRequired();

        builder.Property(x => x.CategoryName).HasColumnType("nvarchar(80)").HasMaxLength(80).IsRequired();

        builder.Property(x => x.DistanceKm).HasColumnType("decimal(6,2)").IsRequired();

        builder.Property(x => x.MinimumAge).HasColumnType("tinyint");

        builder.Property(x => x.MaximumAge).HasColumnType("tinyint");

        builder.Property(x => x.EntryFee).HasColumnType("decimal(10,2)").IsRequired().HasDefaultValue(0m, "DF_EventCategories_EntryFee");

        builder.Property(x => x.Capacity).HasColumnType("int");

        // Write false explicitly instead of letting migration seeds use the true SQL default.
        builder.Property(x => x.IsAvailable).HasColumnType("bit").IsRequired()
            .HasDefaultValue(true, "DF_EventCategories_IsAvailable").ValueGeneratedNever();

        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()", "DF_EventCategories_CreatedAt");

        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");

        // Category names can change. A unique index keeps that possible in EF Core.
        builder.HasIndex(x => new { x.EventId, x.CategoryName }).IsUnique()
            .HasDatabaseName("UQ_EventCategories_Event_CategoryName");

        builder.HasAlternateKey(x => new { x.EventCategoryId, x.EventId }).HasName("UQ_EventCategories_Category_Event");

        builder.HasIndex(x => x.EventId)
            .HasDatabaseName("IX_EventCategories_EventId");

        builder.HasOne(x => x.Event).WithMany(x => x.Categories)
            .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_EventCategories_Events");
    }
}
