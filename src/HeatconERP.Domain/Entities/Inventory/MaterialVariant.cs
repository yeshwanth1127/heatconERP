namespace HeatconERP.Domain.Entities.Inventory;

public class MaterialVariant : BaseEntity
{
    public Guid MaterialCategoryId { get; set; }
    public MaterialCategory MaterialCategory { get; set; } = null!;

    public string Grade { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty; // unique
    public decimal MinimumStockLevel { get; set; }

    public ICollection<StockBatch> StockBatches { get; set; } = [];
}


