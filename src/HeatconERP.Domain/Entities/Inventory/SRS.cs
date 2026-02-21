using HeatconERP.Domain.Enums.Inventory;

namespace HeatconERP.Domain.Entities.Inventory;

public class SRS : BaseEntity
{
    public Guid WorkOrderId { get; set; }
    public SrsStatus Status { get; set; } = SrsStatus.Pending;

    public ICollection<SRSLineItem> LineItems { get; set; } = [];
}


