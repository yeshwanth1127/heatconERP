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


