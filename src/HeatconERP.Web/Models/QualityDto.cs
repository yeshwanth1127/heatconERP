namespace HeatconERP.Web.Models;

public record WorkOrderQualitySummaryDto(Guid WorkOrderId, string WorkOrderNumber, string CurrentStage, IReadOnlyList<QualityGateDto> Gates);

public record QualityGateDto(
    string Stage,
    string GateStatus,
    DateTime? PassedAt,
    string? PassedBy,
    DateTime? FailedAt,
    string? FailedBy,
    string? LastResult,
    DateTime? LastCheckedAt,
    string? LastCheckedBy,
    Guid? OpenNcrId,
    string? OpenNcrStatus);

public record RecordQualityCheckRequest(string Stage, string Result, string? Notes, string? CreatedBy, string? NcrDescription);
public record QualityCheckRecordedDto(Guid QualityCheckId, Guid GateId, string GateStatus);

public record NcrListDto(Guid Id, string Stage, string Status, string Description, DateTime CreatedAt, string CreatedBy, DateTime? ClosedAt, string? ClosedBy, string? Disposition);
public record CloseNcrRequest(string Disposition, string? ClosureNotes, string? ClosedBy);

public record QualityQueuesDto(
    IReadOnlyList<QualityQueueItemDto> AwaitingQc,
    IReadOnlyList<QualityQueueItemDto> FailedQcOpenNcr,
    IReadOnlyList<QualityQueueItemDto> BlockedWorkOrders);

public record QualityQueueItemDto(
    Guid WorkOrderId,
    string WorkOrderNumber,
    string CurrentStage,
    string CurrentGateStatus,
    Guid? OpenNcrId,
    DateTime? LastCheckedAt,
    string? LastResult,
    IReadOnlyList<string>? Blockers);
