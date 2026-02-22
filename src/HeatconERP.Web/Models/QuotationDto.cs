namespace HeatconERP.Web.Models;

public record QuotationListDto(
    Guid Id,
    string ReferenceNumber,
    Guid? EnquiryId,
    string? EnquiryNumber,
    string? ClientName,
    string? ProjectName,
    string Version,
    DateTime CreatedAt,
    decimal? Amount,
    string Status);

public record QuotationDetailDto(
    Guid Id,
    string ReferenceNumber,
    Guid? EnquiryId,
    string? EnquiryNumber,
    string? ClientName,
    string? ProjectName,
    string? Description,
    string? Attachments,
    decimal? ManualPrice,
    string? PriceBreakdown,
    string Version,
    DateTime CreatedAt,
    decimal? Amount,
    string Status,
    string? CreatedByUserName,
    IReadOnlyList<QuotationLineItemDto> LineItems,
    IReadOnlyList<QuotationRevisionDto> Revisions);

public record QuotationLineItemDto(
    Guid Id,
    int SortOrder,
    string PartNumber,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    string? AttachmentPath);

public record QuotationRevisionDto(
    Guid Id,
    string Version,
    string Action,
    string ChangedBy,
    DateTime ChangedAt,
    string? ChangeDetails,
    string? AttachmentFileName,
    DateTime? SentToCustomerAt,
    string? SentToCustomerBy);

public record QuotationRevisionDetailDto(
    Guid Id,
    Guid QuotationId,
    string ReferenceNumber,
    Guid? EnquiryId,
    string? EnquiryNumber,
    string Version,
    string Action,
    string ChangedBy,
    DateTime ChangedAt,
    string? ChangeDetails,
    string? ClientName,
    string? ProjectName,
    string? Description,
    string? Attachments,
    decimal? ManualPrice,
    string? PriceBreakdown,
    string? Status,
    decimal? Amount,
    IReadOnlyList<QuotationLineItemDto> LineItems,
    DateTime? SentToCustomerAt,
    string? SentToCustomerBy);

public record SendRevisionResponse(
    QuotationRevisionDto Revision,
    PurchaseOrderCreatedDto PurchaseOrder);

public record PurchaseOrderCreatedDto(
    Guid Id,
    string OrderNumber,
    Guid? QuotationId,
    Guid? QuotationRevisionId,
    string Status,
    decimal? Value);
