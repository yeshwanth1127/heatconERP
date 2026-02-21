namespace HeatconERP.Web.Models;

public record WorkOrderListDto(
    Guid Id,
    string OrderNumber,
    string Stage,
    string Status,
    string? AssignedToUserName,
    DateTime CreatedAt,
    Guid? PurchaseInvoiceId);

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

