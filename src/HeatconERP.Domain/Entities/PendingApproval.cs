namespace HeatconERP.Domain.Entities;

public class PendingApproval
{
    public Guid Id { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Originator { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Value { get; set; }
}
