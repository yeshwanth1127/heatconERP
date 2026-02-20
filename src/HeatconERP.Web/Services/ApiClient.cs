using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HeatconERP.Web.Models;

namespace HeatconERP.Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public void SetBaseUrl(string baseUrl)
    {
        // No-op: we use full URLs per request to avoid "Properties can only be modified before sending the first request"
    }

    private static string Url(string baseUrl, string path) => $"{baseUrl.TrimEnd('/')}{path}";

    public async Task<(AuthUser? User, string? Error)> LoginAsync(string baseUrl, string username, string password, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { username, password });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/auth/login"), content, ct);
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

    public async Task<IReadOnlyList<WorkOrderDto>?> GetProductionWorkOrdersAsync(string baseUrl, string? stage = null, CancellationToken ct = default)
    {
        try
        {
            var path = string.IsNullOrEmpty(stage) ? "/api/production/workorders" : $"/api/production/workorders?stage={Uri.EscapeDataString(stage)}";
            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<WorkOrderDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<bool> UpdateWorkOrderStageAsync(string baseUrl, Guid id, string stage, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { stage });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PatchAsync(Url(baseUrl, $"/api/production/{id}/stage"), content, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<IReadOnlyList<QualityInspectionDto>?> GetQualityInspectionsAsync(string baseUrl, string? result = null, CancellationToken ct = default)
    {
        try
        {
            var path = string.IsNullOrEmpty(result) ? "/api/quality/inspections" : $"/api/quality/inspections?result={Uri.EscapeDataString(result)}";
            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<QualityInspectionDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<bool> CreateQualityInspectionAsync(string baseUrl, string workOrderNumber, string result, string? notes, string? inspectedBy, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { workOrderNumber, result, notes, inspectedBy });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/quality/inspections"), content, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> SeedEnquiriesAsync(string baseUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync(Url(baseUrl, "/api/enquiries/seed"), null, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<IReadOnlyList<EnquiryListDto>?> GetEnquiriesAsync(string baseUrl, string? status = null, bool? isAerospace = null, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken ct = default)
    {
        try
        {
            var q = new List<string>();
            if (!string.IsNullOrEmpty(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            if (isAerospace.HasValue) q.Add($"isAerospace={isAerospace.Value}");
            if (dateFrom.HasValue) q.Add($"dateFrom={dateFrom.Value:yyyy-MM-dd}");
            if (dateTo.HasValue) q.Add($"dateTo={dateTo.Value:yyyy-MM-dd}");
            var path = q.Count > 0 ? "/api/enquiries?" + string.Join("&", q) : "/api/enquiries";
            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<EnquiryListDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<EnquiryDetailDto?> GetEnquiryByIdAsync(string baseUrl, Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/enquiries/{id}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<EnquiryDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<DashboardResponse?> GetDashboardAsync(string baseUrl, string role, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/dashboard?role={Uri.EscapeDataString(role)}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<DashboardResponse>(opts, ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<QuotationListDto>?> GetQuotationsAsync(string baseUrl, Guid? enquiryId = null, string? searchId = null, string? client = null, string? status = null, CancellationToken ct = default)
    {
        try
        {
            var q = new List<string>();
            if (enquiryId.HasValue) q.Add($"enquiryId={enquiryId.Value}");
            if (!string.IsNullOrEmpty(searchId)) q.Add($"searchId={Uri.EscapeDataString(searchId)}");
            if (!string.IsNullOrEmpty(client)) q.Add($"client={Uri.EscapeDataString(client)}");
            if (!string.IsNullOrEmpty(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            var path = q.Count > 0 ? "/api/quotations?" + string.Join("&", q) : "/api/quotations";
            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<QuotationListDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<QuotationDetailDto?> GetQuotationByIdAsync(string baseUrl, Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/quotations/{id}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<QuotationDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<QuotationRevisionDetailDto?> GetRevisionByIdAsync(string baseUrl, Guid quotationId, Guid revisionId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/quotations/{quotationId}/revisions/{revisionId}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<QuotationRevisionDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<QuotationDetailDto?> CreateQuotationAsync(string baseUrl, Guid? enquiryId, string? createdBy, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { enquiryId, createdBy });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/quotations"), content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errBody = await response.Content.ReadAsStringAsync(ct);
                throw new HttpRequestException($"API returned {(int)response.StatusCode}: {errBody}");
            }
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<QuotationDetailDto>(opts, ct);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Create quotation failed: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateQuotationAsync(string baseUrl, Guid id, UpdateQuotationInput input, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { input.Status, input.ClientName, input.ProjectName, input.Description, input.Attachments, input.LineItems, input.ChangedBy });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(Url(baseUrl, $"/api/quotations/{id}"), content, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<QuotationDetailDto?> GenerateQuotationRevisionAsync(string baseUrl, Guid id, List<LineItemInput>? lineItems, string? changeDetails, string? changedBy, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { lineItems, changeDetails, changedBy });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, $"/api/quotations/{id}/revision"), content, ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<QuotationDetailDto>(opts, ct);
        }
        catch { return null; }
    }
}

public record LineItemInput(string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath = null);

public record UpdateQuotationInput(string? Status, string? ClientName, string? ProjectName, string? Description, string? Attachments, List<LineItemInput>? LineItems, string? ChangedBy);
