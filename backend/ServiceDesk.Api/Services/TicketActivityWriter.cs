using ServiceDesk.Api.Domain.Entities;
using ServiceDesk.Api.Infrastructure.Db;

namespace ServiceDesk.Api.Services;

public class TicketActivityWriter
{
    private readonly AppDbContext _db;

    public TicketActivityWriter(AppDbContext db) => _db = db;

    public async Task AddAsync(Guid ticketId, string by, string action, string message)
    {
        _db.TicketActivities.Add(new TicketActivity
        {
            TicketId = ticketId,
            By = by,
            Action = action,
            Message = message,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
