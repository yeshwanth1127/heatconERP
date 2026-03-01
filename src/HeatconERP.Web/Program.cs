using HeatconERP.Web.Components;
using HeatconERP.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<ApiClient>(client =>
{
    // Converting an enquiry to a quotation can involve DB work and may exceed 10s on larger datasets.
    // Make timeout configurable (env var/appsettings key: HTTP_TIMEOUT_SECONDS). Defaults to 60s.
    var timeoutSeconds = builder.Configuration.GetValue<int?>("HTTP_TIMEOUT_SECONDS") ?? 60;
    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 600));
});
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
