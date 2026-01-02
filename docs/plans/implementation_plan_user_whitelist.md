# Implementation Plan: User Whitelist (Beta Access Control)

## Overview

Implement a runtime-manageable user whitelist to control who can sign into the application during beta testing. This replaces the need to manually configure Google Cloud Console test users.

**Problem Statement:**
- Currently using Google Cloud Console "test users" list for access control
- Requires cloud console access to add/remove users
- Want admins to manage allowed users directly from the application
- Need to block unauthorized Google sign-ins at the application level

**Solution:**
1. Store allowed emails in SQLite database (`AllowedUsers` table)
2. Admin-only Settings page (`/admin/settings`) for managing the whitelist
3. Authentication event handler that rejects users not in the whitelist
4. Graceful rejection with clear error message for unauthorized users

**Key Design Decisions:**
- **Database over config file:** Allows runtime updates without restart
- **Email-based:** Matches Google OAuth identity, case-insensitive
- **Admins always allowed:** `AdminSettings.AdminEmails` bypass the whitelist check
- **Whitelist can be disabled:** Future-proofing for public release

---

## Types

### New Types

**1. AllowedUser Entity** (new file: `src/Riddle.Web/Models/AllowedUser.cs`)
```csharp
namespace Riddle.Web.Models;

/// <summary>
/// Represents an email address allowed to sign into the application.
/// Used for beta testing access control.
/// </summary>
public class AllowedUser
{
    /// <summary>
    /// Primary key (GUID v7 for time-sortability)
    /// </summary>
    public string Id { get; set; } = Guid.CreateVersion7().ToString();
    
    /// <summary>
    /// Email address (stored lowercase, matched case-insensitively)
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional display name or note about this user
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Who added this user to the whitelist (admin's user ID)
    /// </summary>
    public string? AddedByUserId { get; set; }
    
    /// <summary>
    /// When this user was added to the whitelist
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this entry is currently active (allows soft-disable without delete)
    /// </summary>
    public bool IsActive { get; set; } = true;
}
```

**2. WhitelistSettings** (new file: `src/Riddle.Web/Models/WhitelistSettings.cs`)
```csharp
namespace Riddle.Web.Models;

/// <summary>
/// Configuration for the user whitelist feature.
/// Bound from appsettings.json "WhitelistSettings" section.
/// </summary>
public class WhitelistSettings
{
    /// <summary>
    /// Whether the whitelist is enforced. When false, all authenticated users can access.
    /// Default: true (whitelist enforced during beta)
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Message shown to users who are not on the whitelist.
    /// </summary>
    public string RejectionMessage { get; set; } = 
        "This application is currently in private beta. Contact the administrator for access.";
}
```

**3. IAllowedUserService Interface** (new file: `src/Riddle.Web/Services/IAllowedUserService.cs`)
```csharp
namespace Riddle.Web.Services;

public interface IAllowedUserService
{
    /// <summary>
    /// Check if an email is allowed to sign in (either in whitelist or is admin)
    /// </summary>
    Task<bool> IsEmailAllowedAsync(string email, CancellationToken ct = default);
    
    /// <summary>
    /// Get all allowed users (for admin UI)
    /// </summary>
    Task<List<AllowedUser>> GetAllowedUsersAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Add an email to the whitelist
    /// </summary>
    Task<AllowedUser> AddAllowedUserAsync(string email, string? displayName, string addedByUserId, CancellationToken ct = default);
    
    /// <summary>
    /// Remove an email from the whitelist (hard delete)
    /// </summary>
    Task<bool> RemoveAllowedUserAsync(string id, CancellationToken ct = default);
    
    /// <summary>
    /// Toggle active status (soft enable/disable)
    /// </summary>
    Task<bool> SetActiveStatusAsync(string id, bool isActive, CancellationToken ct = default);
    
    /// <summary>
    /// Check if whitelist enforcement is enabled
    /// </summary>
    bool IsWhitelistEnabled { get; }
}
```

---

## Files

### New Files to Create

| Path | Purpose |
|------|---------|
| `src/Riddle.Web/Models/AllowedUser.cs` | Entity for whitelist entries |
| `src/Riddle.Web/Models/WhitelistSettings.cs` | Configuration POCO |
| `src/Riddle.Web/Services/IAllowedUserService.cs` | Service interface |
| `src/Riddle.Web/Services/AllowedUserService.cs` | Service implementation |
| `src/Riddle.Web/Components/Pages/Admin/Settings.razor` | Admin settings page with whitelist management |
| `src/Riddle.Web/Components/Pages/Account/AccessDenied.razor` | Page shown when user is not on whitelist |
| `src/Riddle.Web/Migrations/{timestamp}_AddAllowedUsers.cs` | EF migration |

### Existing Files to Modify

| Path | Changes |
|------|---------|
| `src/Riddle.Web/appsettings.json` | Add `WhitelistSettings` section |
| `src/Riddle.Web/Program.cs` | Register services, bind config, add auth event handler |
| `src/Riddle.Web/Data/RiddleDbContext.cs` | Add `AllowedUsers` DbSet and configuration |
| `src/Riddle.Web/Components/Layout/AppSidebar.razor` | Add "Settings" link for admins |
| `src/Riddle.Web/Services/IAdminService.cs` | (No changes needed - already has IsAdmin) |
| `build.py` | Add `db users` and `db add-user` commands |

---

## Functions

### AllowedUserService Methods

```csharp
public class AllowedUserService : IAllowedUserService
{
    private readonly RiddleDbContext _db;
    private readonly IAdminService _adminService;
    private readonly WhitelistSettings _settings;
    
    // Check if email is allowed (admin emails always allowed)
    public async Task<bool> IsEmailAllowedAsync(string email, CancellationToken ct = default)
    {
        if (!_settings.IsEnabled)
            return true; // Whitelist disabled, everyone allowed
        
        if (_adminService.IsAdmin(email))
            return true; // Admins always allowed
        
        var normalizedEmail = email.ToLowerInvariant();
        return await _db.AllowedUsers
            .AnyAsync(u => u.Email == normalizedEmail && u.IsActive, ct);
    }
    
    // CRUD operations for admin UI
    public async Task<List<AllowedUser>> GetAllowedUsersAsync(CancellationToken ct = default);
    public async Task<AllowedUser> AddAllowedUserAsync(string email, string? displayName, string addedByUserId, CancellationToken ct = default);
    public async Task<bool> RemoveAllowedUserAsync(string id, CancellationToken ct = default);
    public async Task<bool> SetActiveStatusAsync(string id, bool isActive, CancellationToken ct = default);
}
```

### Authentication Event Handler

In `Program.cs`, configure Google OAuth to validate users after successful authentication:

```csharp
.AddGoogle(options =>
{
    // ... existing config ...
    
    options.Events.OnTicketReceived = async context =>
    {
        var email = context.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            context.Fail("Email claim not found.");
            return;
        }
        
        // Get AllowedUserService from DI
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
```

---

## UI Components

### Admin Settings Page (`/admin/settings`)

```razor
@page "/admin/settings"
@attribute [Authorize]
@inject IAdminService AdminService
@inject IAllowedUserService AllowedUserService
@inject AuthenticationStateProvider AuthStateProvider

@if (!_isAdmin)
{
    <Alert Color="AlertColor.Failure" Text="Access Denied" TextEmphasis="You must be an admin to view this page." />
}
else
{
    <Heading Tag="HeadingTag.H1">Settings</Heading>
    
    <!-- Whitelist Section -->
    <Card Class="mb-6">
        <CardBody>
            <Heading Tag="HeadingTag.H2" Class="mb-4">
                User Whitelist
                @if (_whitelistEnabled)
                {
                    <Badge Color="BadgeColor.Green">Enabled</Badge>
                }
                else
                {
                    <Badge Color="BadgeColor.Gray">Disabled</Badge>
                }
            </Heading>
            
            <p class="text-gray-600 dark:text-gray-400 mb-4">
                When enabled, only users in this list (and admins) can sign into the application.
            </p>
            
            <!-- Add User Form -->
            <div class="flex gap-4 mb-6">
                <TextInput @bind-Value="_newEmail" Placeholder="email@example.com" Class="flex-1" />
                <TextInput @bind-Value="_newDisplayName" Placeholder="Display Name (optional)" Class="w-48" />
                <Button Color="ButtonColor.Primary" OnClick="AddUser">Add User</Button>
            </div>
            
            <!-- Users Table -->
            <Table>
                <TableHead>
                    <TableRow>
                        <TableHeadCell>Email</TableHeadCell>
                        <TableHeadCell>Display Name</TableHeadCell>
                        <TableHeadCell>Added</TableHeadCell>
                        <TableHeadCell>Status</TableHeadCell>
                        <TableHeadCell>Actions</TableHeadCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    @foreach (var user in _allowedUsers)
                    {
                        <TableRow>
                            <TableCell>@user.Email</TableCell>
                            <TableCell>@(user.DisplayName ?? "-")</TableCell>
                            <TableCell>@user.CreatedAt.ToString("MMM dd, yyyy")</TableCell>
                            <TableCell>
                                <Badge Color="@(user.IsActive ? BadgeColor.Green : BadgeColor.Gray)">
                                    @(user.IsActive ? "Active" : "Inactive")
                                </Badge>
                            </TableCell>
                            <TableCell>
                                <Button Size="ButtonSize.ExtraSmall" Color="ButtonColor.Alternative" 
                                        OnClick="() => ToggleStatus(user)">
                                    @(user.IsActive ? "Disable" : "Enable")
                                </Button>
                                <Button Size="ButtonSize.ExtraSmall" Color="ButtonColor.Red" 
                                        OnClick="() => RemoveUser(user)">
                                    Remove
                                </Button>
                            </TableCell>
                        </TableRow>
                    }
                </TableBody>
            </Table>
        </CardBody>
    </Card>
}
```

### Access Denied Page (`/Account/AccessDenied`)

```razor
@page "/Account/AccessDenied"
@layout MinimalLayout
@inject IOptions<WhitelistSettings> Settings

<div class="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
    <Card Size="CardSize.Large" Class="max-w-md">
        <CardBody Class="text-center">
            <div class="text-6xl mb-4">🔒</div>
            <Heading Tag="HeadingTag.H1" Class="mb-4">Access Denied</Heading>
            <p class="text-gray-600 dark:text-gray-400 mb-6">
                @Settings.Value.RejectionMessage
            </p>
            <Button Href="/" Color="ButtonColor.Primary">Return Home</Button>
        </CardBody>
    </Card>
</div>
```

---

## Database Configuration

### RiddleDbContext Updates

```csharp
public DbSet<AllowedUser> AllowedUsers => Set<AllowedUser>();

// In OnModelCreating:
builder.Entity<AllowedUser>(entity =>
{
    entity.HasKey(e => e.Id);
    
    // Unique constraint on Email (case-insensitive via storing lowercase)
    entity.HasIndex(e => e.Email).IsUnique();
    
    // Index for active users query
    entity.HasIndex(e => e.IsActive);
    
    entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
    entity.Property(e => e.DisplayName).HasMaxLength(200);
});
```

---

## Configuration

### appsettings.json Addition

```json
{
  "WhitelistSettings": {
    "IsEnabled": true,
    "RejectionMessage": "This application is currently in private beta. Contact the administrator for access."
  }
}
```

### Program.cs Service Registration

```csharp
// Configuration bindings
builder.Services.Configure<WhitelistSettings>(builder.Configuration.GetSection("WhitelistSettings"));

// Services
builder.Services.AddScoped<IAllowedUserService, AllowedUserService>();
```

---

## build.py Commands

Add new database commands for managing whitelist from CLI:

```python
# db users - List all allowed users
# db add-user <email> [display-name] - Add user to whitelist
# db remove-user <email> - Remove user from whitelist
```

Implementation:
```python
def db_users(conn):
    """List all allowed users"""
    cursor = conn.execute("""
        SELECT Email, DisplayName, IsActive, CreatedAt 
        FROM AllowedUsers 
        ORDER BY CreatedAt DESC
    """)
    for row in cursor:
        status = "✓" if row[2] else "✗"
        print(f"{status} {row[0]:<40} {row[1] or '-':<20} {row[3]}")

def db_add_user(conn, email, display_name=None):
    """Add user to whitelist"""
    import uuid
    id = str(uuid.uuid4())  # Or use uuid7 if available
    conn.execute("""
        INSERT INTO AllowedUsers (Id, Email, DisplayName, IsActive, CreatedAt)
        VALUES (?, ?, ?, 1, datetime('now'))
    """, (id, email.lower(), display_name))
    conn.commit()
    print(f"Added: {email}")

def db_remove_user(conn, email):
    """Remove user from whitelist"""
    conn.execute("DELETE FROM AllowedUsers WHERE Email = ?", (email.lower(),))
    conn.commit()
    print(f"Removed: {email}")
```

---

## Testing

### Manual Testing Checklist

1. **Whitelist Enforcement:**
   - [ ] User NOT in whitelist attempts Google sign-in → redirected to AccessDenied page
   - [ ] User IN whitelist attempts Google sign-in → succeeds normally
   - [ ] Admin (in AdminSettings.AdminEmails) always succeeds regardless of whitelist

2. **Admin Settings Page:**
   - [ ] Non-admin user visiting `/admin/settings` → sees "Access Denied" alert
   - [ ] Admin user visiting `/admin/settings` → sees whitelist management UI
   - [ ] Add user form validates email format
   - [ ] Added user appears in table immediately
   - [ ] Toggle status changes badge and persists
   - [ ] Remove user removes from table and database

3. **build.py Commands:**
   - [ ] `python build.py db users` lists all allowed users
   - [ ] `python build.py db add-user test@example.com` adds user
   - [ ] `python build.py db remove-user test@example.com` removes user

4. **Edge Cases:**
   - [ ] Duplicate email add → graceful error (unique constraint)
   - [ ] Email comparison is case-insensitive ("Test@Example.com" matches "test@example.com")
   - [ ] Inactive user cannot sign in
   - [ ] WhitelistSettings.IsEnabled = false → all authenticated users allowed

### Database Verification Commands

```bash
python build.py db users                              # List whitelist
python build.py db "SELECT * FROM AllowedUsers"       # Raw query
python build.py db add-user beta@test.com "Beta Tester"
python build.py db remove-user beta@test.com
```

---

## Implementation Order

1. **Create feature branch**
   ```bash
   git checkout -b feature/user-whitelist
   ```

2. **Add database model and configuration**
   - Create `AllowedUser.cs` model
   - Create `WhitelistSettings.cs` model
   - Update `RiddleDbContext.cs` with DbSet and configuration
   - Create EF migration: `dotnet ef migrations add AddAllowedUsers --project src/Riddle.Web`
   - Apply migration: `dotnet ef database update --project src/Riddle.Web`

3. **Create AllowedUserService**
   - Create `IAllowedUserService.cs` interface
   - Create `AllowedUserService.cs` implementation

4. **Update Program.cs**
   - Bind `WhitelistSettings` configuration
   - Register `IAllowedUserService`
   - Add Google OAuth `OnTicketReceived` event handler

5. **Create UI pages**
   - Create `/Account/AccessDenied.razor` page
   - Create `/Admin/Settings.razor` page

6. **Update navigation**
   - Add "Settings" link to `AppSidebar.razor` (admin-only)

7. **Update build.py**
   - Add `db users` command
   - Add `db add-user` command
   - Add `db remove-user` command

8. **Add appsettings.json configuration**
   - Add `WhitelistSettings` section

9. **Seed initial whitelist (optional)**
   - Pre-populate with beta tester emails via `build.py` or migration

10. **Testing and verification**
    - Run through manual testing checklist
    - Test with both whitelisted and non-whitelisted Google accounts
    - Verify admin bypass works correctly

11. **Documentation update**
    - Update `docs/memory_aid.md` with any gotchas discovered
    - Update CHANGELOG.md

---

## Security Considerations

### Authentication Flow

```
User clicks "Sign in with Google"
        ↓
Google OAuth completes → OnTicketReceived fires
        ↓
Check email against AllowedUsers table
        ↓
    ┌───────────────────────────────────┐
    │ Email found & active?             │
    │   OR email in AdminSettings?      │
    └───────────────────────────────────┘
        ↓ YES                    ↓ NO
   Continue auth           Redirect to /Account/AccessDenied
   (user signs in)         (no cookie set, not authenticated)
```

**Key Points:**
- Rejection happens BEFORE the auth cookie is set
- Unauthorized users never become authenticated in the system
- No partial state - either fully signed in or fully rejected

### Admin-Only Access

The Settings page performs double-validation:
1. `[Authorize]` attribute ensures user is authenticated
2. `IAdminService.IsAdmin()` check ensures user is in admin list

If a non-admin somehow navigates to `/admin/settings`, they see an "Access Denied" alert but the page renders (no sensitive data exposed before the check).

---

## Alternative Approaches Considered

### 1. Middleware-based Approach
**Pros:** Could block requests before hitting Blazor components
**Cons:** Would run on every request (performance), harder to integrate with SignalR

### 2. Claims Transformation
**Pros:** Could add a "WhitelistApproved" claim during sign-in
**Cons:** Still need to check the whitelist database, adds complexity

### 3. Authorization Policy
**Pros:** Could use `[Authorize(Policy = "WhitelistApproved")]`
**Cons:** Requires checking database on every authorized request, better suited for feature flags than sign-in gate

**Decision:** `OnTicketReceived` event is the cleanest approach because:
- Runs only during sign-in (not every request)
- Can reject before auth cookie is issued
- Simple to implement and understand

---

## Rollback Plan

If issues arise, the feature can be disabled without code deployment:

1. **Quick disable (appsettings):** Set `WhitelistSettings.IsEnabled = false`
   - All authenticated users allowed immediately
   - No restart required (if using hot-reload in dev)

2. **Database bypass:** Delete all rows from `AllowedUsers` and disable
   - `python build.py db "DELETE FROM AllowedUsers"`

3. **Code rollback:** Revert the `OnTicketReceived` handler in Program.cs
   - Removes whitelist check entirely

---

## Future Enhancements (Out of Scope)

- **Invite codes:** Generate shareable codes that add users to whitelist
- **Expiring access:** Auto-disable whitelist entries after X days
- **Request access form:** Let non-whitelisted users request access
- **Audit log:** Track who added/removed whitelist entries
- **Email notifications:** Notify admins when new users request access
