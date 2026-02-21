using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Domain.Enums.Inventory;

namespace HeatconERP.Application.Services.Procurement;

public interface IProcurementService
{
    Task<VendorPurchaseOrder> CreateVendorPoAsync(Guid vendorId, DateTime orderDate, IReadOnlyList<CreateVendorPoLine> lines, CancellationToken ct = default);
    Task<GRN> CreateGrnAsync(Guid vendorPurchaseOrderId, DateTime receivedDate, string invoiceNumber, IReadOnlyList<CreateGrnLine> lines, CancellationToken ct = default);
    Task<StockBatch> ProcessGrnLineAndCreateBatchAsync(Guid grnLineItemId, Guid vendorId, QualityStatus qualityStatus, CancellationToken ct = default);
}

public record CreateVendorPoLine(Guid MaterialVariantId, decimal OrderedQuantity, decimal UnitPrice);
public record CreateGrnLine(Guid MaterialVariantId, string BatchNumber, decimal QuantityReceived, decimal UnitPrice, QualityStatus QualityStatus);


