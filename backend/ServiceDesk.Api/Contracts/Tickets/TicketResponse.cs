using ServiceDesk.Api.Domain.Enums;

namespace ServiceDesk.Api.Contracts.Tickets;

public record TicketResponse(
    Guid Id,
    string Title,
    string? Description,
    TicketStatus Status,
    TicketPriority Priority,

    // ✅ CATÁLOGO (NUEVO)
    TicketCategory Category,
    TicketType Type,

    // ✅ SLA (NUEVO)
    int SlaHours,

    string CreatedBy,
    string? AssignedTo,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
