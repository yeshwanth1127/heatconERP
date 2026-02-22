using HeatconERP.Domain.Enums.Inventory;

namespace HeatconERP.Domain.Entities.Inventory;

public class VendorPurchaseInvoice : BaseEntity
{
    public Guid VendorPurchaseOrderId { get; set; }
    public VendorPurchaseOrder VendorPurchaseOrder { get; set; } = null!;

    // Denormalized for indexing/filtering
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public VendorInvoiceStatus Status { get; set; } = VendorInvoiceStatus.Pending;

    public ICollection<VendorPurchaseInvoiceLineItem> LineItems { get; set; } = [];
}


