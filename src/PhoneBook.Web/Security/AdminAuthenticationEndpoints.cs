using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using PhoneBook.Infrastructure.Identity;

namespace PhoneBook.Web.Security;

public static class AdminAuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAdminAuthentication(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/admin/sign-in", LoginAsync).AllowAnonymous();
        endpoints.MapPost("/admin/logout", LogoutAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery)
    {
        if (!await IsValidAntiforgeryRequestAsync(context, antiforgery))
        {
            return Results.BadRequest();
        }

        IFormCollection form = await context.Request.ReadFormAsync(context.RequestAborted);
        string email = form["email"].ToString().Trim();
        string password = form["password"].ToString();
        string returnUrl = GetLocalReturnUrl(form["returnUrl"]);

        SignInResult result = await signInManager.PasswordSignInAsync(
            email,
            password,
            isPersistent: false,
            lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return Results.LocalRedirect(returnUrl);
        }

        string location = "/admin/login?error=invalid&returnUrl="
            + Uri.EscapeDataString(returnUrl);
        return Results.LocalRedirect(location);
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext context,
        SignInManager<ApplicationUser> signInManager,
        IAntiforgery antiforgery)
    {
        if (!await IsValidAntiforgeryRequestAsync(context, antiforgery))
        {
            return Results.BadRequest();
        }

        await signInManager.SignOutAsync();
        return Results.LocalRedirect("/");
    }

    private static async Task<bool> IsValidAntiforgeryRequestAsync(
        HttpContext context,
        IAntiforgery antiforgery)
    {
        try
        {
            await antiforgery.ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            return false;
        }
    }

    private static string GetLocalReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)
            || !returnUrl.StartsWith("/", StringComparison.Ordinal)
            || returnUrl.StartsWith("//", StringComparison.Ordinal)
            || returnUrl.StartsWith("/\\", StringComparison.Ordinal))
        {
            return "/";
        }

        return returnUrl;
    }
}
