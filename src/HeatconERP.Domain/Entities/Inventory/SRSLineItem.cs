namespace HeatconERP.Domain.Entities.Inventory;

public class SRSLineItem : BaseEntity
{
    public Guid SRSId { get; set; }
    public SRS SRS { get; set; } = null!;

    public Guid MaterialVariantId { get; set; }
    public MaterialVariant MaterialVariant { get; set; } = null!;

    public decimal RequiredQuantity { get; set; }

    public ICollection<SRSBatchAllocation> BatchAllocations { get; set; } = [];
}


