using HeatconERP.Domain.Enums;

namespace HeatconERP.Domain.Entities;

public class WorkOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public WorkOrderStage Stage { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }

    public Guid? PurchaseInvoiceId { get; set; }
    public PurchaseInvoice? PurchaseInvoice { get; set; }

    public string? AssignedToUserName { get; set; }

    // CRM -> Production dispatch workflow
    public DateTime? SentToProductionAt { get; set; }
    public string? SentToProductionBy { get; set; }
    public DateTime? ProductionReceivedAt { get; set; }
    public string? ProductionReceivedBy { get; set; }

    // Production Work Pipeline Timeline
    public DateTime? WorkStartedAt { get; set; }
    public string? WorkStartedBy { get; set; }
    public DateTime? WorkCompletedAt { get; set; }
    public string? WorkCompletedBy { get; set; }

    // Stage completion timestamps for timeline tracking
    public DateTime? PlanningCompletedAt { get; set; }
    public DateTime? MaterialCompletedAt { get; set; }
    public DateTime? AssemblyCompletedAt { get; set; }
    public DateTime? TestingCompletedAt { get; set; }
    public DateTime? QcCompletedAt { get; set; }
    public DateTime? PackingCompletedAt { get; set; }

    public ICollection<WorkOrderLineItem> LineItems { get; set; } = [];
}
