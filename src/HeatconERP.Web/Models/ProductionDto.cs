namespace HeatconERP.Web.Models;

public record WorkOrderDto(Guid Id, string OrderNumber, string Stage, string Status, DateTime CreatedAt);
