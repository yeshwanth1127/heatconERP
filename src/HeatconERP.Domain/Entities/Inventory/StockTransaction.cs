using HeatconERP.Domain.Entities;
using HeatconERP.Domain.Enums.Inventory;

namespace HeatconERP.Domain.Entities.Inventory;

public class StockTransaction : BaseEntity
{
    public Guid StockBatchId { get; set; }
    public StockBatch StockBatch { get; set; } = null!;

    public StockTransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }

    public Guid? LinkedWorkOrderId { get; set; }
    public WorkOrder? LinkedWorkOrder { get; set; }

    public Guid? LinkedSRSId { get; set; }
    public SRS? LinkedSRS { get; set; }

    public string? Notes { get; set; }
}


