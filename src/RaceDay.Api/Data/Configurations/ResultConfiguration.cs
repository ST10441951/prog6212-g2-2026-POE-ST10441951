using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Constraints and defaults match docs/RaceDay.sql.
// Fluent mappings: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/
// https://learn.microsoft.com/en-us/ef/core/modeling/relational/default-values
// Accessed: 18 October 2026.
internal sealed class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.ToTable("Results", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Results_ResultStatus", "ResultStatus IN ( N'Completed', N'DidNotFinish', N'Disqualified', N'DidNotStart' )");
            table.HasCheckConstraint("CK_Results_FinishTime", "(ResultStatus = N'Completed' AND FinishTimeSeconds IS NOT NULL AND FinishTimeSeconds > 0) OR (ResultStatus <> N'Completed' AND FinishTimeSeconds IS NULL)");
            table.HasCheckConstraint("CK_Results_OverallPosition", "OverallPosition IS NULL OR OverallPosition > 0");
            table.HasCheckConstraint("CK_Results_CategoryPosition", "CategoryPosition IS NULL OR CategoryPosition > 0");
        });

        builder.HasKey(x => x.ResultId).HasName("PK_Results");

        builder.Property(x => x.ResultId).HasColumnType("int").IsRequired().UseIdentityColumn(1, 1);

        builder.Property(x => x.EnrolmentId).HasColumnType("int").IsRequired();

        builder.Property(x => x.RecordedByUserId).HasColumnType("int").IsRequired();

        builder.Property(x => x.ResultStatus).HasColumnType("nvarchar(20)").HasMaxLength(20).IsRequired().HasDefaultValue("Completed", "DF_Results_ResultStatus");

        builder.Property(x => x.FinishTimeSeconds).HasColumnType("int");

        builder.Property(x => x.OverallPosition).HasColumnType("int");

        builder.Property(x => x.CategoryPosition).HasColumnType("int");

        builder.Property(x => x.Notes).HasColumnType("nvarchar(500)").HasMaxLength(500);

        builder.Property(x => x.RecordedAt).HasColumnType("datetime2(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()", "DF_Results_RecordedAt");

        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasAlternateKey(x => x.EnrolmentId).HasName("UQ_Results_EnrolmentId");

        builder.HasIndex(x => x.RecordedByUserId)
            .HasDatabaseName("IX_Results_RecordedByUserId");

        builder.HasIndex(x => x.ResultStatus)
            .HasDatabaseName("IX_Results_ResultStatus");

        builder.HasOne(x => x.Enrolment).WithOne(x => x.Result)
            .HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Results_Enrolments");

        builder.HasOne(x => x.RecordedBy).WithMany(x => x.RecordedResults)
            .HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Results_Users");
    }
}
