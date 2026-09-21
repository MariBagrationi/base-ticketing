using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TixFlow.Domain.Entities;

namespace TixFlow.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Quantity)
            .IsRequired();

        builder.Property(o => o.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(o => o.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(o => o.IdempotencyKey)
            .IsUnique();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.HasOne(o => o.Buyer)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Event)
            .WithMany(e => e.Orders)
            .HasForeignKey(o => o.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.TicketTier)
            .WithMany(t => t.Orders)
            .HasForeignKey(o => o.TicketTierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
