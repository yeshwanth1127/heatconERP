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

// Pipeline/Timeline DTOs
public record WorkOrderPipelineDto(
    Guid Id,
    string OrderNumber,
    string Status,
    string CurrentStage,
    DateTime CreatedAt,
    DateTime? AcceptedAt,
    DateTime? WorkStartedAt,
    string? WorkStartedBy,
    DateTime? WorkCompletedAt,
    string? WorkCompletedBy,
    bool IsMaterialsAssigned,
    IReadOnlyList<WorkOrderStageTimelineDto> StageTimeline,
    IReadOnlyList<WorkOrderLineItemDto> LineItems);

public record WorkOrderStageTimelineDto(
    string StageName,
    int StageOrder,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string Status); // "Not Started", "In Progress", "Completed"

public record StartWorkOrderRequest(Guid WorkOrderId);

public record UpdateWorkOrderStatusRequest(Guid WorkOrderId, string Stage);

