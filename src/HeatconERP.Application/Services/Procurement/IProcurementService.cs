using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Domain.Enums.Inventory;

namespace HeatconERP.Application.Services.Procurement;

public interface IProcurementService
{
    Task<VendorPurchaseOrder> CreateVendorPoAsync(Guid vendorId, DateTime orderDate, IReadOnlyList<CreateVendorPoLine> lines, CancellationToken ct = default);
    Task<GRN> CreateGrnAsync(Guid vendorPurchaseOrderId, DateTime receivedDate, string invoiceNumber, IReadOnlyList<CreateGrnLine> lines, CancellationToken ct = default);
    Task<StockBatch> ProcessGrnLineAndCreateBatchAsync(Guid grnLineItemId, Guid vendorId, QualityStatus qualityStatus, CancellationToken ct = default);

    // Store direct receipt: auto-create PO + GRN + batches + StockTransaction(GRN) in a single transaction.
    Task<DirectGrnResult> ReceiveDirectGrnAsync(Guid vendorId, DateTime receivedDate, string invoiceNumber, IReadOnlyList<ReceiveDirectGrnLine> lines, CancellationToken ct = default);

    // New store procurement flow (SRS shortage -> Vendor PO -> Vendor Invoice -> GRN draft -> submit)
    Task<VendorPurchaseOrder> CreateVendorPoFromSrsAsync(Guid srsId, Guid vendorId, CancellationToken ct = default);
    Task<VendorPurchaseInvoice> SendVendorPoAndCreateInvoiceAsync(Guid vendorPurchaseOrderId, string? invoiceNumber, DateTime invoiceDate, CancellationToken ct = default);
    Task<GRN> AcceptVendorInvoiceAndCreateGrnDraftAsync(Guid vendorPurchaseInvoiceId, string description, string? decidedBy, CancellationToken ct = default);
    Task RejectVendorInvoiceAsync(Guid vendorPurchaseInvoiceId, string description, string? decidedBy, CancellationToken ct = default);
    Task<IReadOnlyList<VendorInvoiceQcDecision>> GetVendorInvoiceDeclineHistoryAsync(Guid vendorPurchaseInvoiceId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> SubmitGrnDraftAsync(Guid grnId, string? invoiceNumber, DateTime? receivedDate, IReadOnlyList<SubmitGrnDraftLine> lines, CancellationToken ct = default);
    Task<bool> CheckGrnQcAllApprovedAsync(Guid grnId, CancellationToken ct = default);
}

public record CreateVendorPoLine(Guid MaterialVariantId, decimal OrderedQuantity, decimal UnitPrice);
public record CreateGrnLine(Guid MaterialVariantId, string BatchNumber, decimal QuantityReceived, decimal UnitPrice, QualityStatus QualityStatus);

public record ReceiveDirectGrnLine(Guid MaterialVariantId, string BatchNumber, decimal QuantityReceived, decimal UnitPrice);

public record DirectGrnResult(Guid VendorPurchaseOrderId, Guid GrnId, IReadOnlyList<Guid> CreatedBatchIds);

public record SubmitGrnDraftLine(Guid GrnLineItemId, string BatchNumber, decimal QuantityReceived, decimal UnitPrice);


