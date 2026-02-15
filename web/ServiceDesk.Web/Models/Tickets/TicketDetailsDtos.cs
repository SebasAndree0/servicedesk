namespace ServiceDesk.Web.Models.Tickets;

public record TicketActivityDto(
    DateTime CreatedAtUtc,
    string By,
    string Action,
    string Message
);

public record TicketCommentDto(
    DateTime AtUtc,
    string By,
    string Message
);

public record TicketDetailsResponse(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Priority,

    // ✅ Catálogo + SLA
    string Category,
    string Type,
    int SlaHours,

    string CreatedBy,
    string? AssignedTo,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,

    // ✅ Activity log
    List<TicketActivityDto> Activities,

    // ✅ Comentarios (vienen del API details)
    List<TicketCommentDto> Comments
);
