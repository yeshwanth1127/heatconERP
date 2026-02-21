namespace HeatconERP.Domain.Entities.Inventory;

public class VendorPurchaseOrderLineItem : BaseEntity
{
    public Guid VendorPurchaseOrderId { get; set; }
    public VendorPurchaseOrder VendorPurchaseOrder { get; set; } = null!;

    public Guid MaterialVariantId { get; set; }
    public MaterialVariant MaterialVariant { get; set; } = null!;

    public decimal OrderedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
}


