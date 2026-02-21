namespace HeatconERP.Domain.Entities.Inventory;

public class SRSBatchAllocation : BaseEntity
{
    public Guid SRSLineItemId { get; set; }
    public SRSLineItem SRSLineItem { get; set; } = null!;

    public Guid StockBatchId { get; set; }
    public StockBatch StockBatch { get; set; } = null!;

    public decimal ReservedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
}


