namespace HeatconERP.API.Controllers;

public record WorkOrderLineItemDto(
    Guid Id,
    int SortOrder,
    string PartNumber,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal? LineTotal);

