namespace HeatconERP.Web.Models;

public record SrsDto(Guid Id, Guid WorkOrderId, string Status);

public record CreateSrsRequest(Guid WorkOrderId, List<CreateSrsLine> Lines);
public record CreateSrsLine(Guid MaterialVariantId, decimal RequiredQuantity);

public record SrsListDto(Guid Id, Guid WorkOrderId, string WorkOrderNumber, string Status, DateTime CreatedAt);

public record SrsDetailDto(
    Guid Id,
    Guid WorkOrderId,
    string WorkOrderNumber,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<SrsLineItemDetailDto> LineItems);

public record SrsLineItemDetailDto(
    Guid Id,
    Guid MaterialVariantId,
    string Sku,
    string Grade,
    string Size,
    string Unit,
    decimal RequiredQuantity,
    IReadOnlyList<SrsAllocationDto> Allocations);

public record SrsAllocationDto(Guid Id, Guid StockBatchId, string BatchNumber, decimal ReservedQuantity, decimal ConsumedQuantity);


