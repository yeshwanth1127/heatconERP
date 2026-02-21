namespace HeatconERP.Domain.Entities;

public class PurchaseInvoiceLineItem
{
    public Guid Id { get; set; }
    public Guid PurchaseInvoiceId { get; set; }
    public int SortOrder { get; set; }
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal? LineTotal { get; set; } // optional, can be computed

    public PurchaseInvoice PurchaseInvoice { get; set; } = null!;
}
