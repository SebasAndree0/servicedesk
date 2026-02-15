namespace ServiceDesk.Web.Models.Tickets;

public record TicketResponse(
    Guid Id,
    string Title,
    string? Description,

    // ✅ Dejamos tal cual viene del API
    string Status,     // "Open" | "InProgress" | "Closed"
    string Priority,   // "P1" | "P2" | "P3"

    // ✅ NUEVO (para filtros pro y mostrar info)
    string Category,   // "Software" | ...
    string Type,       // "Incidencia" | ...

    string CreatedBy,
    string? AssignedTo,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

public record PagedTicketsResponse(
    int Page,
    int PageSize,
    int Total,
    List<TicketResponse> Items
);
