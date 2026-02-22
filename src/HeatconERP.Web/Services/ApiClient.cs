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

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!string.IsNullOrWhiteSpace(body)) return body.Trim().Trim('"');
        }
        catch { /* ignore */ }
        return $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}".Trim();
    }

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
            var body = JsonSerializer.Serialize(new
            {
                input.Status,
                input.ClientName,
                input.ProjectName,
                input.Description,
                input.Attachments,
                input.ManualPrice,
                input.PriceBreakdown,
                input.LineItems,
                input.ChangedBy
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(Url(baseUrl, $"/api/quotations/{id}"), content, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<QuotationDetailDto?> GenerateQuotationRevisionAsync(string baseUrl, Guid id, List<LineItemInput>? lineItems, decimal? manualPrice, string? priceBreakdown, string? changeDetails, string? changedBy, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { lineItems, manualPrice, priceBreakdown, changeDetails, changedBy });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, $"/api/quotations/{id}/revision"), content, ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<QuotationDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<SendRevisionResponse?> SendRevisionToCustomerAsync(string baseUrl, Guid quotationId, Guid revisionId, string? sentBy, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { sentBy });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, $"/api/quotations/{quotationId}/revisions/{revisionId}/send"), content, ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<SendRevisionResponse>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<PurchaseOrderListDto>?> GetPurchaseOrdersAsync(string baseUrl, Guid? quotationId = null, string? customerPONumber = null, string? status = null, string? client = null, int limit = 100, CancellationToken ct = default)
    {
        try
        {
            var q = new List<string>();
            if (quotationId.HasValue) q.Add($"quotationId={quotationId.Value}");
            if (!string.IsNullOrEmpty(customerPONumber)) q.Add($"customerPONumber={Uri.EscapeDataString(customerPONumber)}");
            if (!string.IsNullOrEmpty(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrEmpty(client)) q.Add($"client={Uri.EscapeDataString(client)}");
            q.Add($"limit={limit}");
            var path = "/api/purchaseorders?" + string.Join("&", q);
            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<PurchaseOrderListDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<PurchaseOrderDetailDto?> GetPurchaseOrderByIdAsync(string baseUrl, Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/purchaseorders/{id}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<PurchaseOrderCompareDto?> GetPurchaseOrderCompareAsync(string baseUrl, Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/purchaseorders/{id}/compare"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<PurchaseOrderCompareDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<PurchaseOrderDetailDto?> CreatePurchaseOrderAsync(string baseUrl, CreatePurchaseOrderRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/purchaseorders"), content, ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<PurchaseOrderDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<bool> UpdatePurchaseOrderAsync(string baseUrl, Guid id, UpdatePurchaseOrderRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(Url(baseUrl, $"/api/purchaseorders/{id}"), content, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<IReadOnlyList<PurchaseInvoiceListDto>?> GetPurchaseInvoicesAsync(string baseUrl, Guid? purchaseOrderId = null, string? status = null, string? invoiceNumber = null, int limit = 100, CancellationToken ct = default)
    {
        try
        {
            var q = new List<string>();
            if (purchaseOrderId.HasValue) q.Add($"purchaseOrderId={purchaseOrderId.Value}");
            if (!string.IsNullOrEmpty(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrEmpty(invoiceNumber)) q.Add($"invoiceNumber={Uri.EscapeDataString(invoiceNumber)}");
            q.Add($"limit={limit}");
            var path = "/api/purchaseinvoices?" + string.Join("&", q);
            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<PurchaseInvoiceListDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<PurchaseInvoiceDetailDto?> GetPurchaseInvoiceByIdAsync(string baseUrl, Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/purchaseinvoices/{id}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<PurchaseInvoiceDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<PurchaseInvoiceDetailDto?> CreatePurchaseInvoiceAsync(string baseUrl, CreatePurchaseInvoiceRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/purchaseinvoices"), content, ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<PurchaseInvoiceDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<(PurchaseInvoiceDetailDto? Invoice, string? Error)> CreatePurchaseInvoiceWithErrorAsync(string baseUrl, CreatePurchaseInvoiceRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/purchaseinvoices"), content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                var msg = string.IsNullOrWhiteSpace(errorText)
                    ? $"Failed to create invoice (HTTP {(int)response.StatusCode})."
                    : errorText.Trim().Trim('"');
                return (null, msg);
            }

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<PurchaseInvoiceDetailDto>(opts, ct);
            return dto == null ? (null, "Failed to read invoice response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<bool> UpdatePurchaseInvoiceAsync(string baseUrl, Guid id, UpdatePurchaseInvoiceRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(Url(baseUrl, $"/api/purchaseinvoices/{id}"), content, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<(WorkOrderCardDto? WorkOrder, string? Error)> CreateWorkOrderFromInvoiceAsync(string baseUrl, Guid purchaseInvoiceId, string? createdBy, CancellationToken ct = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(createdBy)
                ? $"/api/workorders/from-invoice/{purchaseInvoiceId}"
                : $"/api/workorders/from-invoice/{purchaseInvoiceId}?createdBy={Uri.EscapeDataString(createdBy)}";

            var response = await _http.PostAsync(Url(baseUrl, path), null, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync(ct);
                var msg = string.IsNullOrWhiteSpace(errorText)
                    ? $"Failed to create work order (HTTP {(int)response.StatusCode})."
                    : errorText.Trim().Trim('"');
                return (null, msg);
            }
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<WorkOrderCardDto>(opts, ct);
            return dto == null ? (null, "Failed to read work order response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<IReadOnlyList<UserListDto>?> GetUsersAsync(string baseUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, "/api/users"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<UserListDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<WorkOrderCardDto>?> GetWorkOrdersAsync(
        string baseUrl,
        string? status = null,
        string? assignedTo = null,
        bool? sentToProduction = null,
        bool? productionReceived = null,
        int limit = 200,
        CancellationToken ct = default)
    {
        try
        {
            var q = new List<string>();
            if (!string.IsNullOrWhiteSpace(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(assignedTo)) q.Add($"assignedTo={Uri.EscapeDataString(assignedTo)}");
            if (sentToProduction.HasValue) q.Add($"sentToProduction={sentToProduction.Value.ToString().ToLowerInvariant()}");
            if (productionReceived.HasValue) q.Add($"productionReceived={productionReceived.Value.ToString().ToLowerInvariant()}");
            q.Add($"limit={limit}");
            var path = "/api/workorders?" + string.Join("&", q);

            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<WorkOrderCardDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<(IReadOnlyList<WorkOrderCardDto>? WorkOrders, string? Error)> GetWorkOrdersWithErrorAsync(
        string baseUrl,
        string? status = null,
        string? assignedTo = null,
        bool? sentToProduction = null,
        bool? productionReceived = null,
        int limit = 200,
        CancellationToken ct = default)
    {
        try
        {
            var q = new List<string>();
            if (!string.IsNullOrWhiteSpace(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            if (!string.IsNullOrWhiteSpace(assignedTo)) q.Add($"assignedTo={Uri.EscapeDataString(assignedTo)}");
            if (sentToProduction.HasValue) q.Add($"sentToProduction={sentToProduction.Value.ToString().ToLowerInvariant()}");
            if (productionReceived.HasValue) q.Add($"productionReceived={productionReceived.Value.ToString().ToLowerInvariant()}");
            q.Add($"limit={limit}");
            var path = "/api/workorders?" + string.Join("&", q);

            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<List<WorkOrderCardDto>>(opts, ct) ?? [];
            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<WorkOrderDetailDto?> GetWorkOrderByIdAsync(string baseUrl, Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/workorders/{id}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<WorkOrderDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<(WorkOrderDetailDto? WorkOrder, string? Error)> GetWorkOrderByIdWithErrorAsync(string baseUrl, Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/workorders/{id}"), ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<WorkOrderDetailDto>(opts, ct);
            return dto == null ? (null, "Failed to read work order response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<bool> UpdateWorkOrderAsync(string baseUrl, Guid id, UpdateWorkOrderRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PutAsync(Url(baseUrl, $"/api/workorders/{id}"), content, ct);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<(bool Ok, string? Error)> SendWorkOrderToProductionAsync(string baseUrl, Guid id, string? sentBy, CancellationToken ct = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(sentBy)
                ? $"/api/workorders/{id}/send-to-production"
                : $"/api/workorders/{id}/send-to-production?sentBy={Uri.EscapeDataString(sentBy)}";

            var response = await _http.PostAsync(Url(baseUrl, path), null, ct);
            if (!response.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(response, ct));
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string? Error)> ReceiveWorkOrderByProductionAsync(string baseUrl, Guid id, string? receivedBy, CancellationToken ct = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(receivedBy)
                ? $"/api/workorders/{id}/receive-by-production"
                : $"/api/workorders/{id}/receive-by-production?receivedBy={Uri.EscapeDataString(receivedBy)}";

            var response = await _http.PostAsync(Url(baseUrl, path), null, ct);
            if (!response.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(response, ct));
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<InventorySummaryDto?> GetInventorySummaryAsync(string baseUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, "/api/inventory/inventory-summary"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<InventorySummaryDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<BatchHistoryDto>?> GetBatchHistoryAsync(string baseUrl, string batchNumber, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/inventory/batch-history/{Uri.EscapeDataString(batchNumber)}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<BatchHistoryDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<MaterialCategoryDto>?> GetMaterialCategoriesAsync(string baseUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, "/api/material/categories"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<MaterialCategoryDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<MaterialVariantDto>?> GetMaterialVariantsAsync(string baseUrl, Guid? categoryId = null, CancellationToken ct = default)
    {
        try
        {
            var path = categoryId.HasValue ? $"/api/material/variants?categoryId={categoryId.Value}" : "/api/material/variants";
            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<MaterialVariantDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<VariantStockSummaryDto?> GetVariantStockSummaryAsync(string baseUrl, Guid materialVariantId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/inventory/summary/{materialVariantId}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<VariantStockSummaryDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<(MaterialCategoryDto? Category, string? Error)> CreateMaterialCategoryAsync(string baseUrl, string name, string? description, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new { name, description });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/material/categories"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<MaterialCategoryDto>(opts, ct);
            return dto == null ? (null, "Failed to read category response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(MaterialVariantDto? Variant, string? Error)> CreateMaterialVariantAsync(
        string baseUrl,
        Guid materialCategoryId,
        string? grade,
        string? size,
        string unit,
        string sku,
        decimal minimumStockLevel,
        CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                materialCategoryId,
                grade,
                size,
                unit,
                sku,
                minimumStockLevel
            });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/material/variants"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<MaterialVariantDto>(opts, ct);
            return dto == null ? (null, "Failed to read variant response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<IReadOnlyList<MaterialTypeNodeDto>?> GetInventoryMaterialTreeAsync(string baseUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, "/api/inventory/material-tree"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<MaterialTypeNodeDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<VendorDto>?> GetVendorsAsync(string baseUrl, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, "/api/vendor"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<VendorDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<(VendorDto? Vendor, string? Error)> CreateVendorAsync(string baseUrl, CreateVendorRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/vendor"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<VendorDto>(opts, ct);
            return dto == null ? (null, "Failed to read vendor response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<IReadOnlyList<VendorInvoiceListItemDto>?> GetVendorInvoicesAsync(string baseUrl, string? status = null, int limit = 200, CancellationToken ct = default)
    {
        try
        {
            var q = new List<string> { $"limit={limit}" };
            if (!string.IsNullOrWhiteSpace(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            var path = "/api/vendor-invoices?" + string.Join("&", q);
            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<VendorInvoiceListItemDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<VendorInvoiceDetailDto?> GetVendorInvoiceByIdAsync(string baseUrl, Guid id, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/vendor-invoices/{id}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<VendorInvoiceDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<(VendorInvoiceCreatedDto? Invoice, string? Error)> CreateVendorInvoiceFromPoAsync(string baseUrl, Guid vendorPoId, CreateVendorInvoiceFromPoRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, $"/api/vendor-invoices/from-vendor-po/{vendorPoId}"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<VendorInvoiceCreatedDto>(opts, ct);
            return dto == null ? (null, "Failed to read invoice response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(VendorInvoiceAcceptedDto? Result, string? Error)> AcceptVendorInvoiceAsync(string baseUrl, Guid vendorInvoiceId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync(Url(baseUrl, $"/api/vendor-invoices/{vendorInvoiceId}/accept"), null, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<VendorInvoiceAcceptedDto>(opts, ct);
            return dto == null ? (null, "Failed to read accept response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(VendorPoCreatedDto? Po, string? Error)> CreateVendorPoFromSrsAsync(string baseUrl, Guid srsId, Guid vendorId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync(Url(baseUrl, $"/api/srs/{srsId}/create-vendor-po?vendorId={vendorId}"), null, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<VendorPoCreatedDto>(opts, ct);
            return dto == null ? (null, "Failed to read PO response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(VendorPoCreatedDto? Po, string? Error)> CreateVendorPoAsync(string baseUrl, CreateVendorPoRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/procurement/vendor-po"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<VendorPoCreatedDto>(opts, ct);
            return dto == null ? (null, "Failed to read vendor PO response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<(DirectGrnResultDto? Result, string? Error)> CreateDirectGrnAsync(string baseUrl, CreateDirectGrnRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/procurement/direct-grn"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<DirectGrnResultDto>(opts, ct);
            return dto == null ? (null, "Failed to read GRN response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<string?> GetNextBatchNumberAsync(string baseUrl, Guid materialVariantId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/procurement/next-batch-number/{materialVariantId}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<NextBatchNumberDto>(opts, ct);
            return dto?.BatchNumber;
        }
        catch { return null; }
    }

    public async Task<(SubmitGrnDraftResultDto? Result, string? Error)> SubmitGrnDraftAsync(string baseUrl, Guid grnId, SubmitGrnDraftRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, $"/api/procurement/grns/{grnId}/submit-draft"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<SubmitGrnDraftResultDto>(opts, ct);
            return dto == null ? (null, "Failed to read submit response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<IReadOnlyList<GrnListItemDto>?> GetGrnsAsync(string baseUrl, int limit = 50, Guid? vendorId = null, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        try
        {
            var q = new List<string> { $"limit={limit}" };
            if (vendorId.HasValue) q.Add($"vendorId={vendorId.Value}");
            if (from.HasValue) q.Add($"from={from.Value:O}");
            if (to.HasValue) q.Add($"to={to.Value:O}");
            var path = "/api/procurement/grns?" + string.Join("&", q);

            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<GrnListItemDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<GrnDetailDto?> GetGrnByIdAsync(string baseUrl, Guid grnId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/procurement/grns/{grnId}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<GrnDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<IReadOnlyList<VendorPoListItemDto>?> GetVendorPosAsync(string baseUrl, int limit = 100, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/procurement/vendor-pos?limit={limit}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<VendorPoListItemDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<VendorPoDetailDto?> GetVendorPoByIdAsync(string baseUrl, Guid vendorPoId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/procurement/vendor-pos/{vendorPoId}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<VendorPoDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<(SrsDto? Srs, string? Error)> CreateSrsAsync(string baseUrl, CreateSrsRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, "/api/srs"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<SrsDto>(opts, ct);
            return dto == null ? (null, "Failed to read SRS response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<IReadOnlyList<SrsListDto>?> GetSrsListAsync(string baseUrl, string? status = null, Guid? workOrderId = null, int limit = 200, CancellationToken ct = default)
    {
        try
        {
            var q = new List<string>();
            if (!string.IsNullOrWhiteSpace(status)) q.Add($"status={Uri.EscapeDataString(status)}");
            if (workOrderId.HasValue) q.Add($"workOrderId={workOrderId.Value}");
            q.Add($"limit={limit}");
            var path = "/api/srs?" + string.Join("&", q);

            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<SrsListDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<SrsDetailDto?> GetSrsByIdAsync(string baseUrl, Guid srsId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/srs/{srsId}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<SrsDetailDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<(bool Ok, string? Error)> ApproveSrsAsync(string baseUrl, Guid srsId, string? approvedBy, CancellationToken ct = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(approvedBy)
                ? $"/api/srs/{srsId}/approve"
                : $"/api/srs/{srsId}/approve?approvedBy={Uri.EscapeDataString(approvedBy)}";
            var response = await _http.PostAsync(Url(baseUrl, path), null, ct);
            if (!response.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(response, ct));
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string? Error)> AllocateSrsFifoAsync(string baseUrl, Guid srsId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync(Url(baseUrl, $"/api/srs/{srsId}/allocate-fifo"), null, ct);
            if (!response.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(response, ct));
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string? Error)> ConsumeSrsAsync(string baseUrl, Guid srsId, string? consumedBy, CancellationToken ct = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(consumedBy)
                ? $"/api/srs/{srsId}/consume"
                : $"/api/srs/{srsId}/consume?consumedBy={Uri.EscapeDataString(consumedBy)}";
            var response = await _http.PostAsync(Url(baseUrl, path), null, ct);
            if (!response.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(response, ct));
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<QualityQueuesDto?> GetProductionManagerQualityQueuesAsync(string baseUrl, int limit = 500, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/quality/production-manager/queues?limit={limit}"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<QualityQueuesDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<WorkOrderQualitySummaryDto?> GetWorkOrderQualitySummaryAsync(string baseUrl, Guid workOrderId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync(Url(baseUrl, $"/api/workorders/{workOrderId}/quality"), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<WorkOrderQualitySummaryDto>(opts, ct);
        }
        catch { return null; }
    }

    public async Task<(QualityCheckRecordedDto? Result, string? Error)> RecordWorkOrderQualityCheckAsync(string baseUrl, Guid workOrderId, RecordQualityCheckRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, $"/api/workorders/{workOrderId}/quality/checks"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorAsync(response, ct));

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var dto = await response.Content.ReadFromJsonAsync<QualityCheckRecordedDto>(opts, ct);
            return dto == null ? (null, "Failed to read QC response.") : (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<IReadOnlyList<NcrListDto>?> GetWorkOrderNcrsAsync(string baseUrl, Guid workOrderId, string? status = null, CancellationToken ct = default)
    {
        try
        {
            var path = string.IsNullOrWhiteSpace(status)
                ? $"/api/workorders/{workOrderId}/ncrs"
                : $"/api/workorders/{workOrderId}/ncrs?status={Uri.EscapeDataString(status)}";
            var response = await _http.GetAsync(Url(baseUrl, path), ct);
            if (!response.IsSuccessStatusCode) return null;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await response.Content.ReadFromJsonAsync<List<NcrListDto>>(opts, ct) ?? [];
        }
        catch { return null; }
    }

    public async Task<(bool Ok, string? Error)> CloseNcrAsync(string baseUrl, Guid workOrderId, Guid ncrId, CloseNcrRequest req, CancellationToken ct = default)
    {
        try
        {
            var body = JsonSerializer.Serialize(req);
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(Url(baseUrl, $"/api/workorders/{workOrderId}/ncrs/{ncrId}/close"), content, ct);
            if (!response.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(response, ct));
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool Ok, string? Error)> DeleteWorkOrderQualityAsync(string baseUrl, Guid workOrderId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.DeleteAsync(Url(baseUrl, $"/api/workorders/{workOrderId}/quality"), ct);
            if (!response.IsSuccessStatusCode)
                return (false, await ReadErrorAsync(response, ct));
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}

public record LineItemInput(string PartNumber, string Description, int Quantity, decimal UnitPrice, decimal TaxPercent, string? AttachmentPath = null);

public record UpdateQuotationInput(string? Status, string? ClientName, string? ProjectName, string? Description, string? Attachments, decimal? ManualPrice, string? PriceBreakdown, List<LineItemInput>? LineItems, string? ChangedBy);
