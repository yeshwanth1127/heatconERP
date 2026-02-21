namespace HeatconERP.Domain.Entities.Inventory;

public class Vendor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? GSTNumber { get; set; }
    public string? ContactDetails { get; set; }
    public bool IsApprovedVendor { get; set; }

    public ICollection<VendorPurchaseOrder> PurchaseOrders { get; set; } = [];
}


