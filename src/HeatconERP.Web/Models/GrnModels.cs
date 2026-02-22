namespace HeatconERP.Web.Models;

public record ReceiveDirectGrnLine(Guid MaterialVariantId, string BatchNumber, decimal QuantityReceived, decimal UnitPrice);

public record CreateDirectGrnRequest(Guid VendorId, DateTime ReceivedDate, string InvoiceNumber, List<ReceiveDirectGrnLine> Lines);

public record DirectGrnResultDto(Guid GrnId, Guid VendorPurchaseOrderId, IReadOnlyList<Guid> CreatedBatchIds);

public record NextBatchNumberDto(string BatchNumber);

public record SubmitGrnDraftLine(Guid GrnLineItemId, string BatchNumber, decimal QuantityReceived, decimal UnitPrice);

public record SubmitGrnDraftRequest(string? InvoiceNumber, DateTime? ReceivedDate, List<SubmitGrnDraftLine> Lines);

public record SubmitGrnDraftResultDto(Guid GrnId, IReadOnlyList<Guid> CreatedBatchIds);

public record GrnListItemDto(Guid Id, Guid VendorId, string VendorName, string InvoiceNumber, DateTime ReceivedDate, int LineCount, decimal TotalQuantity);

public record GrnDetailDto(
    Guid Id,
    Guid VendorPurchaseOrderId,
    Guid VendorId,
    string VendorName,
    DateTime ReceivedDate,
    string InvoiceNumber,
    IReadOnlyList<GrnLineDto> Lines);

public record GrnLineDto(
    Guid Id,
    Guid MaterialVariantId,
    string Sku,
    string Grade,
    string Size,
    string Unit,
    string BatchNumber,
    decimal QuantityReceived,
    decimal UnitPrice,
    string QualityStatus,
    Guid? StockBatchId);

public record VendorPoListItemDto(Guid Id, Guid VendorId, string VendorName, DateTime OrderDate, string Status, int LineCount, decimal TotalQuantity);

public record VendorPoDetailDto(
    Guid Id,
    Guid VendorId,
    string VendorName,
    DateTime OrderDate,
    string Status,
    IReadOnlyList<VendorPoLineDto> Lines);

public record VendorPoLineDto(Guid Id, Guid MaterialVariantId, string Sku, string Grade, string Size, string Unit, decimal OrderedQuantity, decimal UnitPrice);


