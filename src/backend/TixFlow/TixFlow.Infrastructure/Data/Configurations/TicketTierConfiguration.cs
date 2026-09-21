using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TixFlow.Domain.Entities;

namespace TixFlow.Infrastructure.Data.Configurations;

public class TicketTierConfiguration : IEntityTypeConfiguration<TicketTier>
{
    public void Configure(EntityTypeBuilder<TicketTier> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.PriceUsdc)
            .HasPrecision(18, 6);

        builder.Property(t => t.TotalSupply)
            .IsRequired();

        builder.HasOne(t => t.Event)
            .WithMany(e => e.TicketTiers)
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
