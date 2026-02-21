namespace HeatconERP.Web.Models;

public record PurchaseOrderListDto(
    Guid Id,
    string OrderNumber,
    Guid? QuotationId,
    Guid? QuotationRevisionId,
    string? QuotationReferenceNumber,
    string? QuotationVersion,
    string? SentRevisionVersion,
    string? ClientName,
    string CustomerPONumber,
    DateTime PODate,
    string Status,
    decimal? Value,
    DateTime CreatedAt,
    string CreatedByUserName);

public record PurchaseOrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid? QuotationId,
    Guid? QuotationRevisionId,
    string? QuotationReferenceNumber,
    string? QuotationVersion,
    string? SentRevisionVersion,
    string? ClientName,
    string CustomerPONumber,
    DateTime PODate,
    string? DeliveryTerms,
    string? PaymentTerms,
    string Status,
    decimal? Value,
    DateTime CreatedAt,
    string CreatedByUserName,
    IReadOnlyList<PurchaseOrderLineItemDto> LineItems);

public record PurchaseOrderLineItemDto(
    Guid Id,
    int SortOrder,
    string PartNumber,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    string? AttachmentPath);

public record PurchaseOrderCompareDto(
    Guid PurchaseOrderId,
    string OrderNumber,
    string QuotationReferenceNumber,
    string QuotationVersion,
    IReadOnlyList<PurchaseOrderCompareLineDto> Comparisons);

public record PurchaseOrderCompareLineDto(
    string PartNumber,
    string PoDescription,
    string QuotationDescription,
    bool DescriptionMismatch,
    int PoQuantity,
    int QuotationQuantity,
    bool QuantityMismatch,
    decimal PoUnitPrice,
    decimal QuotationUnitPrice,
    bool PriceMismatch);

public record CreatePurchaseOrderRequest(
    string? OrderNumber,
    Guid? QuotationId,
    Guid? QuotationRevisionId,
    string? CustomerPONumber,
    DateTime PODate,
    string? DeliveryTerms,
    string? PaymentTerms,
    string? Status,
    string? CreatedByUserName,
    List<PurchaseOrderLineItemInput>? LineItems);

public record UpdatePurchaseOrderRequest(
    string? OrderNumber,
    string? CustomerPONumber,
    DateTime? PODate,
    string? DeliveryTerms,
    string? PaymentTerms,
    string? Status,
    List<PurchaseOrderLineItemInput>? LineItems);

public record PurchaseOrderLineItemInput(string? PartNumber, string? Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath = null);
