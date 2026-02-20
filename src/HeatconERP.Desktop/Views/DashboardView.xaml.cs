using System.Windows.Controls;
using HeatconERP.Desktop.Controls;
using HeatconERP.Desktop.Services;

namespace HeatconERP.Desktop.Views;

public partial class DashboardView : UserControl
{
    public DashboardView(AuthService authService)
    {
        InitializeComponent();
        LoadCards(authService.CurrentUser?.Role ?? "");
    }

    private void LoadCards(string role)
    {
        var cards = role switch
        {
            "MD" => new[] { ("Pending Approvals", "0"), ("RCA Sign-offs", "0"), ("Key Metrics", "-"), ("Recent Activity", "-") },
            "GM" => new[] { ("Quotation Approvals", "0"), ("Operational Overview", "-"), ("Escalations", "0") },
            "FrontOffice" => new[] { ("New Enquiries", "0"), ("Today's Tasks", "0"), ("Quick Entry", "-") },
            "CRMManager" => new[] { ("Enquiries to Review", "0"), ("Quotations Pending Approval", "0"), ("Team Activity", "-") },
            "CRMStaff" => new[] { ("My Enquiries", "0"), ("My Quotations", "0"), ("Follow-ups", "0") },
            "ProductionManager" => new[] { ("Active Work Orders", "0"), ("Production Status", "-"), ("QC Issues", "0"), ("Staff Assignment", "-") },
            "ProductionStaff" => new[] { ("My Tasks", "0"), ("Work Order Progress", "-") },
            "QualityCheck" => new[] { ("QC Queue", "0"), ("RCA Pending", "0"), ("Inspection Log", "-") },
            "DispatchStoreManager" => new[] { ("Low Stock", "0"), ("GRN Pending", "0"), ("Dispatch Queue", "0") },
            _ => new[] { ("Welcome", "Select a role") }
        };

        foreach (var (title, value) in cards)
        {
            CardsPanel.Children.Add(new DashboardCard { Title = title, Value = value });
        }
    }
}
