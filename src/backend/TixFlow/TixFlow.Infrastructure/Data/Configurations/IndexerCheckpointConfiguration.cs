using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TixFlow.Domain.Entities;

namespace TixFlow.Infrastructure.Data.Configurations;

public class IndexerCheckpointConfiguration : IEntityTypeConfiguration<IndexerCheckpoint>
{
    public void Configure(EntityTypeBuilder<IndexerCheckpoint> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.BlockHash)
            .HasMaxLength(66);

        builder.Property(c => c.UpdatedAt).IsRequired();
    }
}
