using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Domain.Entities;

namespace ServiceDesk.Api.Infrastructure.Db;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketEvent> TicketEvents => Set<TicketEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ticket>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(120).IsRequired();
            e.Property(x => x.AssignedTo).HasMaxLength(120);

            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.CreatedAtUtc);
        });

        modelBuilder.Entity<TicketEvent>(e =>
        {
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.By).HasMaxLength(120).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();

            e.HasIndex(x => x.TicketId);
            e.HasIndex(x => x.AtUtc);
        });
    }
}
