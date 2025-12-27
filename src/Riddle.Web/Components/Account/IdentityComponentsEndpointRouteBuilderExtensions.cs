using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Riddle.Web.Models;

namespace Riddle.Web.Components.Account;

/// <summary>
/// Extension methods to map additional Identity endpoints for external login, logout, etc.
/// </summary>
internal static class IdentityComponentsEndpointRouteBuilderExtensions
{
    // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var accountGroup = endpoints.MapGroup("/");

        // POST endpoint to initiate external login (Google)
        accountGroup.MapPost("/PerformExternalLogin", async (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromForm] string provider,
            [FromForm] string returnUrl) =>
        {
            IEnumerable<KeyValuePair<string, StringValues>> query = [
                new("ReturnUrl", returnUrl),
                new("Action", "login-callback")
            ];

            var redirectUrl = UriHelper.BuildRelative(
                context.Request.PathBase,
                "/Account/ExternalLogin",
                QueryString.Create(query));

            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return TypedResults.Challenge(properties, [provider]);
        });

        // POST endpoint to perform logout (legacy support)
        accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            SignInManager<ApplicationUser> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        });

        // GET endpoint to perform logout (for Blazor InteractiveServer mode)
        accountGroup.MapGet("/PerformLogout", async (
            SignInManager<ApplicationUser> signInManager,
            [FromQuery] string? returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl ?? "Account/Login"}");
        });

        // GET endpoint for external login callback (processes the OAuth response)
        accountGroup.MapGet("/ExternalLogin", async (
            HttpContext context,
            [FromServices] SignInManager<ApplicationUser> signInManager,
            [FromServices] UserManager<ApplicationUser> userManager,
            [FromQuery] string? returnUrl,
            [FromQuery] string? remoteError,
            [FromQuery] string? action) =>
        {
            returnUrl ??= "/";

            if (remoteError is not null)
            {
                return Results.Redirect($"/Account/Login?error={Uri.EscapeDataString($"Error from external provider: {remoteError}")}");
            }

            var info = await signInManager.GetExternalLoginInfoAsync();
            if (info is null)
            {
                return Results.Redirect("/Account/Login?error=Error+loading+external+login+information");
            }

            // Get the picture URL from the external provider
            var pictureUrl = info.Principal.FindFirstValue("picture");

            // Sign in the user with this external login provider if the user already has a login
            var result = await signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: true,  // Remember the user
                bypassTwoFactor: true);

            if (result.Succeeded)
            {
                // Update any authentication tokens if they've changed
                await signInManager.UpdateExternalAuthenticationTokensAsync(info);
                
                // Update picture claim for returning user
                var user = await userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user is not null && !string.IsNullOrEmpty(pictureUrl))
                {
                    await UpdatePictureClaimAsync(userManager, signInManager, user, pictureUrl);
                }
                
                return Results.LocalRedirect(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return Results.Redirect("/Account/Lockout");
            }

            // If the user does not have an account, create one
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var name = info.Principal.FindFirstValue(ClaimTypes.Name);
            
            if (string.IsNullOrEmpty(email))
            {
                return Results.Redirect("/Account/Login?error=Email+not+provided+by+external+provider");
            }

            // Check if user with this email already exists
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser is not null)
            {
                // Link the external login to existing account
                var addLoginResult = await userManager.AddLoginAsync(existingUser, info);
                if (addLoginResult.Succeeded)
                {
                    await signInManager.SignInAsync(existingUser, isPersistent: true);
                    await signInManager.UpdateExternalAuthenticationTokensAsync(info);
                    return Results.LocalRedirect(returnUrl);
                }
                
                var errors = string.Join(", ", addLoginResult.Errors.Select(e => e.Description));
                return Results.Redirect($"/Account/Login?error={Uri.EscapeDataString(errors)}");
            }

            // Create new user with UUID v7
            var newUser = new ApplicationUser
            {
                Id = Guid.CreateVersion7().ToString(),
                UserName = email,
                Email = email,
                DisplayName = name ?? email.Split('@')[0],
                EmailConfirmed = true // External providers verify email
            };

            var createResult = await userManager.CreateAsync(newUser);
            if (createResult.Succeeded)
            {
                var addLoginResult = await userManager.AddLoginAsync(newUser, info);
                if (addLoginResult.Succeeded)
                {
                    // Add picture claim for new user
                    if (!string.IsNullOrEmpty(pictureUrl))
                    {
                        await userManager.AddClaimAsync(newUser, new Claim("picture", pictureUrl));
                    }
                    
                    await signInManager.SignInAsync(newUser, isPersistent: true);
                    await signInManager.UpdateExternalAuthenticationTokensAsync(info);
                    return Results.LocalRedirect(returnUrl);
                }
            }

            var createErrors = string.Join(", ", createResult.Errors.Select(e => e.Description));
            return Results.Redirect($"/Account/Login?error={Uri.EscapeDataString(createErrors)}");
        });

        return accountGroup;
    }

    /// <summary>
    /// Updates the picture claim for a user, replacing any existing picture claim
    /// and re-signing them in so the new claim is included in the cookie.
    /// </summary>
    private static async Task UpdatePictureClaimAsync(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationUser user,
        string pictureUrl)
    {
        // Get existing claims and check if picture needs updating
        var existingClaims = await userManager.GetClaimsAsync(user);
        var existingPictureClaim = existingClaims.FirstOrDefault(c => c.Type == "picture");
        
        if (existingPictureClaim?.Value != pictureUrl)
        {
            // Remove old picture claim if it exists
            if (existingPictureClaim is not null)
            {
                await userManager.RemoveClaimAsync(user, existingPictureClaim);
            }
            
            // Add updated picture claim
            await userManager.AddClaimAsync(user, new Claim("picture", pictureUrl));
        }
        
        // Re-sign in the user to refresh their cookie with the updated claims
        await signInManager.RefreshSignInAsync(user);
    }
}
