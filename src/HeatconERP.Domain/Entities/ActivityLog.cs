namespace HeatconERP.Domain.Entities;

public class ActivityLog
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Tag { get; set; } = string.Empty; // SYSTEM, AUDIT, CRITICAL, USER, WARN
    public string Message { get; set; } = string.Empty;
    public string? BgClass { get; set; }
}
