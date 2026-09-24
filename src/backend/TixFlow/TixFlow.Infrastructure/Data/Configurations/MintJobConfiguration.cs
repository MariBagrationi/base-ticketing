using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TixFlow.Domain.Entities;

namespace TixFlow.Infrastructure.Data.Configurations;

public class MintJobConfiguration : IEntityTypeConfiguration<MintJob>
{
    public void Configure(EntityTypeBuilder<MintJob> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(m => m.TxHash)
            .HasMaxLength(66);

        builder.Property(m => m.LastError)
            .HasMaxLength(500);

        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.UpdatedAt).IsRequired();

        builder.HasOne(m => m.Ticket)
            .WithMany(t => t.MintJobs)
            .HasForeignKey(m => m.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.Status);
    }
}
