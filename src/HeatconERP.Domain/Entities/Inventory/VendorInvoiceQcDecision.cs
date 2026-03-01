namespace HeatconERP.Domain.Entities.Inventory;

public class VendorInvoiceQcDecision : BaseEntity
{
    public Guid VendorPurchaseInvoiceId { get; set; }
    public VendorPurchaseInvoice VendorPurchaseInvoice { get; set; } = null!;

    public string Decision { get; set; } = string.Empty; // Accepted | Declined
    public string Description { get; set; } = string.Empty;
    public string DecidedBy { get; set; } = string.Empty;
}
