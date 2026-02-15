// ServiceDesk.Api/Domain/Entities/TicketEvidence.cs

using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.Api.Domain.Entities;

public class TicketEvidence
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    [Required]
    [StringLength(260)]
    public string FileName { get; set; } = "";

    [Required]
    [StringLength(120)]
    public string ContentType { get; set; } = "";

    public long SizeBytes { get; set; }

    [Required]
    [StringLength(400)]
    public string StoragePath { get; set; } = "";

    [StringLength(120)]
    public string UploadedBy { get; set; } = "";

    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    // ============================================================
    // ✅ NUEVO: Comentario por evidencia (para UI pro)
    // ============================================================
    [StringLength(500)]
    public string? Comment { get; set; }

    // ============================================================
    // ✅ NUEVO (opcional recomendado): orden para reordenar evidencias
    // ============================================================
    public int SortOrder { get; set; } = 0;

    // ============================================================
    // (Opcional futuro) Soft delete de evidencias
    // Si prefieres NO eliminar físico y dejar trazabilidad en tabla:
    //
    // public bool IsDeleted { get; set; } = false;
    // public DateTime? DeletedAtUtc { get; set; }
    // public string? DeletedBy { get; set; }
    // public string? DeleteReason { get; set; }
    // ============================================================
}
