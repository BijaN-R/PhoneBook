using Microsoft.AspNetCore.Identity;

namespace PhoneBook.Infrastructure.Identity;

public sealed class AdminBootstrapper(
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager)
{
    public const string AdminRoleName = "Admin";

    public async Task BootstrapAsync(
        string? configuredEmail,
        string? configuredPassword,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (!await roleManager.RoleExistsAsync(AdminRoleName))
        {
            IdentityResult roleResult = await roleManager.CreateAsync(
                new IdentityRole(AdminRoleName));
            ThrowIfFailed(roleResult, "creating the Admin role");
        }

        IList<ApplicationUser> administrators = await userManager.GetUsersInRoleAsync(
            AdminRoleName);
        if (administrators.Count > 0)
        {
            return;
        }

        bool hasEmail = !string.IsNullOrWhiteSpace(configuredEmail);
        bool hasPassword = !string.IsNullOrWhiteSpace(configuredPassword);
        if (!hasEmail && !hasPassword)
        {
            return;
        }

        if (!hasEmail || !hasPassword)
        {
            throw new InvalidOperationException(
                "Admin bootstrap requires both AdminBootstrap:Email and AdminBootstrap:Password.");
        }

        string email = configuredEmail!.Trim();
        ApplicationUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };
            IdentityResult createResult = await userManager.CreateAsync(user, configuredPassword!);
            ThrowIfFailed(createResult, "creating the initial administrator");
        }

        if (!await userManager.IsInRoleAsync(user, AdminRoleName))
        {
            IdentityResult roleResult = await userManager.AddToRoleAsync(user, AdminRoleName);
            ThrowIfFailed(roleResult, "assigning the Admin role");
        }
    }

    private static void ThrowIfFailed(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        string errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Identity failed while {operation}: {errors}");
    }
}
