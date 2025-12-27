# Phase 1 Implementation Plan: Project Riddle Foundation

**Version:** 1.0  
**Date:** December 27, 2024  
**Status:** Ready for Implementation  
**Phase:** Foundation (Week 1)

---

## [Overview]

Phase 1 establishes the foundational infrastructure for Project Riddle, a Blazor Server application that serves as an LLM-driven Dungeon Master assistant for D&D 5th Edition. This phase focuses on creating the core project structure, implementing data models with Entity Framework Core, configuring Google OAuth authentication, and establishing the basic Blazor Server layout with Tailwind CSS and Flowbite components.

**Key Objectives:**
- Create a working Blazor Server project with all required dependencies
- Implement complete data model layer with EF Core migrations
- Configure Google OAuth for user authentication
- Establish UI foundation with Flowbite Blazor components
- Create landing page with session management

**Success Criteria:**
- Application compiles and runs successfully
- SQLite database initializes with proper schema
- Google authentication flow works end-to-end
- Landing page displays with proper styling
- Session list can be viewed (empty initially)

---

## [Types]

Complete type definitions for all data models, including Entity Framework Core configurations and JSON column support.

### RiddleSession (Root Entity)
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Riddle.Web.Models;

[Index(nameof(DmUserId))]
public class RiddleSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(200)]
    public string CampaignName { get; set; } = "Lost Mine of Phandelver";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    
    // Owner
    [Required]
    public string DmUserId { get; set; } = null!;
    
    [ForeignKey(nameof(DmUserId))]
    public ApplicationUser DmUser { get; set; } = null!;
    
    // Campaign Progression
    [Required]
    [MaxLength(100)]
    public string CurrentChapterId { get; set; } = "chapter_1";
    
    [Required]
    [MaxLength(100)]
    public string CurrentLocationId { get; set; } = "goblin_ambush";
    
    // JSON stored collections
    [Column(TypeName = "text")] // SQLite uses text for JSON
    public string CompletedMilestonesJson { get; set; } = "[]";
    
    [Column(TypeName = "text")]
    public string KnownNpcIdsJson { get; set; } = "[]";
    
    [Column(TypeName = "text")]
    public string DiscoveredLocationsJson { get; set; } = "[]";
    
    // Entity collections as JSON
    [Column(TypeName = "text")]
    public string PartyStateJson { get; set; } = "[]";
    
    [Column(TypeName = "text")]
    public string ActiveQuestsJson { get; set; } = "[]";
    
    [Column(TypeName = "text")]
    public string? ActiveCombatJson { get; set; }
    
    [Column(TypeName = "text")]
    public string NarrativeLogJson { get; set; } = "[]";
    
    [Column(TypeName = "text")]
    public string PreferencesJson { get; set; } = "{}";
    
    // Context
    [MaxLength(5000)]
    public string? LastNarrativeSummary { get; set; }
    
    // UI State
    [Column(TypeName = "text")]
    public string ActivePlayerChoicesJson { get; set; } = "[]";
    
    [MaxLength(500)]
    public string? CurrentSceneImageUri { get; set; }
    
    [MaxLength(5000)]
    public string? CurrentReadAloudText { get; set; }
    
    // Navigation properties (not mapped to JSON)
    [NotMapped]
    public List<string> CompletedMilestones
    {
        get => JsonSerializer.Deserialize<List<string>>(CompletedMilestonesJson) ?? new();
        set => CompletedMilestonesJson = JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public List<string> KnownNpcIds
    {
        get => JsonSerializer.Deserialize<List<string>>(KnownNpcIdsJson) ?? new();
        set => KnownNpcIdsJson = JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public List<string> DiscoveredLocations
    {
        get => JsonSerializer.Deserialize<List<string>>(DiscoveredLocationsJson) ?? new();
        set => DiscoveredLocationsJson = JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public List<Character> PartyState
    {
        get => JsonSerializer.Deserialize<List<Character>>(PartyStateJson) ?? new();
        set => PartyStateJson = JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public List<Quest> ActiveQuests
    {
        get => JsonSerializer.Deserialize<List<Quest>>(ActiveQuestsJson) ?? new();
        set => ActiveQuestsJson = JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public CombatEncounter? ActiveCombat
    {
        get => string.IsNullOrEmpty(ActiveCombatJson) 
            ? null 
            : JsonSerializer.Deserialize<CombatEncounter>(ActiveCombatJson);
        set => ActiveCombatJson = value == null ? null : JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public List<LogEntry> NarrativeLog
    {
        get => JsonSerializer.Deserialize<List<LogEntry>>(NarrativeLogJson) ?? new();
        set => NarrativeLogJson = JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public PartyPreferences Preferences
    {
        get => JsonSerializer.Deserialize<PartyPreferences>(PreferencesJson) ?? new();
        set => PreferencesJson = JsonSerializer.Serialize(value);
    }
    
    [NotMapped]
    public List<string> ActivePlayerChoices
    {
        get => JsonSerializer.Deserialize<List<string>>(ActivePlayerChoicesJson) ?? new();
        set => ActivePlayerChoicesJson = JsonSerializer.Serialize(value);
    }
}
```

### Character
```csharp
namespace Riddle.Web.Models;

public class Character
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = null!;
    public string Type { get; set; } = "PC"; // "PC" or "NPC"
    
    // Core Stats
    public int ArmorClass { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int Initiative { get; set; }
    public int PassivePerception { get; set; }
    
    // State
    public List<string> Conditions { get; set; } = new();
    public string? StatusNotes { get; set; }
    
    // Player Info (for PCs)
    public string? PlayerId { get; set; }
    public string? PlayerName { get; set; }
}
```

### Quest
```csharp
namespace Riddle.Web.Models;

public class Quest
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = null!;
    public string State { get; set; } = "Active"; // "Active", "Completed", "Failed"
    public bool IsMainStory { get; set; }
    public List<string> Objectives { get; set; } = new();
    public string? RewardDescription { get; set; }
}
```

### PartyPreferences
```csharp
namespace Riddle.Web.Models;

public class PartyPreferences
{
    public string CombatFocus { get; set; } = "Medium"; // "Low", "Medium", "High"
    public string RoleplayFocus { get; set; } = "Medium";
    public string Pacing { get; set; } = "Methodical"; // "Fast", "Methodical"
    public string Tone { get; set; } = "Adventurous"; // "Adventurous", "Dark", "Comedic"
    public List<string> AvoidedTopics { get; set; } = new();
}
```

### CombatEncounter
```csharp
namespace Riddle.Web.Models;

public class CombatEncounter
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool IsActive { get; set; } = true;
    public int RoundNumber { get; set; } = 1;
    
    // Turn Order
    public List<string> TurnOrder { get; set; } = new(); // Character IDs
    public int CurrentTurnIndex { get; set; } = 0;
    public List<string> SurprisedEntities { get; set; } = new();
}
```

### LogEntry
```csharp
namespace Riddle.Web.Models;

public class LogEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Entry { get; set; } = null!;
    public string Importance { get; set; } = "standard"; // "minor", "standard", "critical"
}
```

### ApplicationUser
```csharp
using Microsoft.AspNetCore.Identity;

namespace Riddle.Web.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

---

## [Files]

Comprehensive list of all files to be created or modified for Phase 1.

### New Files to Create

#### Project Files
- `src/Riddle.Web/Riddle.Web.csproj` - Main project file with NuGet packages
- `src/Riddle.Web/Program.cs` - Application entry point and service configuration
- `src/Riddle.Web/appsettings.json` - Configuration settings
- `src/Riddle.Web/appsettings.Development.json` - Development-specific settings

#### Data Layer
- `src/Riddle.Web/Data/RiddleDbContext.cs` - EF Core DbContext
- `src/Riddle.Web/Models/RiddleSession.cs` - Root entity
- `src/Riddle.Web/Models/Character.cs` - Character entity
- `src/Riddle.Web/Models/Quest.cs` - Quest entity
- `src/Riddle.Web/Models/PartyPreferences.cs` - Preferences entity
- `src/Riddle.Web/Models/CombatEncounter.cs` - Combat entity
- `src/Riddle.Web/Models/LogEntry.cs` - Log entry entity
- `src/Riddle.Web/Models/ApplicationUser.cs` - Identity user

#### UI Components
- `src/Riddle.Web/Components/App.razor` - Root component
- `src/Riddle.Web/Components/Routes.razor` - Routing configuration
- `src/Riddle.Web/Components/Layout/MainLayout.razor` - Main layout
- `src/Riddle.Web/Components/Layout/NavMenu.razor` - Navigation menu
- `src/Riddle.Web/Components/Pages/Home.razor` - Landing page
- `src/Riddle.Web/Components/Pages/Account/Login.razor` - Login page
- `src/Riddle.Web/Components/Pages/Account/Logout.razor` - Logout page
- `src/Riddle.Web/Components/Pages/Account/AccessDenied.razor` - Access denied page

#### Static Assets
- `src/Riddle.Web/wwwroot/css/app.css` - Tailwind input file
- `src/Riddle.Web/tailwind.config.js` - Tailwind configuration
- `src/Riddle.Web/postcss.config.js` - PostCSS configuration
- `src/Riddle.Web/wwwroot/favicon.ico` - Site favicon

### Files to Modify
- None (this is a new project)

---

## [Functions]

Service layer functions to be implemented in Phase 1.

### No Service Functions in Phase 1
Phase 1 focuses on infrastructure and authentication. Service layer implementation (GameStateService, LLM integration) is deferred to Phase 2.

---

## [Classes]

Complete class implementations for Phase 1.

### RiddleDbContext
```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Riddle.Web.Models;

namespace Riddle.Web.Data;

public class RiddleDbContext : IdentityDbContext<ApplicationUser>
{
    public RiddleDbContext(DbContextOptions<RiddleDbContext> options)
        : base(options)
    {
    }

    public DbSet<RiddleSession> RiddleSessions => Set<RiddleSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure RiddleSession
        builder.Entity<RiddleSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.DmUserId);
            
            entity.HasOne(e => e.DmUser)
                .WithMany()
                .HasForeignKey(e => e.DmUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // String length constraints
            entity.Property(e => e.CampaignName).HasMaxLength(200);
            entity.Property(e => e.CurrentChapterId).HasMaxLength(100);
            entity.Property(e => e.CurrentLocationId).HasMaxLength(100);
            entity.Property(e => e.LastNarrativeSummary).HasMaxLength(5000);
            entity.Property(e => e.CurrentSceneImageUri).HasMaxLength(500);
            entity.Property(e => e.CurrentReadAloudText).HasMaxLength(5000);
        });

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.DisplayName).HasMaxLength(100);
        });
    }
}
```

### Program.cs Configuration
```csharp
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Riddle.Web.Components;
using Riddle.Web.Components.Account;
using Riddle.Web.Data;
using Riddle.Web.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database
builder.Services.AddDbContext<RiddleDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlite(connectionString);
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
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

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<RiddleDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Google OAuth
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] 
            ?? throw new InvalidOperationException("Google ClientId not configured");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google ClientSecret not configured");
    });

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline
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
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAdditionalIdentityEndpoints();

app.Run();
```

---

## [Dependencies]

All NuGet packages required for Phase 1.

### Riddle.Web.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PostCSSConfig>postcss.config.js</PostCSSConfig>
    <TailwindConfig>tailwind.config.js</TailwindConfig>
  </PropertyGroup>

  <ItemGroup>
    <!-- Blazor Server -->
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="9.0.0" />
    
    <!-- Entity Framework Core -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore" Version="9.0.0" />
    
    <!-- Authentication -->
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="9.0.0" />
    
    <!-- UI Components -->
    <PackageReference Include="Flowbite" Version="0.1.2-beta" />
    <PackageReference Include="Flowbite.ExtendedIcons" Version="0.0.5-alpha" />
  </ItemGroup>

  <!-- Tailwind CSS Build Target -->
  <Target Name="TailwindBuild" BeforeTargets="Build" Condition="'$(OS)'=='Windows_NT'">
    <Error Condition="!Exists('.\tools\tailwindcss.exe')" Text="Tailwind executable not found at .\tools\tailwindcss.exe" />
    <Exec 
      Command=".\tools\tailwindcss.exe -i ./wwwroot/css/app.css -o ./wwwroot/css/app.min.css" 
      ConsoleToMSBuild="true"
      StandardOutputImportance="high"
      StandardErrorImportance="high"
      IgnoreExitCode="true">
      <Output TaskParameter="ConsoleOutput" PropertyName="TailwindOutput" />
      <Output TaskParameter="ExitCode" PropertyName="TailwindExitCode" />
    </Exec>
    <Error Condition="'$(TailwindExitCode)' != '0'" Text="Tailwind command failed with exit code $(TailwindExitCode). Output: $(TailwindOutput)" />
  </Target>

  <ItemGroup>
    <UpToDateCheckBuilt Include="wwwroot/css/app.css" Set="Css" />
    <UpToDateCheckBuilt Include="wwwroot/css/app.min.css" Set="Css" />
    <UpToDateCheckBuilt Include="tailwind.config.js" Set="Css" />
  </ItemGroup>

  <ItemGroup>
    <None Remove="wwwroot\css\app.css" />
    <None Remove="wwwroot\css\app.min.css" />
    <None Remove="tools\tailwindcss.exe" />
  </ItemGroup>

</Project>
```

---

## [Testing]

Testing approach for Phase 1 (primarily manual testing).

### Manual Testing Checklist
1. **Build & Run**
   - Project compiles without errors
   - Application starts and listens on http://localhost:5269
   - No runtime errors in console

2. **Database**
   - SQLite database file created at `riddle.db`
   - All migrations applied successfully
   - Tables created with proper schema

3. **Authentication**
   - Login page loads
   - Google OAuth redirects to Google
   - Successful login creates user in database
   - User can logout
   - Protected pages require authentication

4. **UI**
   - Landing page loads with Tailwind styles
   - Flowbite components render correctly
   - Navigation menu works
   - Responsive design functions properly

### No Unit Tests Yet
Automated testing infrastructure will be added in later phases.

---

## [Implementation Order]

Step-by-step implementation sequence for Phase 1.

### Step 1: Create Project Structure
1. Create `Riddle.Web.csproj` with all NuGet packages
2. Create `Program.cs` with minimal configuration
3. Create `appsettings.json` and `appsettings.Development.json`
4. Run `dotnet restore` to verify packages

### Step 2: Implement Data Models
1. Create all model classes in `Models/` directory
2. Create `RiddleDbContext` in `Data/` directory
3. Update `Program.cs` to register DbContext
4. Create initial migration: `dotnet ef migrations add InitialCreate`
5. Update database: `dotnet ef database update`

### Step 3: Configure Authentication
1. Update `Program.cs` with Identity and Google OAuth services
2. Create Identity helper classes (IdentityUserAccessor, IdentityRedirectManager, etc.)
3. Create Account pages (Login, Logout, AccessDenied)
4. Configure Google OAuth in Google Cloud Console
5. Add credentials to user secrets

### Step 4: Create UI Foundation
1. Create `App.razor` root component
2. Create `Routes.razor` for routing
3. Create `MainLayout.razor` with Flowbite structure
4. Create `NavMenu.razor` with authentication links
5. Create `Home.razor` landing page

### Step 5: Configure Tailwind CSS
1. Create `tailwind.config.js` with Flowbite plugin
2. Create `postcss.config.js`
3. Create `wwwroot/css/app.css` with Tailwind directives
4. Verify Tailwind build target in csproj

### Step 6: Test & Verify
1. Run `python build.py` to verify build
2. Test Google authentication flow
3. Verify database creation
4. Test navigation and layout
5. Verify Tailwind styles applied

---

## Commands for Phase 1 Implementation

```bash
# Step 1: Restore packages
cd src/Riddle.Web
dotnet restore

# Step 2: Create initial migration
dotnet ef migrations add InitialCreate --project src/Riddle.Web

# Step 3: Update database
dotnet ef database update --project src/Riddle.Web

# Step 4: Configure user secrets
dotnet user-secrets init --project src/Riddle.Web
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID" --project src/Riddle.Web
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET" --project src/Riddle.Web

# Step 5: Build and run
python build.py
python build.py run
```

---

## Phase 1 Completion Checklist

- [ ] Riddle.Web.csproj created with all dependencies
- [ ] Program.cs configured with services
- [ ] All model classes implemented
- [ ] RiddleDbContext created
- [ ] Initial EF Core migration created
- [ ] Database created and updated
- [ ] Google OAuth configured in Google Cloud Console
- [ ] Authentication services registered
- [ ] Identity pages created (Login, Logout, AccessDenied)
- [ ] App.razor and Routes.razor created
- [ ] MainLayout.razor created with Flowbite
- [ ] NavMenu.razor created
- [ ] Home.razor landing page created
- [ ] Tailwind configuration files created
- [ ] Application builds successfully
- [ ] Application runs without errors
- [ ] Google authentication works end-to-end
- [ ] Landing page displays correctly
- [ ] Navigation functions properly

---

## Next Phase Preview

**Phase 2: LLM Integration (Week 2)**
- Implement LLM Tornado service
- Build all 7 tool handlers
- Create tool executor
- Implement GameStateService
- Test basic chat flow with OpenRouter
