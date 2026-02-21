using HeatconERP.Domain.Enums.Inventory;

namespace HeatconERP.Domain.Entities.Inventory;

public class VendorPurchaseOrder : BaseEntity
{
    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public DateTime OrderDate { get; set; }
    public VendorPurchaseOrderStatus Status { get; set; } = VendorPurchaseOrderStatus.Ordered;

    public ICollection<VendorPurchaseOrderLineItem> LineItems { get; set; } = [];
    public ICollection<GRN> Grns { get; set; } = [];
}


