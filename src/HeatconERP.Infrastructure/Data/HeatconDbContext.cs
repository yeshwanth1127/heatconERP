using HeatconERP.Application.Abstractions;
using HeatconERP.Domain.Entities;
using HeatconERP.Domain.Entities.Inventory;
using HeatconERP.Domain.Enums.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HeatconERP.Infrastructure.Data;

public class HeatconDbContext : DbContext, IHeatconDbContext
{
    public HeatconDbContext(DbContextOptions<HeatconDbContext> options)
        : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Enquiry> Enquiries => Set<Enquiry>();
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationLineItem> QuotationLineItems => Set<QuotationLineItem>();
    public DbSet<QuotationRevision> QuotationRevisions => Set<QuotationRevision>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLineItem> PurchaseOrderLineItems => Set<PurchaseOrderLineItem>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseInvoiceLineItem> PurchaseInvoiceLineItems => Set<PurchaseInvoiceLineItem>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderLineItem> WorkOrderLineItems => Set<WorkOrderLineItem>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<PendingApproval> PendingApprovals => Set<PendingApproval>();
    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();

    // Inventory & Procurement (batch traceability)
    public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
    public DbSet<MaterialVariant> MaterialVariants => Set<MaterialVariant>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<VendorPurchaseOrder> VendorPurchaseOrders => Set<VendorPurchaseOrder>();
    public DbSet<VendorPurchaseOrderLineItem> VendorPurchaseOrderLineItems => Set<VendorPurchaseOrderLineItem>();
    public DbSet<GRN> GRNs => Set<GRN>();
    public DbSet<GRNLineItem> GRNLineItems => Set<GRNLineItem>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<WorkOrderMaterialRequirement> WorkOrderMaterialRequirements => Set<WorkOrderMaterialRequirement>();
    public DbSet<SRS> SRSs => Set<SRS>();
    public DbSet<SRSLineItem> SRSLineItems => Set<SRSLineItem>();
    public DbSet<SRSBatchAllocation> SRSBatchAllocations => Set<SRSBatchAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .Property(u => u.Role)
            .HasConversion<string>();

        modelBuilder.Entity<WorkOrder>()
            .Property(w => w.Stage)
            .HasConversion<string>();

        modelBuilder.Entity<WorkOrder>()
            .HasOne(w => w.PurchaseInvoice)
            .WithMany()
            .HasForeignKey(w => w.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WorkOrderLineItem>()
            .HasOne(li => li.WorkOrder)
            .WithMany(w => w.LineItems)
            .HasForeignKey(li => li.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuotationLineItem>()
            .HasOne(li => li.Quotation)
            .WithMany(q => q.LineItems)
            .HasForeignKey(li => li.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QuotationRevision>()
            .HasOne(r => r.Quotation)
            .WithMany(q => q.Revisions)
            .HasForeignKey(r => r.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(p => p.Quotation)
            .WithMany()
            .HasForeignKey(p => p.QuotationId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrder>()
            .HasOne(p => p.QuotationRevision)
            .WithMany()
            .HasForeignKey(p => p.QuotationRevisionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PurchaseOrderLineItem>()
            .HasOne(li => li.PurchaseOrder)
            .WithMany(p => p.LineItems)
            .HasForeignKey(li => li.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PurchaseInvoice>()
            .HasOne(i => i.PurchaseOrder)
            .WithMany()
            .HasForeignKey(i => i.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PurchaseInvoiceLineItem>()
            .HasOne(li => li.PurchaseInvoice)
            .WithMany(i => i.LineItems)
            .HasForeignKey(li => li.PurchaseInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        ConfigureInventory(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyBaseEntityRules();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyBaseEntityRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyBaseEntityRules()
    {
        var now = DateTime.UtcNow;
        foreach (var e in ChangeTracker.Entries<BaseEntity>())
        {
            if (e.State == EntityState.Added)
            {
                if (e.Entity.Id == Guid.Empty) e.Entity.Id = Guid.NewGuid();
                e.Entity.CreatedAt = now;
                e.Entity.UpdatedAt = null;
                e.Entity.IsDeleted = false;
                e.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
            else if (e.State == EntityState.Modified)
            {
                e.Entity.UpdatedAt = now;
                e.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
            else if (e.State == EntityState.Deleted)
            {
                // Never hard delete (for BaseEntity types). Convert to soft delete.
                e.State = EntityState.Modified;
                e.Entity.IsDeleted = true;
                e.Entity.UpdatedAt = now;
                e.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }
    }

    private static void ConfigureInventory(ModelBuilder modelBuilder)
    {
        // Soft delete query filters (module-only)
        modelBuilder.Entity<MaterialCategory>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<MaterialVariant>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<Vendor>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<VendorPurchaseOrder>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<VendorPurchaseOrderLineItem>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<GRN>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<GRNLineItem>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<StockBatch>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<StockTransaction>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<WorkOrderMaterialRequirement>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<SRS>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<SRSLineItem>().HasQueryFilter(x => !x.IsDeleted);
        modelBuilder.Entity<SRSBatchAllocation>().HasQueryFilter(x => !x.IsDeleted);

        // Concurrency token (module-only)
        modelBuilder.Entity<MaterialCategory>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<MaterialVariant>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<Vendor>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<VendorPurchaseOrder>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<VendorPurchaseOrderLineItem>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<GRN>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<GRNLineItem>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<StockBatch>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<StockTransaction>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<WorkOrderMaterialRequirement>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<SRS>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<SRSLineItem>().Property(x => x.RowVersion).IsConcurrencyToken();
        modelBuilder.Entity<SRSBatchAllocation>().Property(x => x.RowVersion).IsConcurrencyToken();

        // Enum conversions
        modelBuilder.Entity<VendorPurchaseOrder>().Property(x => x.Status).HasConversion<string>();
        modelBuilder.Entity<GRNLineItem>().Property(x => x.QualityStatus).HasConversion<string>();
        modelBuilder.Entity<StockBatch>().Property(x => x.QualityStatus).HasConversion<string>();
        modelBuilder.Entity<StockTransaction>().Property(x => x.TransactionType).HasConversion<string>();
        modelBuilder.Entity<SRS>().Property(x => x.Status).HasConversion<string>();

        // Constraints / indexes
        modelBuilder.Entity<MaterialVariant>().HasIndex(x => x.SKU).IsUnique();
        modelBuilder.Entity<StockBatch>().HasIndex(x => new { x.MaterialVariantId, x.BatchNumber }).IsUnique();

        // Relationships (Restrict deletes; never cascade in inventory)
        modelBuilder.Entity<MaterialCategory>()
            .HasMany(x => x.Variants)
            .WithOne(x => x.MaterialCategory)
            .HasForeignKey(x => x.MaterialCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaterialVariant>()
            .HasMany(x => x.StockBatches)
            .WithOne(x => x.MaterialVariant)
            .HasForeignKey(x => x.MaterialVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vendor>()
            .HasMany(x => x.PurchaseOrders)
            .WithOne(x => x.Vendor)
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VendorPurchaseOrder>()
            .HasMany(x => x.LineItems)
            .WithOne(x => x.VendorPurchaseOrder)
            .HasForeignKey(x => x.VendorPurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VendorPurchaseOrderLineItem>()
            .HasOne(x => x.MaterialVariant)
            .WithMany()
            .HasForeignKey(x => x.MaterialVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VendorPurchaseOrder>()
            .HasMany(x => x.Grns)
            .WithOne(x => x.VendorPurchaseOrder)
            .HasForeignKey(x => x.VendorPurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GRN>()
            .HasMany(x => x.LineItems)
            .WithOne(x => x.GRN)
            .HasForeignKey(x => x.GRNId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GRNLineItem>()
            .HasOne(x => x.MaterialVariant)
            .WithMany()
            .HasForeignKey(x => x.MaterialVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GRNLineItem>()
            .HasOne(x => x.StockBatch)
            .WithOne(x => x.GRNLineItem)
            .HasForeignKey<StockBatch>(x => x.GRNLineItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockBatch>()
            .HasOne(x => x.Vendor)
            .WithMany()
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockBatch>()
            .HasMany(x => x.Transactions)
            .WithOne(x => x.StockBatch)
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransaction>()
            .HasOne(x => x.LinkedWorkOrder)
            .WithMany()
            .HasForeignKey(x => x.LinkedWorkOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockTransaction>()
            .HasOne(x => x.LinkedSRS)
            .WithMany()
            .HasForeignKey(x => x.LinkedSRSId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkOrderMaterialRequirement>()
            .HasOne(x => x.MaterialVariant)
            .WithMany()
            .HasForeignKey(x => x.MaterialVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SRS>()
            .HasMany(x => x.LineItems)
            .WithOne(x => x.SRS)
            .HasForeignKey(x => x.SRSId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SRSLineItem>()
            .HasOne(x => x.MaterialVariant)
            .WithMany()
            .HasForeignKey(x => x.MaterialVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SRSLineItem>()
            .HasMany(x => x.BatchAllocations)
            .WithOne(x => x.SRSLineItem)
            .HasForeignKey(x => x.SRSLineItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SRSBatchAllocation>()
            .HasOne(x => x.StockBatch)
            .WithMany(x => x.SrsAllocations)
            .HasForeignKey(x => x.StockBatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
