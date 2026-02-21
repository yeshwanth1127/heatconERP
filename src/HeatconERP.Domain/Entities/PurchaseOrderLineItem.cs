namespace HeatconERP.Domain.Entities;

public class PurchaseOrderLineItem
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public int SortOrder { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxPercent { get; set; }
    public string? AttachmentPath { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
}
