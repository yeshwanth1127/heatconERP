using HeatconERP.Domain.Enums;

namespace HeatconERP.Domain.Entities;

/// <summary>
/// Phase 1: simple NCR created automatically when a stage QC fails.
/// RCA is intentionally NOT required here.
/// </summary>
public class Ncr : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public WorkOrder WorkOrder { get; set; } = null!;

    public WorkOrderStage Stage { get; set; }

    public string Description { get; set; } = string.Empty;
    public NcrStatus Status { get; set; } = NcrStatus.Open;
    public NcrDisposition? Disposition { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }
    public string? ClosureNotes { get; set; }
}


