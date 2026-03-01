namespace HeatconERP.Web.Models;

public record InventorySummaryDto(
    decimal TotalReceived,
    decimal TotalAvailable,
    decimal TotalReserved,
    decimal TotalConsumed,
    decimal IncomingOrderedQuantity);

public record BatchHistoryDto(
    Guid StockBatchId,
    string BatchNumber,
    string Vendor,
    string InvoiceNumber,
    Guid GrnId,
    IReadOnlyList<Guid> LinkedWorkOrders);

public record VariantStockSummaryDto(
    Guid MaterialVariantId,
    decimal TotalReceived,
    decimal TotalAvailable,
    decimal TotalReserved,
    decimal TotalConsumed);

public record BatchTreeNodeDto(Guid Id, string Name, string? Description, IReadOnlyList<BatchTreeGradeNodeDto> Grades);

public record BatchTreeGradeNodeDto(string Grade, IReadOnlyList<BatchTreeVariantNodeDto> Variants);

public record BatchTreeVariantNodeDto(
    Guid Id,
    string SKU,
    string Grade,
    string Size,
    string Unit,
    IReadOnlyList<BatchTreeBatchDto> Batches);

public record BatchTreeBatchDto(
    Guid Id,
    string BatchNumber,
    string Vendor,
    string InvoiceNumber,
    Guid GrnId,
    decimal QuantityReceived,
    decimal QuantityAvailable,
    decimal QuantityReserved,
    decimal QuantityConsumed,
    IReadOnlyList<Guid> LinkedWorkOrders);
