using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Constraints and defaults match docs/RaceDay.sql.
// Fluent mappings: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/
// https://learn.microsoft.com/en-us/ef/core/modeling/relational/default-values
// Accessed: 18 October 2026.
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Users_FirstName_NotBlank", "LEN(LTRIM(RTRIM(FirstName))) > 0");
            table.HasCheckConstraint("CK_Users_LastName_NotBlank", "LEN(LTRIM(RTRIM(LastName))) > 0");
            table.HasCheckConstraint("CK_Users_Email_NotBlank", "LEN(LTRIM(RTRIM(Email))) > 0");
            table.HasCheckConstraint("CK_Users_PasswordHash_NotBlank", "LEN(LTRIM(RTRIM(PasswordHash))) > 0");
            table.HasCheckConstraint("CK_Users_ProfilePictureBlobName_NotBlank", "ProfilePictureBlobName IS NULL OR LEN(LTRIM(RTRIM(ProfilePictureBlobName))) > 0");
            table.HasCheckConstraint("CK_Users_DateOfBirth", "DateOfBirth IS NULL OR DateOfBirth < CONVERT(DATE, SYSUTCDATETIME())");
        });

        builder.HasKey(x => x.UserId).HasName("PK_Users");

        builder.Property(x => x.UserId).HasColumnType("int").IsRequired().UseIdentityColumn(1, 1);

        builder.Property(x => x.RoleId).HasColumnType("tinyint").IsRequired();

        builder.Property(x => x.FirstName).HasColumnType("nvarchar(50)").HasMaxLength(50).IsRequired();

        builder.Property(x => x.LastName).HasColumnType("nvarchar(50)").HasMaxLength(50).IsRequired();

        builder.Property(x => x.Email).HasColumnType("nvarchar(254)").HasMaxLength(254).IsRequired();

        builder.Property(x => x.PasswordHash).HasColumnType("nvarchar(255)").HasMaxLength(255).IsRequired();

        builder.Property(x => x.PhoneNumber).HasColumnType("nvarchar(20)").HasMaxLength(20);

        builder.Property(x => x.DateOfBirth).HasColumnType("date");

        builder.Property(x => x.ProfilePictureBlobName).HasColumnType("nvarchar(255)").HasMaxLength(255);

        builder.Property(x => x.IsActive).HasColumnType("bit").IsRequired()
            .HasDefaultValue(true, "DF_Users_IsActive").ValueGeneratedNever();

        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").IsRequired().HasDefaultValueSql("SYSUTCDATETIME()", "DF_Users_CreatedAt");

        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)");

        builder.HasAlternateKey(x => x.Email).HasName("UQ_Users_Email");

        builder.HasIndex(x => x.RoleId)
            .HasDatabaseName("IX_Users_RoleId");

        builder.HasOne(x => x.Role).WithMany(x => x.Users)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Users_Roles");
    }
}
