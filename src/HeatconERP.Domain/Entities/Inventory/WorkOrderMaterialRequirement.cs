namespace HeatconERP.Domain.Entities.Inventory;

public class WorkOrderMaterialRequirement : BaseEntity
{
    public Guid WorkOrderId { get; set; }

    public Guid MaterialVariantId { get; set; }
    public MaterialVariant MaterialVariant { get; set; } = null!;

    public decimal RequiredQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
}


