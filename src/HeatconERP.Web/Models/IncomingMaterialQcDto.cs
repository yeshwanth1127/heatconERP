namespace HeatconERP.Web.Models;

public record GrnQcListDto(
    Guid Id,
    string VendorName,
    DateTime ReceivedDate,
    string InvoiceNumber,
    int TotalLines,
    int PendingLines,
    string QcStatus); // "Pending", "Partial", "Complete"

public record GrnQcDetailDto(
    Guid Id,
    string VendorName,
    string InvoiceNumber,
    DateTime ReceivedDate,
    string QcStatus,
    List<GrnQcLineDto> Lines);

public record GrnQcLineDto(
    Guid Id,
    string Sku,
    string Grade,
    string Size,
    string Unit,
    string BatchNumber,
    decimal QuantityReceived,
    decimal UnitPrice,
    string QcStatus); // "PendingQC", "Approved", "Rejected"

public record ApproveGrnLineRequest(string? Notes, string? ApprovedBy);
public record RejectGrnLineRequest(string Reason, string? NcrDescription, string? RejectedBy);
public record ApproveGrnLineResult(bool Approved, Guid? BatchId);
