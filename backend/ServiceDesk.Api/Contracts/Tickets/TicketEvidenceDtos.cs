namespace ServiceDesk.Api.Contracts.Tickets;

public record TicketEvidenceItem(
    Guid Id,
    Guid TicketId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string UploadedBy,
    DateTime UploadedAtUtc
);

public record TicketEvidenceListResponse(List<TicketEvidenceItem> Items);
