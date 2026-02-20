using System.Windows.Controls;

namespace HeatconERP.Desktop.Views;

public partial class PlaceholderModuleView : UserControl
{
    public PlaceholderModuleView(string moduleKey)
    {
        InitializeComponent();
        ModuleTitle.Text = moduleKey switch
        {
            "enquiries" => "Enquiries",
            "quotations" => "Quotations",
            "purchaseorders" => "Purchase Orders",
            "workorders" => "Work Orders",
            "production" => "Production",
            "quality" => "Quality/RCA",
            "inventory" => "Inventory",
            "vendors" => "Vendors",
            "reports" => "Reports",
            _ => moduleKey
        };
    }
}
