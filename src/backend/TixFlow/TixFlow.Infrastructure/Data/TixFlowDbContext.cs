using Microsoft.EntityFrameworkCore;
using TixFlow.Domain.Entities;

namespace TixFlow.Infrastructure.Data;

public class TixFlowDbContext : DbContext
{
    public TixFlowDbContext(DbContextOptions<TixFlowDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<TicketTier> TicketTiers => Set<TicketTier>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<MintJob> MintJobs => Set<MintJob>();
    public DbSet<IndexerCheckpoint> IndexerCheckpoints => Set<IndexerCheckpoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TixFlowDbContext).Assembly);
    }
}
