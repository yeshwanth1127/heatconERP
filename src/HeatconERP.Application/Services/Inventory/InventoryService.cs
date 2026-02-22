using HeatconERP.Application.Abstractions;
using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Domain.Enums.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HeatconERP.Application.Services.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IHeatconDbContext _db;

    public InventoryService(IHeatconDbContext db) => _db = db;

    public async Task<VariantStockSummaryDto> GetVariantStockSummaryAsync(Guid materialVariantId, CancellationToken ct = default)
    {
        var batches = await _db.StockBatches
            .AsNoTracking()
            .Where(b => b.MaterialVariantId == materialVariantId)
            .ToListAsync(ct);

        return new VariantStockSummaryDto(
            materialVariantId,
            TotalReceived: batches.Sum(b => b.QuantityReceived),
            TotalAvailable: batches.Sum(b => b.QuantityAvailable),
            TotalReserved: batches.Sum(b => b.QuantityReserved),
            TotalConsumed: batches.Sum(b => b.QuantityConsumed));
    }

    public async Task<StockBatch?> GetBatchDetailsAsync(Guid stockBatchId, CancellationToken ct = default)
    {
        return await _db.StockBatches
            .AsNoTracking()
            .Include(b => b.MaterialVariant)
            .Include(b => b.Vendor)
            .Include(b => b.GRNLineItem)
            .Include(b => b.Transactions)
            .FirstOrDefaultAsync(b => b.Id == stockBatchId, ct);
    }

    public async Task<IReadOnlyList<StockAllocationResult>> ReserveStockFifoAsync(Guid materialVariantId, decimal quantity, Guid? linkedWorkOrderId, Guid? linkedSrsId, string? notes, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be > 0");

        IDbContextTransaction? tx = null;
        var startedHere = _db.Database.CurrentTransaction == null;
        if (startedHere)
            tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var allocations = new List<StockAllocationResult>();
            var remaining = quantity;

            var fifo = await _db.StockBatches
                .Where(b =>
                    b.MaterialVariantId == materialVariantId
                    && b.QualityStatus == QualityStatus.Approved
                    && b.QuantityAvailable > 0)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync(ct);

            foreach (var batch in fifo)
            {
                if (remaining <= 0) break;
                var take = Math.Min(batch.QuantityAvailable, remaining);
                if (take <= 0) continue;

                // Invariants: never directly edit quantities outside a StockTransaction write path.
                _db.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    StockBatchId = batch.Id,
                    TransactionType = StockTransactionType.Reserve,
                    Quantity = take,
                    LinkedWorkOrderId = linkedWorkOrderId,
                    LinkedSRSId = linkedSrsId,
                    Notes = notes
                });

                batch.QuantityAvailable -= take;
                batch.QuantityReserved += take;

                allocations.Add(new StockAllocationResult(batch.Id, batch.BatchNumber, take));
                remaining -= take;
            }

            if (remaining > 0)
                throw new InvalidOperationException($"Insufficient approved stock to reserve. Short by {remaining}.");

            await _db.SaveChangesAsync(ct);
            if (startedHere)
                await tx!.CommitAsync(ct);

            return allocations;
        }
        catch
        {
            if (startedHere && tx != null)
                await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (startedHere && tx != null)
                await tx.DisposeAsync();
        }
    }

    public async Task ReleaseReservationAsync(Guid materialVariantId, decimal quantity, Guid? linkedWorkOrderId, Guid? linkedSrsId, string? notes, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be > 0");

        IDbContextTransaction? tx = null;
        var startedHere = _db.Database.CurrentTransaction == null;
        if (startedHere)
            tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var remaining = quantity;

            // Release in FIFO order from reserved quantities (oldest batches first)
            var batches = await _db.StockBatches
                .Where(b => b.MaterialVariantId == materialVariantId && b.QuantityReserved > 0)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync(ct);

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;
                var take = Math.Min(batch.QuantityReserved, remaining);
                if (take <= 0) continue;

                _db.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    StockBatchId = batch.Id,
                    TransactionType = StockTransactionType.Release,
                    Quantity = take,
                    LinkedWorkOrderId = linkedWorkOrderId,
                    LinkedSRSId = linkedSrsId,
                    Notes = notes
                });

                batch.QuantityReserved -= take;
                batch.QuantityAvailable += take;
                remaining -= take;
            }

            if (remaining > 0)
                throw new InvalidOperationException($"Cannot release {quantity}: only {quantity - remaining} is reserved.");

            await _db.SaveChangesAsync(ct);
            if (startedHere)
                await tx!.CommitAsync(ct);
        }
        catch
        {
            if (startedHere && tx != null)
                await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (startedHere && tx != null)
                await tx.DisposeAsync();
        }
    }

    public async Task ConsumeReservedStockAsync(Guid materialVariantId, decimal quantity, Guid? linkedWorkOrderId, Guid? linkedSrsId, string? notes, CancellationToken ct = default)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be > 0");

        IDbContextTransaction? tx = null;
        var startedHere = _db.Database.CurrentTransaction == null;
        if (startedHere)
            tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            var remaining = quantity;

            // Consume from reserved quantities FIFO (oldest batches first)
            var batches = await _db.StockBatches
                .Where(b => b.MaterialVariantId == materialVariantId && b.QuantityReserved > 0)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync(ct);

            foreach (var batch in batches)
            {
                if (remaining <= 0) break;
                var take = Math.Min(batch.QuantityReserved, remaining);
                if (take <= 0) continue;

                _db.StockTransactions.Add(new StockTransaction
                {
                    Id = Guid.NewGuid(),
                    StockBatchId = batch.Id,
                    TransactionType = StockTransactionType.Consume,
                    Quantity = take,
                    LinkedWorkOrderId = linkedWorkOrderId,
                    LinkedSRSId = linkedSrsId,
                    Notes = notes
                });

                batch.QuantityReserved -= take;
                batch.QuantityConsumed += take;
                remaining -= take;
            }

            if (remaining > 0)
                throw new InvalidOperationException($"Cannot consume {quantity}: only {quantity - remaining} is reserved.");

            await _db.SaveChangesAsync(ct);
            if (startedHere)
                await tx!.CommitAsync(ct);
        }
        catch
        {
            if (startedHere && tx != null)
                await tx.RollbackAsync(ct);
            throw;
        }
        finally
        {
            if (startedHere && tx != null)
                await tx.DisposeAsync();
        }
    }
}


