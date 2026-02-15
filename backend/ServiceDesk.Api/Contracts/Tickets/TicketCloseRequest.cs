namespace ServiceDesk.Api.Contracts.Tickets;

public record TicketCloseRequest(string By, string? Comment);
