using System.Windows;
using System.Windows.Controls;
using HeatconERP.Desktop.Services;

namespace HeatconERP.Desktop.Views;

public partial class LoginView : UserControl
{
    private readonly ApiClient _apiClient = new();
    private readonly AuthService _authService;
    private readonly Action _onLoginSuccess;

    public LoginView(AuthService authService, Action onLoginSuccess)
    {
        InitializeComponent();
        _authService = authService;
        _onLoginSuccess = onLoginSuccess;
    }

    private async void LoginBtn_Click(object sender, RoutedEventArgs e)
    {
        var apiUrl = ApiUrlBox.Text?.Trim();
        var username = UsernameBox.Text?.Trim();
        var password = PasswordBox.Password;

        if (string.IsNullOrEmpty(apiUrl))
        {
            ShowError("Please enter API URL.");
            return;
        }
        if (string.IsNullOrEmpty(username))
        {
            ShowError("Please enter username.");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Please enter password.");
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        LoginBtn.IsEnabled = false;

        var (user, error) = await _apiClient.LoginAsync(apiUrl, username, password);

        LoginBtn.IsEnabled = true;

        if (user != null)
        {
            _authService.Login(user, apiUrl);
            _onLoginSuccess();
        }
        else
        {
            ShowError(error ?? "Login failed.");
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
