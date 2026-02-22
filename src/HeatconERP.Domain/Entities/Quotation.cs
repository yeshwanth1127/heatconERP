namespace HeatconERP.Domain.Entities;

public class Quotation
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public Guid? EnquiryId { get; set; }
    public string Version { get; set; } = "v1";
    public string? ClientName { get; set; }
    public string? ProjectName { get; set; }
    public string? Description { get; set; }
    public string? Attachments { get; set; } // Comma-separated paths or JSON array of attachment URLs
    public decimal? ManualPrice { get; set; } // Optional override
    public string? PriceBreakdown { get; set; } // Optional breakdown/notes
    public string Status { get; set; } = "Draft"; // Draft, In Review, Published, Expired
    public decimal? Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }

    public Enquiry? Enquiry { get; set; }
    public ICollection<QuotationLineItem> LineItems { get; set; } = [];
    public ICollection<QuotationRevision> Revisions { get; set; } = [];
}
