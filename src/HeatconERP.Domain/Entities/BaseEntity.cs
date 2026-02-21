namespace HeatconERP.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    // Optimistic concurrency token.
    // In PostgreSQL this is NOT auto-updated like SQL Server rowversion; we'll stamp it in DbContext SaveChanges.
    public byte[] RowVersion { get; set; } = [];
}


