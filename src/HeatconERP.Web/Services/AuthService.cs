using System.Text.Json;
using HeatconERP.Web.Models;
using Microsoft.JSInterop;

namespace HeatconERP.Web.Services;

public class AuthService
{
    private const string StorageKey = "heatcon_auth";
    private AuthUser? _currentUser;
    private string _baseUrl = "http://localhost:5212";
    private bool _isInitialized;

    private readonly IJSRuntime _js;

    public AuthService(IJSRuntime js)
    {
        _js = js;
    }

    public AuthUser? CurrentUser => _currentUser;
    public string BaseUrl => _baseUrl;
    public bool IsLoggedIn => _currentUser != null;
    public bool IsInitializing => !_isInitialized;

    public event Action? OnAuthStateChanged;

    public void SetBaseUrl(string baseUrl) => _baseUrl = baseUrl.TrimEnd('/');

    public void Login(AuthUser user, string baseUrl)
    {
        _currentUser = user;
        _baseUrl = baseUrl.TrimEnd('/');
        _ = PersistSessionAsync();
        OnAuthStateChanged?.Invoke();
    }

    public async Task LoginAsync(AuthUser user, string baseUrl)
    {
        _currentUser = user;
        _baseUrl = baseUrl.TrimEnd('/');
        await PersistSessionAsync();
        OnAuthStateChanged?.Invoke();
    }

    public void Logout()
    {
        _currentUser = null;
        // Keep base URL (now configured via appsettings); only clear user session.
        _ = ClearSessionAsync();
        OnAuthStateChanged?.Invoke();
    }

    public async Task RestoreSessionAsync()
    {
        if (_isInitialized) return;
        try
        {
            var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonSerializer.Deserialize<StoredSession>(json);
                if (data?.User != null && !string.IsNullOrEmpty(data.BaseUrl))
                {
                    _currentUser = data.User;
                    _baseUrl = data.BaseUrl.TrimEnd('/');
                }
            }
        }
        catch (Exception)
        {
            // sessionStorage or JS interop not available (e.g. prerender)
        }
        finally
        {
            _isInitialized = true;
            OnAuthStateChanged?.Invoke();
        }
    }

    public async Task EnsureSessionPersistedAsync() => await PersistSessionAsync();

    private async Task PersistSessionAsync()
    {
        if (_currentUser == null || string.IsNullOrEmpty(_baseUrl)) return;
        try
        {
            var data = new StoredSession { User = _currentUser, BaseUrl = _baseUrl };
            var json = JsonSerializer.Serialize(data);
            await _js.InvokeVoidAsync("heatconStorage.set", StorageKey, json);
        }
        catch (JSException) { /* ignore */ }
    }

    private async Task ClearSessionAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("heatconStorage.remove", StorageKey);
        }
        catch (JSException) { /* ignore */ }
    }

    private sealed class StoredSession
    {
        public AuthUser? User { get; set; }
        public string? BaseUrl { get; set; }
    }
}
