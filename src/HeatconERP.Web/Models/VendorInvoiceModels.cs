namespace HeatconERP.Web.Models;

public record VendorInvoiceListItemDto(
    Guid Id,
    Guid VendorId,
    string VendorName,
    Guid VendorPurchaseOrderId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string Status,
    int LineCount,
    decimal TotalQuantity);

public record VendorInvoiceDetailDto(
    Guid Id,
    Guid VendorId,
    string VendorName,
    Guid VendorPurchaseOrderId,
    string InvoiceNumber,
    DateTime InvoiceDate,
    string Status,
    IReadOnlyList<VendorInvoiceLineDto> Lines);

public record VendorInvoiceLineDto(Guid Id, Guid MaterialVariantId, string Sku, string Grade, string Size, string Unit, decimal Quantity, decimal UnitPrice);

public record CreateVendorInvoiceFromPoRequest(string? InvoiceNumber, DateTime InvoiceDate);

public record VendorInvoiceCreatedDto(Guid Id, Guid VendorPurchaseOrderId, string InvoiceNumber, DateTime InvoiceDate, string Status);

public record VendorInvoiceAcceptedDto(Guid VendorPurchaseInvoiceId, Guid GrnId);

public record GrnQcStatusDto(Guid GrnId, bool AllApproved, int PendingCount, int ApprovedCount, int RejectedCount);

public record VendorInvoiceQcDecisionRequest(string Description, string? DecidedBy);

public record VendorInvoiceQcHistoryDto(Guid Id, string Decision, string Description, string DecidedBy, DateTime CreatedAt);

public record VendorPoCreatedDto(Guid Id, Guid VendorId, DateTime OrderDate, string Status);
