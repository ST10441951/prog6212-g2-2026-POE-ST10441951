using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Constraints and defaults match docs/RaceDay.sql.
// Fluent mappings: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/
// https://learn.microsoft.com/en-us/ef/core/modeling/relational/default-values
// Accessed: 18 October 2026.
internal sealed class EnrolmentConfiguration : IEntityTypeConfiguration<Enrolment>
{
    public void Configure(EntityTypeBuilder<Enrolment> builder)
    {
        builder.ToTable("Enrolments", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Enrolments_Status", "Status IN (N'Confirmed', N'Cancelled')");
        });

        builder.HasKey(x => x.EnrolmentId).HasName("PK_Enrolments");

        builder.Property(x => x.EnrolmentId).HasColumnType("int").IsRequired().UseIdentityColumn(1, 1);

        builder.Property(x => x.ParticipantUserId).HasColumnType("int").IsRequired();

        builder.Property(x => x.EventId).HasColumnType("int").IsRequired();

        builder.Property(x => x.EventCategoryId).HasColumnType("int").IsRequired();

        builder.Property(x => x.EnrolmentDate).HasColumnType("datetime2(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()", "DF_Enrolments_EnrolmentDate");

        builder.Property(x => x.Status).HasColumnType("nvarchar(20)").HasMaxLength(20).IsRequired().HasDefaultValue("Confirmed", "DF_Enrolments_Status");

        builder.Property(x => x.BibNumber).HasColumnType("nvarchar(20)").HasMaxLength(20);

        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasAlternateKey(x => new { x.ParticipantUserId, x.EventId }).HasName("UQ_Enrolments_Participant_Event");

        builder.HasIndex(x => new { x.EventId, x.Status })
            .HasDatabaseName("IX_Enrolments_EventId_Status");

        builder.HasIndex(x => x.EventCategoryId)
            .HasDatabaseName("IX_Enrolments_EventCategoryId");

        builder.HasIndex(x => new { x.EventId, x.BibNumber }).IsUnique()
            .HasDatabaseName("UX_Enrolments_EventId_BibNumber")
            .HasFilter("BibNumber IS NOT NULL");

        builder.HasOne(x => x.Participant).WithMany(x => x.Enrolments)
            .HasForeignKey(x => x.ParticipantUserId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Enrolments_Users");

        builder.HasOne(x => x.Event).WithMany(x => x.Enrolments)
            .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Enrolments_Events");

        builder.HasOne(x => x.Category).WithMany(x => x.Enrolments)
            .HasForeignKey(x => new { x.EventCategoryId, x.EventId })
            .HasPrincipalKey(x => new { x.EventCategoryId, x.EventId })
            .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_Enrolments_EventCategories");
    }
}
