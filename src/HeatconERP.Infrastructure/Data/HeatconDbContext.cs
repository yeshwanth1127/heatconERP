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
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
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
    }
}
