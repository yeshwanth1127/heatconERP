namespace HeatconERP.Web.Models;

public record PurchaseInvoiceListDto(
    Guid Id,
    string InvoiceNumber,
    Guid PurchaseOrderId,
    string? PurchaseOrderNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    string CreatedByUserName);

public record PurchaseInvoiceDetailDto(
    Guid Id,
    string InvoiceNumber,
    Guid PurchaseOrderId,
    string? PurchaseOrderNumber,
    string? QuotationReferenceNumber,
    string? QuotationVersion,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    string CreatedByUserName,
    IReadOnlyList<PurchaseInvoiceLineItemDto> LineItems);

public record PurchaseInvoiceLineItemDto(
    Guid Id,
    int SortOrder,
    string PartNumber,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal? LineTotal);

public record CreatePurchaseInvoiceRequest(
    Guid PurchaseOrderId,
    string? InvoiceNumber,
    DateTime InvoiceDate,
    DateTime? DueDate,
    string? Status,
    string? CreatedByUserName);

public record UpdatePurchaseInvoiceRequest(
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    string? Status,
    List<PurchaseInvoiceLineItemInput>? LineItems);

public record PurchaseInvoiceLineItemInput(string? PartNumber, string? Description, int Quantity, decimal UnitPrice, decimal TaxPercent);
