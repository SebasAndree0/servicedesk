namespace ServiceDesk.Web.Models.Tickets;

public static class TicketEnumText
{
    // Soporta:
    // - valores exactos: 1=Open, 2=InProgress, 4=Closed
    // - valores tipo FLAGS (bitmask): 3, 6, 7, etc. (prioriza Closed > InProgress > Open)
    // - compat con 0 como Open
    public static string StatusToText(int s)
    {
        if ((s & 4) == 4) return "Closed";
        if ((s & 2) == 2) return "InProgress";
        if ((s & 1) == 1) return "Open";
        if (s == 0) return "Open";
        return "Unknown";
    }

    public static string StatusToLabel(int s)
    {
        if ((s & 4) == 4) return "Cerrado";
        if ((s & 2) == 2) return "En progreso";
        if ((s & 1) == 1) return "Abierto";
        if (s == 0) return "Abierto";
        return "Desconocido";
    }

    public static string StatusToBadgeClass(int s)
    {
        if ((s & 4) == 4) return "bg-success";
        if ((s & 2) == 2) return "bg-info text-dark";
        if ((s & 1) == 1) return "bg-warning text-dark";
        if (s == 0) return "bg-warning text-dark";
        return "bg-secondary";
    }

    public static string PriorityToText(int p) => p switch
    {
        1 => "P1",
        2 => "P2",
        3 => "P3",
        _ => "P?"
    };
}
