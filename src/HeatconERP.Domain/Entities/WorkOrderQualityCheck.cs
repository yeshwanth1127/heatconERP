using HeatconERP.Domain.Enums;

namespace HeatconERP.Domain.Entities;

/// <summary>
/// Immutable (append-only) QC event log for a WorkOrder + Stage.
/// No updates, no deletes: the application should only ever INSERT.
/// </summary>
public class WorkOrderQualityCheck : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;

    public Guid WorkOrderQualityGateId { get; set; }
    public WorkOrderQualityGate WorkOrderQualityGate { get; set; } = null!;

    public WorkOrderStage Stage { get; set; }
    public QualityCheckResult Result { get; set; }
    public string? Notes { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}


