namespace HeatconERP.Domain.Entities.Inventory;

public class VendorPurchaseInvoiceLineItem : BaseEntity
{
    public Guid VendorPurchaseInvoiceId { get; set; }
    public VendorPurchaseInvoice VendorPurchaseInvoice { get; set; } = null!;

    public Guid MaterialVariantId { get; set; }
    public MaterialVariant MaterialVariant { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}


