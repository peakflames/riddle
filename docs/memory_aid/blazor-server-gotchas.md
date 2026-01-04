# Blazor Server Platform Gotchas

> **Keywords:** HttpClient, IWebHostEnvironment, .NET 10, blazor.web.js, MapStaticAssets, shell commands, GUID
> **Related:** [Flowbite Blazor](./flowbite-blazor.md), [Blazor Components](./blazor-components.md)

This document covers platform-specific gotchas for Blazor Server development.

---

## Blazor Server vs WASM

- This project uses **Blazor Server** with `InteractiveServer` render mode, NOT WASM
- The Flowbite Blazor Admin Dashboard reference is WASM - don't blindly copy App.razor/Program.cs
- Interactive pages need `@rendermode InteractiveServer` directive

---

## CRITICAL: HttpClient Not Available in Blazor Server by Default

**Problem:** `HttpClient` is NOT registered in DI by default for Blazor Server apps. Using `@inject HttpClient Http` causes runtime exceptions when the component renders.

**Error:** `InvalidOperationException: Cannot provide a value for property 'Http' on type 'YourComponent'. There is no registered service of type 'System.Net.Http.HttpClient'.`

**Root cause:** Blazor WASM automatically configures `HttpClient` for same-origin requests. Blazor Server does NOT because server-side code can access files/APIs directly without HTTP.

**Fix for loading files from wwwroot:**

❌ **WRONG - HttpClient (WASM pattern):**
```csharp
@inject HttpClient Http

var content = await Http.GetStringAsync("docs/my-file.md");  // Fails in Blazor Server!
```

✅ **CORRECT - IWebHostEnvironment (Server pattern):**
```csharp
@inject IWebHostEnvironment WebHostEnvironment

var filePath = Path.Combine(WebHostEnvironment.WebRootPath, "docs", "my-file.md");
var content = await File.ReadAllTextAsync(filePath);  // Works in Blazor Server
```

**Alternative:** If you need HttpClient for external API calls, register it explicitly in Program.cs:
```csharp
builder.Services.AddHttpClient();  // Basic registration
// Or for typed clients:
builder.Services.AddHttpClient<IMyApiClient, MyApiClient>();
```

---

## .NET 10 Blazor Server Setup

- Use `blazor.web.js` (not `blazor.server.js`) with `@Assets["_framework/blazor.web.js"]` syntax
- App.razor needs: `<ResourcePreloader />`, `<ImportMap />`, `<ReconnectModal />`
- Program.cs: Use `MapStaticAssets()` instead of `UseStaticFiles()`
- For non-development runs: Add `builder.WebHost.UseStaticWebAssets()` before building
- Generate reference: `dotnet new blazor -int Server` in tmp/ folder for correct patterns

---

## Package Management

- For .NET 10 preview packages: `dotnet add package {Name} --prerelease`
- Use `--version 10.0.1` for specific versions

---

## Windows Shell Commands

- Don't use Unix commands like `find /i` - use PowerShell: `Select-String -Pattern "error"`
- `sqlite3` may not be installed - verify database via EF Core or migration files

---

## UUID/GUID

- Use `Guid.CreateVersion7()` for time-sortable IDs (requires .NET 9+)
