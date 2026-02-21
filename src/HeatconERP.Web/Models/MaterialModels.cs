namespace HeatconERP.Web.Models;

public record MaterialCategoryDto(Guid Id, string Name, string? Description);

public record MaterialVariantDto(
    Guid Id,
    Guid MaterialCategoryId,
    string Grade,
    string Size,
    string Unit,
    string SKU,
    decimal MinimumStockLevel);


