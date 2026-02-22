namespace HeatconERP.Domain.Entities;

public class QuotationRevision
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }
    public string Version { get; set; } = string.Empty; // e.g. v3.4, v3.5
    public string Action { get; set; } = string.Empty;   // created, updated, uploaded, initiated draft
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? ChangeDetails { get; set; }          // JSON or text describing changes
    public string? AttachmentPath { get; set; }
    public string? AttachmentFileName { get; set; }

    // Snapshot of quotation at this revision (for viewing past revisions)
    public string? SnapshotClientName { get; set; }
    public string? SnapshotProjectName { get; set; }
    public string? SnapshotDescription { get; set; }
    public string? SnapshotAttachments { get; set; }
    public decimal? SnapshotManualPrice { get; set; }
    public string? SnapshotPriceBreakdown { get; set; }
    public string? SnapshotStatus { get; set; }
    public decimal? SnapshotAmount { get; set; }
    public string? SnapshotLineItemsJson { get; set; }  // JSON array of line items

    /// <summary>When this revision was sent to customer (for linking incoming PO).</summary>
    public DateTime? SentToCustomerAt { get; set; }
    public string? SentToCustomerBy { get; set; }

    public Quotation Quotation { get; set; } = null!;
}
