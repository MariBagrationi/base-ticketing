using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TixFlow.Domain.Entities;

namespace TixFlow.Infrastructure.Data.Configurations;

public class RedemptionCheckpointConfiguration : IEntityTypeConfiguration<RedemptionCheckpoint>
{
    public void Configure(EntityTypeBuilder<RedemptionCheckpoint> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Root).IsRequired().HasMaxLength(66);
        builder.Property(c => c.TxHash).HasMaxLength(66);
        builder.HasIndex(c => new { c.EventId, c.CreatedAt });

        builder.HasOne(c => c.Event)
            .WithMany()
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
