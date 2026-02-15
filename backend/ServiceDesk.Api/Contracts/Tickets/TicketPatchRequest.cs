using ServiceDesk.Api.Domain.Enums;

namespace ServiceDesk.Api.Contracts.Tickets;

// Para PATCH: cambios parciales
public record TicketPatchRequest(
    TicketStatus? Status,
    TicketPriority? Priority,
    string? AssignedTo,
    string By
);
