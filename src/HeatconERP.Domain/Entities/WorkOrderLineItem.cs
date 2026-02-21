namespace HeatconERP.Domain.Entities;

public class WorkOrderLineItem
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public int SortOrder { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal? LineTotal { get; set; }

    public WorkOrder WorkOrder { get; set; } = null!;
}

