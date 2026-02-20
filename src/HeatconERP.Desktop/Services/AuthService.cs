using HeatconERP.Desktop.Models;

namespace HeatconERP.Desktop.Services;

public class AuthService
{
    private AuthUser? _currentUser;
    private string _baseUrl = "http://localhost:5212";

    public AuthUser? CurrentUser => _currentUser;
    public string BaseUrl => _baseUrl;
    public bool IsLoggedIn => _currentUser != null;

    public void SetBaseUrl(string baseUrl) => _baseUrl = baseUrl.TrimEnd('/');

    public void Login(AuthUser user, string baseUrl)
    {
        _currentUser = user;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public void Logout()
    {
        _currentUser = null;
    }
}
