// C:\ProyectoASPNET\servicedesk\backend\ServiceDesk.Api\Contracts\Admin\SlaRuleDtos.cs
using ServiceDesk.Api.Domain.Enums;

namespace ServiceDesk.Api.Contracts.Admin;

public record SlaRuleResponse(
    TicketPriority Priority,
    int Hours,
    DateTime UpdatedAtUtc
);

public record SlaRuleUpsertRequest(
    TicketPriority Priority,
    int Hours
);
