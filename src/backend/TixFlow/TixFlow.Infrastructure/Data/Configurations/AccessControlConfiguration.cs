using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TixFlow.Domain.Entities;

namespace TixFlow.Infrastructure.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.HasKey(r => new { r.UserId, r.Role });
        builder.Property(r => r.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(r => r.User)
            .WithMany(u => u.Roles)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StaffAssignmentConfiguration : IEntityTypeConfiguration<StaffAssignment>
{
    public void Configure(EntityTypeBuilder<StaffAssignment> builder)
    {
        builder.HasKey(a => new { a.UserId, a.EventId });

        builder.HasOne(a => a.User)
            .WithMany(u => u.StaffAssignments)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Event)
            .WithMany()
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ActorWallet).IsRequired().HasMaxLength(42);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(60);
        builder.Property(a => a.Target).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Details).HasMaxLength(500);
        builder.HasIndex(a => a.CreatedAt);
    }
}
