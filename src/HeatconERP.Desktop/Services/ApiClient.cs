using System.Net.Http;
using System.Text;
using System.Text.Json;
using HeatconERP.Desktop.Models;

namespace HeatconERP.Desktop.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public ApiClient(string baseUrl = "http://localhost:5212")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _http = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
        // Allow self-signed certs in dev
        _http.DefaultRequestHeaders.Add("User-Agent", "HeatconERP.Desktop/1.0");
    }

    public void SetBaseUrl(string baseUrl)
    {
        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
    }

    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetHealthStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/health", ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            return json;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> GetUsersAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/api/users", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<(AuthUser? User, string? Error)> LoginAsync(string baseUrl, string username, string password, CancellationToken ct = default)
    {
        try
        {
            _http.BaseAddress = new Uri(baseUrl.TrimEnd('/'));
            var body = JsonSerializer.Serialize(new { username, password });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/api/auth/login", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                try
                {
                    var errJson = JsonSerializer.Deserialize<JsonElement>(err);
                    if (errJson.TryGetProperty("error", out var errProp))
                        return (null, errProp.GetString());
                }
                catch { }
                return (null, response.StatusCode == System.Net.HttpStatusCode.Unauthorized ? "Invalid username or password." : err);
            }
            var json = await response.Content.ReadAsStringAsync(ct);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var auth = JsonSerializer.Deserialize<AuthUser>(json, opts);
            return (auth, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
