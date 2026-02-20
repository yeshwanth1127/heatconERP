namespace HeatconERP.Web.Models;

public record QualityInspectionDto(Guid Id, string WorkOrderNumber, string Result, string? Notes, DateTime InspectedAt, string InspectedBy);
