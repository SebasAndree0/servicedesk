// C:\ProyectoASPNET\servicedesk\backend\ServiceDesk.Api\Infrastructure\Db\AppDbContext.cs

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Api.Domain.Entities;

namespace ServiceDesk.Api.Infrastructure.Db;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketEvent> TicketEvents => Set<TicketEvent>();
    public DbSet<TicketActivity> TicketActivities => Set<TicketActivity>();

    // ✅ Evidencias
    public DbSet<TicketEvidence> TicketEvidences => Set<TicketEvidence>();

    // ✅ ADMIN: SLA rules
    public DbSet<SlaRule> SlaRules => Set<SlaRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ticket>(e =>
        {
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.CreatedBy).HasMaxLength(120).IsRequired();
            e.Property(x => x.AssignedTo).HasMaxLength(120);

            e.Property(x => x.Category).IsRequired();
            e.Property(x => x.Type).IsRequired();
            e.Property(x => x.SlaHours).IsRequired();

            // ✅ Soft delete + trazabilidad
            e.Property(x => x.IsDeleted).IsRequired();
            e.Property(x => x.DeletedBy).HasMaxLength(120);
            e.Property(x => x.DeleteReason).HasMaxLength(500);

            // ✅ Índices para filtros/listado
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.Priority);
            e.HasIndex(x => x.Category);
            e.HasIndex(x => x.Type);

            e.HasIndex(x => x.CreatedAtUtc);
            e.HasIndex(x => x.UpdatedAtUtc);

            e.HasIndex(x => x.CreatedBy);
            e.HasIndex(x => x.AssignedTo);

            // ✅ índice clave para excluir borrados rápido
            e.HasIndex(x => x.IsDeleted);

            // ✅ para historial de borrados
            e.HasIndex(x => x.DeletedAtUtc);
            e.HasIndex(x => x.DeletedBy);
        });

        modelBuilder.Entity<TicketEvent>(e =>
        {
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.By).HasMaxLength(120).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();

            e.HasIndex(x => x.TicketId);
            e.HasIndex(x => x.AtUtc);
        });

        modelBuilder.Entity<TicketActivity>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.By).HasMaxLength(120).IsRequired();
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.Message).HasMaxLength(2000).IsRequired();

            e.HasIndex(x => x.TicketId);
            e.HasIndex(x => x.CreatedAtUtc);

            e.HasOne(x => x.Ticket)
             .WithMany(t => t.Activities)
             .HasForeignKey(x => x.TicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ✅ Evidencias (FK -> Ticket)
        modelBuilder.Entity<TicketEvidence>(e =>
        {
            e.HasKey(x => x.Id);

            e.HasIndex(x => x.TicketId);
            e.HasIndex(x => x.UploadedAtUtc);

            e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            e.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            e.Property(x => x.StoragePath).HasMaxLength(400).IsRequired();
            e.Property(x => x.UploadedBy).HasMaxLength(120);

            // ✅ nuevos campos pro
            e.Property(x => x.Comment).HasMaxLength(500);
            e.Property(x => x.SortOrder).IsRequired();

            e.HasOne(x => x.Ticket)
             .WithMany() // si después agregas Ticket.Evidences => .WithMany(t => t.Evidences)
             .HasForeignKey(x => x.TicketId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ✅ ADMIN: Users (Identity) + campos extra tuyos
        modelBuilder.Entity<AppUser>(e =>
        {
            e.Property(x => x.UserName).HasMaxLength(120).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();

            e.Property(x => x.IsActive).IsRequired();

            e.Property(x => x.CreatedAtUtc).IsRequired();
            e.Property(x => x.UpdatedAtUtc).IsRequired();

            e.HasIndex(x => x.UserName).IsUnique();
            e.HasIndex(x => x.IsActive);

            e.Property(x => x.FullName).HasMaxLength(200);
        });

        // ✅ ADMIN: SLA rules
        modelBuilder.Entity<SlaRule>(e =>
        {
            e.Property(x => x.Hours).IsRequired();
            e.HasIndex(x => x.Priority).IsUnique();
        });
    }

    // ✅ Opcional: timestamps automáticos (solo AppUser por ahora)
    public override int SaveChanges()
    {
        TouchUserTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        TouchUserTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void TouchUserTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AppUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.UpdatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
