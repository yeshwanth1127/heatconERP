namespace HeatconERP.Web.Models;

public record WorkOrderCardDto(
    Guid Id,
    string OrderNumber,
    string Stage,
    string Status,
    string? AssignedToUserName,
    DateTime CreatedAt,
    Guid? PurchaseInvoiceId,
    string? PurchaseInvoiceNumber,
    DateTime? SentToProductionAt,
    string? SentToProductionBy,
    DateTime? ProductionReceivedAt,
    string? ProductionReceivedBy,
    IReadOnlyList<WorkOrderCardLineItemDto> LineItems);

public record WorkOrderCardLineItemDto(
    string PartNumber,
    string Description,
    int Quantity);

public record WorkOrderDetailDto(
    Guid Id,
    string OrderNumber,
    string Stage,
    string Status,
    string? AssignedToUserName,
    DateTime CreatedAt,
    Guid? PurchaseInvoiceId,
    IReadOnlyList<WorkOrderLineItemDto> LineItems);

public record WorkOrderLineItemDto(
    Guid Id,
    int SortOrder,
    string PartNumber,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal? LineTotal);

public record UpdateWorkOrderRequest(string? Stage, string? Status, string? AssignedToUserName);

public record UserListDto(Guid Id, string Username, string Role);

