using HeatconERP.Domain.Enums;

namespace HeatconERP.Domain.Entities;

/// <summary>
/// Phase 1: one quality gate per WorkOrder stage (Planning/Material/Assembly/Testing/QC/Packing).
/// </summary>
public class WorkOrderQualityGate : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;

    public WorkOrderStage Stage { get; set; }
    public QualityGateStatus GateStatus { get; set; } = QualityGateStatus.Pending;

    public DateTime? PassedAt { get; set; }
    public string? PassedBy { get; set; }

    public DateTime? FailedAt { get; set; }
    public string? FailedBy { get; set; }
}


