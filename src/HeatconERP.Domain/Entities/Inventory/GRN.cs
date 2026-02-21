namespace HeatconERP.Domain.Entities.Inventory;

public class GRN : BaseEntity
{
    public Guid VendorPurchaseOrderId { get; set; }
    public VendorPurchaseOrder VendorPurchaseOrder { get; set; } = null!;

    public DateTime ReceivedDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public ICollection<GRNLineItem> LineItems { get; set; } = [];
}


