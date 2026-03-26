using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TechUnited_AiStudio.Components;
using TechUnited_AiStudio.Components.Account;
using TechUnited_AiStudio.Data;
using TechUnited_AiStudio.Hubs;
using TechUnited_AiStudio.Models;
using TechUnited_AiStudio.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. SERVICE REGISTRATIONS
// ============================================================

// --- Blazor Components ---
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DetailedErrors = true; // This will show the real error in the browser F12 console
    });

// --- SignalR Service ---
builder.Services.AddSignalR();

// CRITICAL FIX: Map SignalR UserID to the Database GUID
// This ensures Clients.User(id) works with the ApplicationUser.Id
builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

// --- Database Configuration ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- Identity & Authentication Services ---
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

builder.Services.AddIdentityCore<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// --- Custom Application Services ---
builder.Services.AddScoped<DocumentProcessingService>();

// --- Ollama API Clients ---
builder.Services.AddHttpClient<OllamaService>(client =>
{
    client.BaseAddress = new Uri("http://10.0.0.103:11434/");
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddHttpClient("EmbeddingClient", client =>
{
    client.BaseAddress = new Uri("http://10.0.0.103:11444/");
    client.Timeout = TimeSpan.FromMinutes(2);
});

// ============================================================
// 2. BUILD THE APP
// ============================================================

var app = builder.Build();

// ============================================================
// 3. MIDDLEWARE & ENDPOINTS
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

// IMPORTANT: Static files must be handled before Antiforgery to resolve 404s
app.UseStaticFiles();
app.UseAntiforgery();

// Handles .NET 9 optimized asset delivery
app.MapStaticAssets();

// --- SignalR Hub Route ---
app.MapHub<ChatHub>("/chathub");

// --- Identity & Razor Components ---
app.MapAdditionalIdentityEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// ============================================================
// 4. HELPER CLASSES
// ============================================================

/// <summary>
/// Custom User ID Provider for SignalR to use the Database GUID (NameIdentifier claim)
/// </summary>
public class NameUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        // This maps the 'senderId' and 'recipientUserId' to the string GUID in the DB
        return connection.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }
}