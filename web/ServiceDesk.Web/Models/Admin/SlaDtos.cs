namespace ServiceDesk.Web.Models.Admin;

public record SlaRuleDto(
    string Id,
    string Priority,
    int Hours,
    DateTime UpdatedAtUtc
);

public record SlaRulesListResponse(
    List<SlaRuleDto> Items
);

public record UpsertSlaRequest(
    int Hours
);
