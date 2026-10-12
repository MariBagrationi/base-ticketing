using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TixFlow.Domain.Entities;

namespace TixFlow.Infrastructure.Data.Configurations;

public class ChainActivityConfiguration : IEntityTypeConfiguration<ChainActivity>
{
    public void Configure(EntityTypeBuilder<ChainActivity> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Kind).IsRequired().HasMaxLength(40);
        builder.Property(a => a.ContractAddress).IsRequired().HasMaxLength(42);
        builder.Property(a => a.TxHash).IsRequired().HasMaxLength(66);
        builder.Property(a => a.Summary).IsRequired().HasMaxLength(300);

        // A log is identified by its transaction and index, which makes re-indexing idempotent.
        builder.HasIndex(a => new { a.TxHash, a.LogIndex }).IsUnique();
        builder.HasIndex(a => new { a.EventId, a.BlockNumber });
    }
}
