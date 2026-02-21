using HeatconERP.Domain.Enums.Inventory;

namespace HeatconERP.Domain.Entities.Inventory;

public class StockBatch : BaseEntity
{
    public Guid MaterialVariantId { get; set; }
    public MaterialVariant MaterialVariant { get; set; } = null!;

    public string BatchNumber { get; set; } = string.Empty;

    public Guid GRNLineItemId { get; set; }
    public GRNLineItem GRNLineItem { get; set; } = null!;

    public Guid VendorId { get; set; }
    public Vendor Vendor { get; set; } = null!;

    public decimal QuantityReceived { get; set; }
    public decimal QuantityAvailable { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityConsumed { get; set; }

    public decimal UnitPrice { get; set; }
    public QualityStatus QualityStatus { get; set; } = QualityStatus.PendingQC;

    public ICollection<StockTransaction> Transactions { get; set; } = [];
    public ICollection<SRSBatchAllocation> SrsAllocations { get; set; } = [];
}


