namespace HeatconERP.Domain.Entities;

public class PurchaseInvoice
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty; // e.g. INV-2501
    public Guid PurchaseOrderId { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Sent, Paid
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public ICollection<PurchaseInvoiceLineItem> LineItems { get; set; } = [];
}
