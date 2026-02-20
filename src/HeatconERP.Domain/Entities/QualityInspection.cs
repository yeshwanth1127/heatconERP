namespace HeatconERP.Domain.Entities;

public class QualityInspection
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public string WorkOrderNumber { get; set; } = string.Empty;
    public string Result { get; set; } = "Pending"; // Pass, Fail, Retest, Pending
    public string? Notes { get; set; }
    public DateTime InspectedAt { get; set; }
    public string InspectedBy { get; set; } = string.Empty;
}
