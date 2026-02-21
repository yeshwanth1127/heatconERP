namespace HeatconERP.Domain.Entities;

public class PurchaseOrder
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty; // Internal editable format e.g. PO-2501
    public Guid? QuotationId { get; set; }
    /// <summary>Revision that was sent to customer; links this PO to that sent quotation.</summary>
    public Guid? QuotationRevisionId { get; set; }
    public string CustomerPONumber { get; set; } = string.Empty; // Customer's PO reference
    public DateTime PODate { get; set; }
    public string? DeliveryTerms { get; set; }
    public string? PaymentTerms { get; set; }
    public string Status { get; set; } = "Active"; // Active, Completed, Cancelled
    public decimal? Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;

    public Quotation? Quotation { get; set; }
    public QuotationRevision? QuotationRevision { get; set; }
    public ICollection<PurchaseOrderLineItem> LineItems { get; set; } = [];
}
