namespace HeatconERP.Domain.Entities.Inventory;

public class MaterialCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<MaterialVariant> Variants { get; set; } = [];
}


