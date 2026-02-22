namespace HeatconERP.Domain.Entities;

/// <summary>
/// Legacy entity kept only to support existing EF migrations/snapshots.
/// Phase 1 Quality Gates replaces this model for all new writes.
/// </summary>
public class QualityInspection
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime InspectedAt { get; set; }
    public string InspectedBy { get; set; } = string.Empty;
}


