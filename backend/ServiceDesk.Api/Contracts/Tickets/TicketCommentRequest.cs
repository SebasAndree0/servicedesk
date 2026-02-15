namespace ServiceDesk.Api.Contracts.Tickets;

public record TicketCommentRequest(
    string By,
    string Comment
);
