using HeatconERP.Domain.Enums.Inventory;

namespace HeatconERP.Domain.Entities.Inventory;

public class GRNLineItem : BaseEntity
{
    public Guid GRNId { get; set; }
    public GRN GRN { get; set; } = null!;

    public Guid MaterialVariantId { get; set; }
    public MaterialVariant MaterialVariant { get; set; } = null!;

    public string BatchNumber { get; set; } = string.Empty; // required
    public decimal QuantityReceived { get; set; }
    public decimal UnitPrice { get; set; }
    public QualityStatus QualityStatus { get; set; } = QualityStatus.PendingQC;

    public StockBatch? StockBatch { get; set; } // 1:1 created after processing
}


