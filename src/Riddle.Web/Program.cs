using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Riddle.Web.Components;
using Riddle.Web.Components.Account;
using Riddle.Web.Data;
using Riddle.Web.Hubs;
using Riddle.Web.Models;
using Riddle.Web.Services;
using Flowbite.Services;
using DotNetEnv;

// Load .env file if it exists (for local development secrets)
Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Enable static web assets for non-published runs (development mode outside of VS)
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseStaticWebAssets();
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configuration bindings
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));
builder.Services.Configure<WhitelistSettings>(builder.Configuration.GetSection("WhitelistSettings"));

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=riddle.db";
builder.Services.AddDbContext<RiddleDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity helper services
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Identity configuration
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
})
    .AddEntityFrameworkStores<RiddleDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Authentication with Google OAuth
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies(options =>
    {
        // Configure persistent cookies (30-day expiration for "remember me")
        options.ApplicationCookie?.Configure(cookie =>
        {
            cookie.ExpireTimeSpan = TimeSpan.FromDays(30);
            cookie.SlidingExpiration = true;
        });
    });

// Google OAuth - environment variables take precedence over configuration
var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") 
    ?? builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
    ?? builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.CallbackPath = "/signin-google";
            
            // Request profile picture claim
            options.Scope.Add("profile");
            options.ClaimActions.MapJsonKey("picture", "picture");
            
            // Validate user against whitelist during sign-in
            options.Events.OnTicketReceived = async context =>
            {
                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                if (string.IsNullOrEmpty(email))
                {
                    context.Fail("Email claim not found.");
                    return;
                }
                
                // Get AllowedUserService from DI - need to resolve from scope
                var allowedUserService = context.HttpContext.RequestServices
                    .GetRequiredService<IAllowedUserService>();
                
                if (!await allowedUserService.IsEmailAllowedAsync(email))
                {
                    // Redirect to access denied page instead of failing outright
                    context.Response.Redirect("/Account/AccessDenied");
                    context.HandleResponse(); // Prevents default processing
                    return;
                }
                
                // User is allowed, continue with sign-in
            };
        });
}
else
{
    // Log warning if Google OAuth not configured
    Console.WriteLine("WARNING: Google OAuth not configured. Set GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET environment variables.");
}

// Authorization
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Flowbite
builder.Services.AddFlowbite();

// SignalR for real-time communication
builder.Services.AddSignalR();

// Connection tracking (singleton for cross-request state)
builder.Services.AddSingleton<IConnectionTracker, ConnectionTracker>();

// Application Services
builder.Services.AddScoped<IAllowedUserService, AllowedUserService>();
builder.Services.AddScoped<IAppEventService, AppEventService>();
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<ICharacterService, CharacterService>();
builder.Services.AddScoped<ICharacterTemplateService, CharacterTemplateService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IGameStateService, GameStateService>();
builder.Services.AddScoped<IToolExecutor, ToolExecutor>();
builder.Services.AddScoped<IRiddleLlmService, RiddleLlmService>();
builder.Services.AddScoped<ICombatService, CombatService>();

// SignalR notification service for broadcasting events to campaign participants
builder.Services.AddScoped<INotificationService, NotificationService>();

// Health checks for container orchestration
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseAntiforgery();

app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map additional identity endpoints (external login callback, etc.)
// Skip in Testing environment where Identity is replaced with test auth
if (!app.Environment.IsEnvironment("Testing"))
{
    app.MapGroup("/Account").MapAdditionalIdentityEndpoints();
}

// Map SignalR hub for real-time game events
app.MapHub<GameHub>("/gamehub");

// Health check endpoint for container orchestration (Docker, Kubernetes)
app.MapHealthChecks("/health");

// Ensure database is created (skip during integration testing to avoid provider conflicts)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RiddleDbContext>();
    db.Database.EnsureCreated();
}

app.Run();

// Marker class for WebApplicationFactory<Program> in integration tests
public partial class Program { }
