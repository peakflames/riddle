using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Riddle.Web.Components;
using Riddle.Web.Components.Account;
using Riddle.Web.Data;
using Riddle.Web.Models;
using Flowbite.Services;

var builder = WebApplication.CreateBuilder(args);

// Enable static web assets for non-published runs (development mode outside of VS)
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.UseStaticWebAssets();
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map additional identity endpoints (external login callback, etc.)
app.MapGroup("/Account").MapAdditionalIdentityEndpoints();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RiddleDbContext>();
    db.Database.EnsureCreated();
}

app.Run();
