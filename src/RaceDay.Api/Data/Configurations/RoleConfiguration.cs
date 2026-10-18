using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

// Constraints and defaults match docs/RaceDay.sql.
// Fluent mappings: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/
// https://learn.microsoft.com/en-us/ef/core/modeling/relational/default-values
// Accessed: 18 October 2026.
internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "dbo", table =>
        {
            table.HasCheckConstraint("CK_Roles_RoleName", "RoleName IN (N'Organiser', N'Participant')");
        });

        builder.HasKey(x => x.RoleId).HasName("PK_Roles");

        builder.Property(x => x.RoleId).HasColumnType("tinyint").IsRequired().ValueGeneratedNever();

        builder.Property(x => x.RoleName).HasColumnType("nvarchar(20)").HasMaxLength(20).IsRequired();

        builder.HasAlternateKey(x => x.RoleName).HasName("UQ_Roles_RoleName");
    }
}
