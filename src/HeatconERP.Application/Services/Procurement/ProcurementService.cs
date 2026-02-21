using HeatconERP.Application.Abstractions;
using HeatconERP.Application.Services.Inventory;
using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Domain.Enums.Inventory;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.Application.Services.Procurement;

public class ProcurementService : IProcurementService
{
    private readonly IHeatconDbContext _db;

    public ProcurementService(IHeatconDbContext db) => _db = db;

    public async Task<VendorPurchaseOrder> CreateVendorPoAsync(Guid vendorId, DateTime orderDate, IReadOnlyList<CreateVendorPoLine> lines, CancellationToken ct = default)
    {
        if (lines.Count == 0) throw new InvalidOperationException("PO must contain at least one line item.");

        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId, ct);
        if (vendor == null) throw new InvalidOperationException("Vendor not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var po = new VendorPurchaseOrder
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            OrderDate = orderDate,
            Status = VendorPurchaseOrderStatus.Ordered
        };
        _db.VendorPurchaseOrders.Add(po);

        foreach (var l in lines)
        {
            _db.VendorPurchaseOrderLineItems.Add(new VendorPurchaseOrderLineItem
            {
                Id = Guid.NewGuid(),
                VendorPurchaseOrderId = po.Id,
                MaterialVariantId = l.MaterialVariantId,
                OrderedQuantity = l.OrderedQuantity,
                UnitPrice = l.UnitPrice
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return po;
    }

    public async Task<GRN> CreateGrnAsync(Guid vendorPurchaseOrderId, DateTime receivedDate, string invoiceNumber, IReadOnlyList<CreateGrnLine> lines, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) throw new InvalidOperationException("InvoiceNumber is required.");
        if (lines.Count == 0) throw new InvalidOperationException("GRN must contain at least one line item.");

        var po = await _db.VendorPurchaseOrders
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.Id == vendorPurchaseOrderId, ct);
        if (po == null) throw new InvalidOperationException("Vendor PO not found.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var grn = new GRN
        {
            Id = Guid.NewGuid(),
            VendorPurchaseOrderId = vendorPurchaseOrderId,
            ReceivedDate = receivedDate,
            InvoiceNumber = invoiceNumber.Trim()
        };
        _db.GRNs.Add(grn);

        foreach (var l in lines)
        {
            if (string.IsNullOrWhiteSpace(l.BatchNumber)) throw new InvalidOperationException("BatchNumber is required.");
            _db.GRNLineItems.Add(new GRNLineItem
            {
                Id = Guid.NewGuid(),
                GRNId = grn.Id,
                MaterialVariantId = l.MaterialVariantId,
                BatchNumber = l.BatchNumber.Trim(),
                QuantityReceived = l.QuantityReceived,
                UnitPrice = l.UnitPrice,
                QualityStatus = l.QualityStatus
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return grn;
    }

    public async Task<StockBatch> ProcessGrnLineAndCreateBatchAsync(Guid grnLineItemId, Guid vendorId, QualityStatus qualityStatus, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var li = await _db.GRNLineItems
            .Include(x => x.GRN)
            .ThenInclude(g => g.VendorPurchaseOrder)
            .FirstOrDefaultAsync(x => x.Id == grnLineItemId, ct);
        if (li == null) throw new InvalidOperationException("GRN line item not found.");

        var existing = await _db.StockBatches.FirstOrDefaultAsync(b => b.GRNLineItemId == grnLineItemId, ct);
        if (existing != null) return existing;

        // Validate uniqueness at app-level (DB constraint also exists)
        var dup = await _db.StockBatches.AnyAsync(b => b.MaterialVariantId == li.MaterialVariantId && b.BatchNumber == li.BatchNumber, ct);
        if (dup) throw new InvalidOperationException("BatchNumber already exists for this material variant.");

        var batch = new StockBatch
        {
            Id = Guid.NewGuid(),
            MaterialVariantId = li.MaterialVariantId,
            BatchNumber = li.BatchNumber,
            GRNLineItemId = li.Id,
            VendorId = vendorId,
            QuantityReceived = li.QuantityReceived,
            QuantityAvailable = qualityStatus == QualityStatus.Approved ? li.QuantityReceived : 0,
            QuantityReserved = 0,
            QuantityConsumed = 0,
            UnitPrice = li.UnitPrice,
            QualityStatus = qualityStatus
        };
        _db.StockBatches.Add(batch);

        // StockTransaction is mandatory for any stock-affecting event.
        _db.StockTransactions.Add(new StockTransaction
        {
            Id = Guid.NewGuid(),
            StockBatchId = batch.Id,
            TransactionType = StockTransactionType.GRN,
            Quantity = li.QuantityReceived,
            Notes = $"GRN {li.GRN.InvoiceNumber} batch created; QC={qualityStatus}"
        });

        // Update line item status
        li.QualityStatus = qualityStatus;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return batch;
    }
}


