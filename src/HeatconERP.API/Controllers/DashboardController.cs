using HeatconERP.API.Models;
using HeatconERP.Domain.Entities;
using HeatconERP.Domain.Enums;
using HeatconERP.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HeatconERP.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly HeatconDbContext _db;

    public DashboardController(HeatconDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get([FromQuery] string role, CancellationToken ct)
    {
        var kpis = await GetKpisAsync(role, ct);
        var chart = await GetChartAsync(role, ct);
        var activities = await GetActivitiesAsync(ct);
        var approvals = await GetApprovalsAsync(role, ct);

        return Ok(new DashboardResponse(kpis, chart, activities, approvals));
    }

    private async Task<List<KpiDto>> GetKpisAsync(string role, CancellationToken ct)
    {
        return role switch
        {
            "CRMManager" => await GetCrmManagerKpisAsync(ct),
            "CRMStaff" => await GetCrmStaffKpisAsync(ct),
            _ => GetDefaultKpis()
        };
    }

    private async Task<List<KpiDto>> GetCrmManagerKpisAsync(CancellationToken ct)
    {
        var openEnquiries = await _db.Enquiries.CountAsync(e => e.Status == "Open", ct);
        var quotationsPending = await _db.Quotations.CountAsync(q => q.Status == "Pending", ct);
        var activeOrders = await _db.PurchaseOrders.CountAsync(p => p.Status == "Active", ct);
        var approvalsCount = await _db.PendingApprovals.CountAsync(ct);
        var todayUtc = DateTime.UtcNow.Date;
        var todayActivities = await _db.ActivityLogs.CountAsync(a => a.OccurredAt >= todayUtc && a.OccurredAt < todayUtc.AddDays(1), ct);

        return
        [
            new("Open Enquiries", openEnquiries.ToString(), "-", "chat_bubble", "text-[#195de6]", "text-white", "text-slate-400"),
            new("Quotations Pending", quotationsPending.ToString(), "-", "request_quote", "text-[#195de6]", "text-white", "text-slate-400"),
            new("Active Orders", activeOrders.ToString(), "-", "settings_suggest", "text-[#195de6]", "text-white", "text-slate-400"),
            new("Team Activity", todayActivities.ToString(), "Today", "groups", "text-[#195de6]", "text-white", "text-slate-400"),
            new("Approvals", approvalsCount.ToString(), "-", "verified", "text-[#195de6]", "text-white", "text-slate-400")
        ];
    }

    private async Task<List<KpiDto>> GetCrmStaffKpisAsync(CancellationToken ct)
    {
        var myEnquiries = await _db.Enquiries.CountAsync(e => e.Status == "Open", ct);
        var myQuotations = await _db.Quotations.CountAsync(q => q.Status == "Pending", ct);
        return
        [
            new("My Enquiries", myEnquiries.ToString(), "-", "mail", "text-[#195de6]", "text-white", "text-slate-400"),
            new("My Quotations", myQuotations.ToString(), "Pending", "request_quote", "text-[#195de6]", "text-white", "text-slate-400"),
            new("Follow-ups", "0", "Due", "schedule", "text-rose-500", "text-rose-500", "text-rose-500")
        ];
    }

    private static List<KpiDto> GetDefaultKpis() =>
    [
        new("Key Metric 1", "0", "-", "dashboard", "text-[#195de6]", "text-white", "text-slate-400"),
        new("Key Metric 2", "0", "-", "analytics", "text-[#195de6]", "text-white", "text-slate-400"),
        new("Key Metric 3", "-", "-", "verified", "text-[#195de6]", "text-white", "text-slate-400")
    ];

    private async Task<ChartDto> GetChartAsync(string role, CancellationToken ct)
    {
        if (role is "CRMManager" or "CRMStaff")
        {
            var stages = await _db.WorkOrders
                .Where(w => w.Status == "Active")
                .GroupBy(w => w.Stage)
                .Select(g => new { Stage = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var stageOrder = new[] { WorkOrderStage.Planning, WorkOrderStage.Material, WorkOrderStage.Assembly, WorkOrderStage.Testing, WorkOrderStage.QC, WorkOrderStage.Packing };
            var stageDict = stages.ToDictionary(s => s.Stage, s => s.Count);
            var maxCount = stages.Count > 0 ? stages.Max(s => s.Count) : 1;

            var bars = stageOrder.Select(s =>
            {
                var count = stageDict.GetValueOrDefault(s, 0);
                var height = maxCount > 0 ? (int)((double)count / maxCount * 90) : 0;
                if (height < 5 && count > 0) height = 5;
                return new ChartBarDto(s.ToString(), count, height);
            }).ToList();

            var total = bars.Sum(b => b.Value);
            return new ChartDto(total, "Units in pipeline", bars);
        }

        return new ChartDto(0, "Items", [new ChartBarDto("Total", 0, 100)]);
    }

    private async Task<List<ActivityDto>> GetActivitiesAsync(CancellationToken ct)
    {
        var logs = await _db.ActivityLogs
            .OrderByDescending(a => a.OccurredAt)
            .Take(20)
            .ToListAsync(ct);

        return logs.Select(a => MapActivity(a)).ToList();
    }

    private static ActivityDto MapActivity(ActivityLog a)
    {
        var (tagClass, tagBorder, tagColor, textClass, bgClass) = a.Tag switch
        {
            "SYSTEM" => ("bg-emerald-900/40 text-emerald-400", "border-emerald-800", "text-slate-500", "text-slate-300", ""),
            "AUDIT" => ("bg-[#195de6]/20 text-[#195de6]", "border-[#195de6]/30", "text-slate-500", "text-slate-300", ""),
            "CRITICAL" => ("bg-rose-900/40 text-rose-500", "border-rose-800", "text-rose-500/70", "text-rose-200", "bg-rose-950/10"),
            "USER" => ("bg-[#243047]/50 text-slate-400", "border-[#344465]", "text-slate-500", "text-slate-300", ""),
            "WARN" => ("bg-amber-900/40 text-amber-500", "border-amber-800", "text-slate-500", "text-slate-300", ""),
            _ => ("bg-[#243047]/50 text-slate-400", "border-[#344465]", "text-slate-500", "text-slate-300", "")
        };
        return new ActivityDto(
            a.OccurredAt.ToString("HH:mm:ss"),
            a.Tag,
            tagClass,
            tagBorder,
            tagColor,
            textClass,
            a.Message,
            bgClass ?? "");
    }

    private async Task<List<ApprovalDto>> GetApprovalsAsync(string role, CancellationToken ct)
    {
        if (role is "CRMStaff") return [];

        var items = await _db.PendingApprovals
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        return items.Select(p => new ApprovalDto(
            p.ReferenceId,
            p.Module,
            p.Originator,
            p.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
            p.Value ?? "--")).ToList();
    }
}
