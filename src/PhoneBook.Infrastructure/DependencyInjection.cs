using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PhoneBook.Application.Abstractions.Persistence;
using PhoneBook.Infrastructure.Data;
using PhoneBook.Infrastructure.Identity;
using PhoneBook.Infrastructure.Persistence;

namespace PhoneBook.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPhoneBookInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();
        services.AddSingleton<IPhoneBookRepository, EfPhoneBookRepository>();
        services.AddSingleton<IAppSettingsRepository, EfAppSettingsRepository>();
        services.AddScoped<AdminBootstrapper>();
        return services;
    }
}
