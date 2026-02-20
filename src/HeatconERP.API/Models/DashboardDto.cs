namespace HeatconERP.API.Models;

public record DashboardResponse(
    IReadOnlyList<KpiDto> Kpis,
    ChartDto Chart,
    IReadOnlyList<ActivityDto> Activities,
    IReadOnlyList<ApprovalDto> Approvals);

public record KpiDto(string Title, string Value, string Change, string Icon, string IconColor, string ValueColor, string ChangeColor);
public record ChartBarDto(string Label, int Value, int Height);
public record ChartDto(int Total, string UnitLabel, IReadOnlyList<ChartBarDto> Bars);
public record ActivityDto(string Time, string Tag, string TagClass, string TagBorder, string TagColor, string TextClass, string Message, string BgClass);
public record ApprovalDto(string Id, string Module, string Originator, string Created, string Value);
