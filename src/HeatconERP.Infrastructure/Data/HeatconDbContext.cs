using HeatconERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HeatconERP.Infrastructure.Data;

public class HeatconDbContext : DbContext
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
    }
}
