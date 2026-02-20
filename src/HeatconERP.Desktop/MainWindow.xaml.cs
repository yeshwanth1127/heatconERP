using System.Windows;
using System.Windows.Controls;
using HeatconERP.Desktop.Services;

namespace HeatconERP.Desktop;

public partial class MainWindow : Window
{
    private ApiClient? _apiClient;

    public MainWindow()
    {
        InitializeComponent();
        OutputText.Text = "Enter API base URL and click 'Connect to API' to test the connection.";
    }

    private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
    {
        var url = ApiUrlBox.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            OutputText.Text = "Please enter the API base URL (e.g. http://localhost:5212)";
            return;
        }

        _apiClient = new ApiClient(url);
        OutputText.Text = "Connecting...";

        try
        {
            var result = await _apiClient.GetHealthStatusAsync();
            OutputText.Text = $"Health check response:\n{result}";
        }
        catch (Exception ex)
        {
            OutputText.Text = $"Connection failed: {ex.Message}\n\nMake sure the API is running (dotnet run --project src/HeatconERP.API)";
        }
    }

    private async void GetUsersBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_apiClient == null)
        {
            var url = ApiUrlBox.Text?.Trim() ?? "http://localhost:5212";
            _apiClient = new ApiClient(url);
        }

        OutputText.Text = "Fetching users...";

        try
        {
            var result = await _apiClient.GetUsersAsync();
            OutputText.Text = $"Users response:\n{result}";
        }
        catch (Exception ex)
        {
            OutputText.Text = $"Error: {ex.Message}";
        }
    }
}
