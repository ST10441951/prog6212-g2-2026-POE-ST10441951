using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Constraints and defaults match docs/RaceDay.sql.
// Fluent mappings: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/
// https://learn.microsoft.com/en-us/ef/core/modeling/relational/default-values
// Accessed: 18 October 2026.
internal sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Events_EventName_NotBlank", "LEN(LTRIM(RTRIM(EventName))) > 0");
            table.HasCheckConstraint("CK_Events_EventType", "EventType IN (N'Running', N'Walking', N'Cycling')");
            table.HasCheckConstraint("CK_Events_Description_NotBlank", "LEN(LTRIM(RTRIM(Description))) > 0");
            table.HasCheckConstraint("CK_Events_BannerImageBlobName_NotBlank", "BannerImageBlobName IS NULL OR LEN(LTRIM(RTRIM(BannerImageBlobName))) > 0");
            table.HasCheckConstraint("CK_Events_DistanceKm", "DistanceKm > 0");
            table.HasCheckConstraint("CK_Events_EntryClosingDateTime", "EntryClosingDateTime < EventDateTime");
            table.HasCheckConstraint("CK_Events_VenueName_NotBlank", "LEN(LTRIM(RTRIM(VenueName))) > 0");
            table.HasCheckConstraint("CK_Events_AddressLine1_NotBlank", "LEN(LTRIM(RTRIM(AddressLine1))) > 0");
            table.HasCheckConstraint("CK_Events_City_NotBlank", "LEN(LTRIM(RTRIM(City))) > 0");
            table.HasCheckConstraint("CK_Events_Province", "Province IN ( N'Eastern Cape', N'Free State', N'Gauteng', N'KwaZulu-Natal', N'Limpopo', N'Mpumalanga', N'North West', N'Northern Cape', N'Western Cape' )");
            table.HasCheckConstraint("CK_Events_Latitude", "Latitude IS NULL OR Latitude BETWEEN -90 AND 90");
            table.HasCheckConstraint("CK_Events_Longitude", "Longitude IS NULL OR Longitude BETWEEN -180 AND 180");
            table.HasCheckConstraint("CK_Events_Status", "Status IN (N'Draft', N'Open', N'Closed', N'Cancelled', N'Completed')");
        });

        builder.HasKey(x => x.EventId).HasName("PK_Events");

        builder.Property(x => x.EventId).HasColumnType("int").IsRequired().UseIdentityColumn(1, 1);

        builder.Property(x => x.OrganiserUserId).HasColumnType("int").IsRequired();

        builder.Property(x => x.EventName).HasColumnType("nvarchar(120)").HasMaxLength(120).IsRequired();

        builder.Property(x => x.EventType).HasColumnType("nvarchar(20)").HasMaxLength(20).IsRequired();

        builder.Property(x => x.Description).HasColumnType("nvarchar(1000)").HasMaxLength(1000).IsRequired();

        builder.Property(x => x.BannerImageBlobName).HasColumnType("nvarchar(255)").HasMaxLength(255);

        builder.Property(x => x.DistanceKm).HasColumnType("decimal(6,2)").IsRequired();

        builder.Property(x => x.EventDateTime).HasColumnType("datetime2(0)").IsRequired();

        builder.Property(x => x.EntryClosingDateTime).HasColumnType("datetime2(0)").IsRequired();

        builder.Property(x => x.VenueName).HasColumnType("nvarchar(120)").HasMaxLength(120).IsRequired();

        builder.Property(x => x.AddressLine1).HasColumnType("nvarchar(120)").HasMaxLength(120).IsRequired();

        builder.Property(x => x.City).HasColumnType("nvarchar(80)").HasMaxLength(80).IsRequired();

        builder.Property(x => x.Province).HasColumnType("nvarchar(30)").HasMaxLength(30).IsRequired();

        builder.Property(x => x.Latitude).HasColumnType("decimal(9,6)");

        builder.Property(x => x.Longitude).HasColumnType("decimal(9,6)");

        builder.Property(x => x.Status).HasColumnType("nvarchar(20)").HasMaxLength(20).IsRequired().HasDefaultValue("Draft", "DF_Events_Status");

        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()", "DF_Events_CreatedAt");

        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasIndex(x => x.OrganiserUserId)
            .HasDatabaseName("IX_Events_OrganiserUserId");

        builder.HasIndex(x => new { x.EventDateTime, x.Status })
            .HasDatabaseName("IX_Events_EventDateTime_Status");

        builder.HasOne(x => x.Organiser).WithMany(x => x.Events)
            .HasForeignKey(x => x.OrganiserUserId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Events_Users");
    }
}
