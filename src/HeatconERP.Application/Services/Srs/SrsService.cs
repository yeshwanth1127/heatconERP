using HeatconERP.Application.Abstractions;
using HeatconERP.Application.Services.Inventory;
using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Domain.Enums.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.Application.Services.Srs;

public class SrsService : ISrsService
{
    private readonly IHeatconDbContext _db;
    private readonly IInventoryService _inventory;

    public SrsService(IHeatconDbContext db, IInventoryService inventory)
    {
        _db = db;
        _inventory = inventory;
    }

    public async Task<SRS> CreateSrsFromWorkOrderAsync(Guid workOrderId, IReadOnlyList<CreateSrsLine> lines, CancellationToken ct = default)
    {
        if (lines.Count == 0) throw new InvalidOperationException("SRS must contain at least one line item.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var srs = new SRS
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrderId,
            Status = SrsStatus.Pending
        };
        _db.SRSs.Add(srs);

        foreach (var l in lines)
        {
            _db.SRSLineItems.Add(new SRSLineItem
            {
                Id = Guid.NewGuid(),
                SRSId = srs.Id,
                MaterialVariantId = l.MaterialVariantId,
                RequiredQuantity = l.RequiredQuantity
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return srs;
    }

    public async Task<SRS> ApproveSrsAsync(Guid srsId, string? approvedBy, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var srs = await _db.SRSs.Include(x => x.LineItems).FirstOrDefaultAsync(x => x.Id == srsId, ct);
        if (srs == null) throw new InvalidOperationException("SRS not found.");
        if (srs.Status != SrsStatus.Pending) return srs;

        srs.Status = SrsStatus.Approved;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return srs;
    }

    public async Task AllocateBatchesFifoAsync(Guid srsId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var srs = await _db.SRSs
            .Include(x => x.LineItems)
            .ThenInclude(li => li.BatchAllocations)
            .FirstOrDefaultAsync(x => x.Id == srsId, ct);
        if (srs == null) throw new InvalidOperationException("SRS not found.");
        if (srs.Status != SrsStatus.Approved) throw new InvalidOperationException("SRS must be Approved before allocating.");

        // Allocate per line item using FIFO + approved batches only.
        foreach (var li in srs.LineItems)
        {
            var required = li.RequiredQuantity;
            var alreadyAllocated = li.BatchAllocations.Sum(a => a.ReservedQuantity);
            var toAllocate = required - alreadyAllocated;
            if (toAllocate <= 0) continue;

            var allocations = await _inventory.ReserveStockFifoAsync(li.MaterialVariantId, toAllocate, linkedWorkOrderId: srs.WorkOrderId, linkedSrsId: srs.Id, notes: "SRS allocation", ct: ct);

            foreach (var a in allocations)
            {
                _db.SRSBatchAllocations.Add(new SRSBatchAllocation
                {
                    Id = Guid.NewGuid(),
                    SRSLineItemId = li.Id,
                    StockBatchId = a.StockBatchId,
                    ReservedQuantity = a.QuantityAllocated,
                    ConsumedQuantity = 0
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}


