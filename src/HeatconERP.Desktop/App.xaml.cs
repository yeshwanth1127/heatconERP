using System.Windows;
using HeatconERP.Desktop.Views;

namespace HeatconERP.Desktop;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        new LoginWindow().Show();
    }
}

