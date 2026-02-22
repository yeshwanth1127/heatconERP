using HeatconERP.Application.Abstractions;
using HeatconERP.Application.Services.Inventory;
using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Domain.Enums.Inventory;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HeatconERP.Application.Services.Procurement;

public class ProcurementService : IProcurementService
{
    private readonly IHeatconDbContext _db;

    public ProcurementService(IHeatconDbContext db) => _db = db;

    // Npgsql maps DateTime -> timestamptz by default and requires UTC for writes.
    // Normalize date inputs so UI/API callers can pass Local/Unspecified safely.
    private static DateTime NormalizeToUtc(DateTime dt) =>
        dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };

    public async Task<VendorPurchaseOrder> CreateVendorPoAsync(Guid vendorId, DateTime orderDate, IReadOnlyList<CreateVendorPoLine> lines, CancellationToken ct = default)
    {
        if (lines.Count == 0) throw new InvalidOperationException("PO must contain at least one line item.");

        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId, ct);
        if (vendor == null) throw new InvalidOperationException("Vendor not found.");

        orderDate = NormalizeToUtc(orderDate);
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

        receivedDate = NormalizeToUtc(receivedDate);
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

        var batch = await CreateBatchFromGrnLineAsync(li, vendorId, qualityStatus, ct);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return batch;
    }

    public async Task<DirectGrnResult> ReceiveDirectGrnAsync(Guid vendorId, DateTime receivedDate, string invoiceNumber, IReadOnlyList<ReceiveDirectGrnLine> lines, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) throw new InvalidOperationException("InvoiceNumber is required.");
        if (lines.Count == 0) throw new InvalidOperationException("GRN must contain at least one line item.");

        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId, ct);
        if (vendor == null) throw new InvalidOperationException("Vendor not found.");

        receivedDate = NormalizeToUtc(receivedDate);

        // Normalize + basic validation
        var normalized = lines.Select(l => new ReceiveDirectGrnLine(
                l.MaterialVariantId,
                (l.BatchNumber ?? "").Trim(),
                l.QuantityReceived,
                l.UnitPrice))
            .ToList();

        if (normalized.Any(l => string.IsNullOrWhiteSpace(l.BatchNumber)))
            throw new InvalidOperationException("BatchNumber is required for all lines.");

        if (normalized.Any(l => l.QuantityReceived <= 0))
            throw new InvalidOperationException("QuantityReceived must be > 0 for all lines.");

        // Prevent duplicate batch numbers in the same request (per variant)
        var dupInReq = normalized
            .GroupBy(x => new { x.MaterialVariantId, Batch = x.BatchNumber.ToUpperInvariant() })
            .Any(g => g.Count() > 1);
        if (dupInReq) throw new InvalidOperationException("Duplicate (Material, BatchNumber) in request.");

        // Ensure material variants exist
        var variantIds = normalized.Select(x => x.MaterialVariantId).Distinct().ToList();
        var existingVariants = await _db.MaterialVariants
            .AsNoTracking()
            .Where(v => variantIds.Contains(v.Id))
            .Select(v => v.Id)
            .ToListAsync(ct);
        if (existingVariants.Count != variantIds.Count)
            throw new InvalidOperationException("One or more materials (variants) not found.");

        // Check existing batches to fail fast with a friendly message (DB unique constraint will also enforce)
        var batchNumbers = normalized.Select(x => x.BatchNumber).Distinct().ToList();
        var existingBatches = await _db.StockBatches
            .AsNoTracking()
            .Where(b => variantIds.Contains(b.MaterialVariantId) && batchNumbers.Contains(b.BatchNumber))
            .Select(b => new { b.MaterialVariantId, b.BatchNumber })
            .ToListAsync(ct);
        var existingSet = existingBatches
            .Select(x => $"{x.MaterialVariantId:N}:{x.BatchNumber.ToUpperInvariant()}")
            .ToHashSet();
        foreach (var l in normalized)
        {
            if (existingSet.Contains($"{l.MaterialVariantId:N}:{l.BatchNumber.ToUpperInvariant()}"))
                throw new InvalidOperationException($"BatchNumber '{l.BatchNumber}' already exists for this material.");
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Auto-create a receipt-driven PO so GRN is still linked to a PO (domain requirement)
        var po = new VendorPurchaseOrder
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            OrderDate = receivedDate,
            Status = VendorPurchaseOrderStatus.Completed
        };
        _db.VendorPurchaseOrders.Add(po);

        // Aggregate PO lines by variant (PO line doesn't track batch). Use weighted avg unit price if multiple batches exist.
        foreach (var g in normalized.GroupBy(x => x.MaterialVariantId))
        {
            var totalQty = g.Sum(x => x.QuantityReceived);
            var totalValue = g.Sum(x => x.QuantityReceived * x.UnitPrice);
            var avgPrice = totalQty == 0 ? 0 : totalValue / totalQty;

            _db.VendorPurchaseOrderLineItems.Add(new VendorPurchaseOrderLineItem
            {
                Id = Guid.NewGuid(),
                VendorPurchaseOrderId = po.Id,
                MaterialVariantId = g.Key,
                OrderedQuantity = totalQty,
                UnitPrice = avgPrice
            });
        }

        var grn = new GRN
        {
            Id = Guid.NewGuid(),
            VendorPurchaseOrderId = po.Id,
            ReceivedDate = receivedDate,
            InvoiceNumber = invoiceNumber.Trim()
        };
        _db.GRNs.Add(grn);

        var createdBatchIds = new List<Guid>();

        foreach (var l in normalized)
        {
            var li = new GRNLineItem
            {
                Id = Guid.NewGuid(),
                GRNId = grn.Id,
                MaterialVariantId = l.MaterialVariantId,
                BatchNumber = l.BatchNumber,
                QuantityReceived = l.QuantityReceived,
                UnitPrice = l.UnitPrice,
                QualityStatus = QualityStatus.Approved
            };
            _db.GRNLineItems.Add(li);

            var batch = await CreateBatchFromGrnLineAsync(li, vendorId, QualityStatus.Approved, ct);
            createdBatchIds.Add(batch.Id);
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return new DirectGrnResult(po.Id, grn.Id, createdBatchIds);
    }

    public async Task<VendorPurchaseOrder> CreateVendorPoFromSrsAsync(Guid srsId, Guid vendorId, CancellationToken ct = default)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == vendorId, ct);
        if (vendor == null) throw new InvalidOperationException("Vendor not found.");

        var srs = await _db.SRSs
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == srsId, ct);
        if (srs == null) throw new InvalidOperationException("SRS not found.");

        if (srs.LineItems.Count == 0) throw new InvalidOperationException("SRS has no line items.");

        var variantIds = srs.LineItems.Select(li => li.MaterialVariantId).Distinct().ToList();

        // Available stock for each variant = sum(Approved batches QuantityAvailable)
        var available = await _db.StockBatches.AsNoTracking()
            .Where(b => variantIds.Contains(b.MaterialVariantId) && b.QualityStatus == QualityStatus.Approved)
            .GroupBy(b => b.MaterialVariantId)
            .Select(g => new { VariantId = g.Key, Available = g.Sum(x => x.QuantityAvailable) })
            .ToDictionaryAsync(x => x.VariantId, x => x.Available, ct);

        var shortages = srs.LineItems
            .GroupBy(li => li.MaterialVariantId)
            .Select(g =>
            {
                var req = g.Sum(x => x.RequiredQuantity);
                var avail = available.GetValueOrDefault(g.Key, 0);
                var shortage = Math.Max(0, req - avail);
                return new { VariantId = g.Key, Shortage = shortage };
            })
            .Where(x => x.Shortage > 0)
            .ToList();

        if (shortages.Count == 0) throw new InvalidOperationException("No shortage found. Stock is already available.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var po = new VendorPurchaseOrder
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            OrderDate = DateTime.UtcNow,
            Status = VendorPurchaseOrderStatus.Ordered
        };
        _db.VendorPurchaseOrders.Add(po);

        foreach (var s in shortages)
        {
            _db.VendorPurchaseOrderLineItems.Add(new VendorPurchaseOrderLineItem
            {
                Id = Guid.NewGuid(),
                VendorPurchaseOrderId = po.Id,
                MaterialVariantId = s.VariantId,
                OrderedQuantity = s.Shortage,
                UnitPrice = 0 // placeholder (can be edited later when pricing is introduced)
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return po;
    }

    public async Task<VendorPurchaseInvoice> SendVendorPoAndCreateInvoiceAsync(Guid vendorPurchaseOrderId, string? invoiceNumber, DateTime invoiceDate, CancellationToken ct = default)
    {
        var po = await _db.VendorPurchaseOrders
            .Include(x => x.Vendor)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == vendorPurchaseOrderId, ct);
        if (po == null) throw new InvalidOperationException("Vendor PO not found.");
        if (po.LineItems.Count == 0) throw new InvalidOperationException("Vendor PO has no line items.");

        invoiceDate = NormalizeToUtc(invoiceDate);
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // If invoice exists already for this PO, return it (idempotent).
        var existing = await _db.VendorPurchaseInvoices
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.VendorPurchaseOrderId == vendorPurchaseOrderId, ct);
        if (existing != null) return existing;

        var year = DateTime.UtcNow.Year;
        var count = await _db.VendorPurchaseInvoices.CountAsync(i => i.CreatedAt.Year == year, ct);
        var defaultNumber = $"VPI-{year % 100}{count + 1:D4}";

        var inv = new VendorPurchaseInvoice
        {
            Id = Guid.NewGuid(),
            VendorPurchaseOrderId = po.Id,
            VendorId = po.VendorId,
            InvoiceNumber = string.IsNullOrWhiteSpace(invoiceNumber) ? defaultNumber : invoiceNumber.Trim(),
            InvoiceDate = invoiceDate,
            Status = VendorInvoiceStatus.Pending
        };
        _db.VendorPurchaseInvoices.Add(inv);

        foreach (var li in po.LineItems)
        {
            _db.VendorPurchaseInvoiceLineItems.Add(new VendorPurchaseInvoiceLineItem
            {
                Id = Guid.NewGuid(),
                VendorPurchaseInvoiceId = inv.Id,
                MaterialVariantId = li.MaterialVariantId,
                Quantity = li.OrderedQuantity,
                UnitPrice = li.UnitPrice
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return inv;
    }

    public async Task<GRN> AcceptVendorInvoiceAndCreateGrnDraftAsync(Guid vendorPurchaseInvoiceId, CancellationToken ct = default)
    {
        var inv = await _db.VendorPurchaseInvoices
            .Include(x => x.VendorPurchaseOrder)
            .Include(x => x.LineItems)
            .FirstOrDefaultAsync(x => x.Id == vendorPurchaseInvoiceId, ct);
        if (inv == null) throw new InvalidOperationException("Vendor invoice not found.");
        if (inv.LineItems.Count == 0) throw new InvalidOperationException("Vendor invoice has no line items.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var existingGrn = await _db.GRNs.FirstOrDefaultAsync(g => g.VendorPurchaseInvoiceId == vendorPurchaseInvoiceId, ct);
        if (existingGrn != null) return existingGrn;

        inv.Status = VendorInvoiceStatus.Accepted;

        var grn = new GRN
        {
            Id = Guid.NewGuid(),
            VendorPurchaseOrderId = inv.VendorPurchaseOrderId,
            VendorPurchaseInvoiceId = inv.Id,
            ReceivedDate = NormalizeToUtc(inv.InvoiceDate),
            InvoiceNumber = inv.InvoiceNumber
        };
        _db.GRNs.Add(grn);

        foreach (var li in inv.LineItems)
        {
            var batchNumber = await GenerateNextBatchNumberForVariantAsync(li.MaterialVariantId, ct);
            _db.GRNLineItems.Add(new GRNLineItem
            {
                Id = Guid.NewGuid(),
                GRNId = grn.Id,
                MaterialVariantId = li.MaterialVariantId,
                BatchNumber = batchNumber,
                QuantityReceived = li.Quantity,
                UnitPrice = li.UnitPrice,
                QualityStatus = QualityStatus.Approved
            });
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return grn;
    }

    public async Task<IReadOnlyList<Guid>> SubmitGrnDraftAsync(Guid grnId, string? invoiceNumber, DateTime? receivedDate, IReadOnlyList<SubmitGrnDraftLine> lines, CancellationToken ct = default)
    {
        if (lines.Count == 0) throw new InvalidOperationException("No GRN lines provided.");

        var grn = await _db.GRNs
            .Include(g => g.VendorPurchaseOrder)
            .ThenInclude(po => po.Vendor)
            .Include(g => g.LineItems)
            .FirstOrDefaultAsync(g => g.Id == grnId, ct);
        if (grn == null) throw new InvalidOperationException("GRN not found.");

        var vendorId = grn.VendorPurchaseOrder.VendorId;

        if (!string.IsNullOrWhiteSpace(invoiceNumber))
            grn.InvoiceNumber = invoiceNumber.Trim();
        if (receivedDate.HasValue && receivedDate.Value != default)
            grn.ReceivedDate = NormalizeToUtc(receivedDate.Value);

        var lineMap = lines.ToDictionary(x => x.GrnLineItemId, x => x);
        foreach (var l in grn.LineItems)
        {
            if (!lineMap.TryGetValue(l.Id, out var upd)) continue;
            l.BatchNumber = (upd.BatchNumber ?? "").Trim();
            l.QuantityReceived = upd.QuantityReceived;
            l.UnitPrice = upd.UnitPrice;
        }

        if (grn.LineItems.Any(l => string.IsNullOrWhiteSpace(l.BatchNumber)))
            throw new InvalidOperationException("BatchNumber is required for all GRN lines.");
        if (grn.LineItems.Any(l => l.QuantityReceived <= 0))
            throw new InvalidOperationException("QuantityReceived must be > 0 for all GRN lines.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var created = new List<Guid>();
        foreach (var li in grn.LineItems)
        {
            var existing = await _db.StockBatches.FirstOrDefaultAsync(b => b.GRNLineItemId == li.Id, ct);
            if (existing != null) continue; // idempotent

            var batch = await CreateBatchFromGrnLineAsync(li, vendorId, li.QualityStatus, ct);
            created.Add(batch.Id);
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return created;
    }

    private async Task<StockBatch> CreateBatchFromGrnLineAsync(GRNLineItem li, Guid vendorId, QualityStatus qualityStatus, CancellationToken ct)
    {
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
            Notes = $"GRN receipt batch created; QC={qualityStatus}"
        });

        // Update line item status
        li.QualityStatus = qualityStatus;

        return batch;
    }

    private async Task<string> GenerateNextBatchNumberForVariantAsync(Guid materialVariantId, CancellationToken ct)
    {
        var sku = await _db.MaterialVariants.AsNoTracking()
            .Where(v => v.Id == materialVariantId)
            .Select(v => v.SKU)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(sku))
            throw new InvalidOperationException("Material variant not found.");

        var prefix = $"{sku}-B";

        var existing = await _db.StockBatches.AsNoTracking()
            .Where(b => b.MaterialVariantId == materialVariantId && b.BatchNumber.StartsWith(prefix))
            .Select(b => b.BatchNumber)
            .ToListAsync(ct);

        var max = 0;
        foreach (var bn in existing)
        {
            if (!bn.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
            var nPart = bn[prefix.Length..];
            if (int.TryParse(nPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > max) max = n;
        }

        return $"{prefix}{(max + 1):D4}";
    }
}


