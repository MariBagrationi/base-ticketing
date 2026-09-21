using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TixFlow.Domain.Entities;

namespace TixFlow.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.WalletAddress)
            .IsRequired()
            .HasMaxLength(42);

        builder.HasIndex(u => u.WalletAddress)
            .IsUnique();

        builder.Property(u => u.Email)
            .HasMaxLength(256);

        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}
