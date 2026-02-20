using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using HeatconERP.Desktop.Services;

namespace HeatconERP.Desktop.Views;

public partial class MainShell : Window
{
    private readonly AuthService _authService;
    private readonly Action _onLogout;
    private readonly Dictionary<string, UserControl> _contentMap = new();

    public MainShell(AuthService authService, Action onLogout)
    {
        InitializeComponent();
        _authService = authService;
        _onLogout = onLogout;

        UserInfo.Text = $"{_authService.CurrentUser?.Username} ({_authService.CurrentUser?.Role})";
        LoadNavigation();
        ShowDashboard();
    }

    private void LoadNavigation()
    {
        var role = _authService.CurrentUser?.Role ?? "";
        var items = GetNavItemsForRole(role);

        NavList.Items.Clear();
        foreach (var (label, key) in items)
        {
            NavList.Items.Add(new NavItem(label, key));
        }
        NavList.DisplayMemberPath = "Label";
    }

    private static List<(string Label, string Key)> GetNavItemsForRole(string role)
    {
        var all = new List<(string, string)>
        {
            ("Dashboard", "dashboard"),
            ("Enquiries", "enquiries"),
            ("Quotations", "quotations"),
            ("Purchase Orders", "purchaseorders"),
            ("Work Orders", "workorders"),
            ("Production", "production"),
            ("Quality/RCA", "quality"),
            ("Inventory", "inventory"),
            ("Vendors", "vendors"),
            ("Reports", "reports")
        };

        return role switch
        {
            "MD" => new() { ("Dashboard", "dashboard"), ("Reports", "reports") },
            "GM" => new() { ("Dashboard", "dashboard"), ("Reports", "reports") },
            "FrontOffice" => new() { ("Dashboard", "dashboard"), ("Enquiries", "enquiries") },
            "CRMManager" => new() { ("Dashboard", "dashboard"), ("Enquiries", "enquiries"), ("Quotations", "quotations"), ("Purchase Orders", "purchaseorders"), ("Work Orders", "workorders") },
            "CRMStaff" => new() { ("Dashboard", "dashboard"), ("Enquiries", "enquiries"), ("Quotations", "quotations"), ("Purchase Orders", "purchaseorders") },
            "ProductionManager" => new() { ("Dashboard", "dashboard"), ("Purchase Orders", "purchaseorders"), ("Work Orders", "workorders"), ("Production", "production"), ("Quality/RCA", "quality"), ("Inventory", "inventory") },
            "ProductionStaff" => new() { ("Dashboard", "dashboard"), ("Work Orders", "workorders"), ("Production", "production") },
            "QualityCheck" => new() { ("Dashboard", "dashboard"), ("Work Orders", "workorders"), ("Quality/RCA", "quality") },
            "DispatchStoreManager" => new() { ("Dashboard", "dashboard"), ("Purchase Orders", "purchaseorders"), ("Work Orders", "workorders"), ("Inventory", "inventory"), ("Vendors", "vendors") },
            _ => new() { ("Dashboard", "dashboard") }
        };
    }

    private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (NavList.SelectedItem is NavItem item)
            ShowContent(item.Key);
    }

    private void ShowDashboard()
    {
        HeaderTitle.Text = "Dashboard";
        ContentArea.Content = GetOrCreateContent("dashboard", () => new DashboardView(_authService));
    }

    private void ShowContent(string key)
    {
        HeaderTitle.Text = key switch
        {
            "dashboard" => "Dashboard",
            "enquiries" => "Enquiries",
            "quotations" => "Quotations",
            "purchaseorders" => "Purchase Orders",
            "workorders" => "Work Orders",
            "production" => "Production",
            "quality" => "Quality/RCA",
            "inventory" => "Inventory",
            "vendors" => "Vendors",
            "reports" => "Reports",
            _ => key
        };

        ContentArea.Content = GetOrCreateContent(key, () => new PlaceholderModuleView(key));
    }

    private UserControl GetOrCreateContent(string key, Func<UserControl> factory)
    {
        if (!_contentMap.TryGetValue(key, out var content))
        {
            content = factory();
            _contentMap[key] = content;
        }
        return content;
    }

    private void LogoutBtn_Click(object sender, RoutedEventArgs e)
    {
        _authService.Logout();
        _onLogout();
        Close();
    }

    private record NavItem(string Label, string Key);
}
