using HeatconERP.Domain.Entities.Inventory;

namespace HeatconERP.Application.Services.Inventory;

public interface IInventoryService
{
    Task<VariantStockSummaryDto> GetVariantStockSummaryAsync(Guid materialVariantId, CancellationToken ct = default);
    Task<StockBatch?> GetBatchDetailsAsync(Guid stockBatchId, CancellationToken ct = default);

    Task<IReadOnlyList<StockAllocationResult>> ReserveStockFifoAsync(Guid materialVariantId, decimal quantity, Guid? linkedWorkOrderId, Guid? linkedSrsId, string? notes, CancellationToken ct = default);
    Task ReleaseReservationAsync(Guid materialVariantId, decimal quantity, Guid? linkedWorkOrderId, Guid? linkedSrsId, string? notes, CancellationToken ct = default);
    Task ConsumeReservedStockAsync(Guid materialVariantId, decimal quantity, Guid? linkedWorkOrderId, Guid? linkedSrsId, string? notes, CancellationToken ct = default);
}

public record VariantStockSummaryDto(
    Guid MaterialVariantId,
    decimal TotalReceived,
    decimal TotalAvailable,
    decimal TotalReserved,
    decimal TotalConsumed);

public record StockAllocationResult(Guid StockBatchId, string BatchNumber, decimal QuantityAllocated);


