using HeatconERP.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HeatconERP.Application.Abstractions;

public interface IHeatconDbContext
{
    DatabaseFacade Database { get; }

    DbSet<MaterialCategory> MaterialCategories { get; }
    DbSet<MaterialVariant> MaterialVariants { get; }
    DbSet<Vendor> Vendors { get; }
    DbSet<VendorPurchaseOrder> VendorPurchaseOrders { get; }
    DbSet<VendorPurchaseOrderLineItem> VendorPurchaseOrderLineItems { get; }
    DbSet<GRN> GRNs { get; }
    DbSet<GRNLineItem> GRNLineItems { get; }
    DbSet<StockBatch> StockBatches { get; }
    DbSet<StockTransaction> StockTransactions { get; }
    DbSet<WorkOrderMaterialRequirement> WorkOrderMaterialRequirements { get; }
    DbSet<SRS> SRSs { get; }
    DbSet<SRSLineItem> SRSLineItems { get; }
    DbSet<SRSBatchAllocation> SRSBatchAllocations { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}


