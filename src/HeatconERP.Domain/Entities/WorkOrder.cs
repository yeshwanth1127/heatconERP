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

    public ICollection<WorkOrderLineItem> LineItems { get; set; } = [];
}
