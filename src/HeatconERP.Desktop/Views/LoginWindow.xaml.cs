using System.Windows;
using HeatconERP.Desktop.Services;

namespace HeatconERP.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _authService = new();

    public LoginWindow()
    {
        InitializeComponent();
        LoginContent.Content = new LoginView(_authService, OnLoginSuccess);
    }

    private void OnLoginSuccess()
    {
        Hide();
        var shell = new MainShell(_authService, OnLogout);
        shell.Closed += (s, e) => Show();
        shell.Show();
    }

    private void OnLogout()
    {
        Show();
        LoginContent.Content = new LoginView(_authService, OnLoginSuccess);
    }
}
