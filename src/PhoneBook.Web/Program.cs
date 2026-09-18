// FILE: src/PhoneBook.Web/Program.cs
using Microsoft.Data.Sqlite;
using PhoneBook.Application.Services;
using PhoneBook.Core.Layout;
using PhoneBook.Export.Image.Skia;
using PhoneBook.Export.OpenXml.OpenXml;
using PhoneBook.Infrastructure;
using PhoneBook.Web.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

string connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");
string databasePath = ResolveDatabasePath(connectionString, builder.Environment.ContentRootPath);

if (!File.Exists(databasePath))
{
    throw new InvalidOperationException(
        $"Phone book database was not found at '{databasePath}'. "
        + "Create it manually with 'dotnet ef migrations add InitialCreate' (if needed) "
        + "and 'dotnet ef database update' before starting the application.");
}

string configuredFontsPath = builder.Configuration["Fonts:BasePath"] ?? "wwwroot/fonts";
string fontsPath = Path.IsPathRooted(configuredFontsPath)
    ? Path.GetFullPath(configuredFontsPath)
    : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, configuredFontsPath));

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddPhoneBookInfrastructure(connectionString);

builder.Services.AddSingleton(_ => new FontProvider(fontsPath));
builder.Services.AddSingleton<ITextMeasurer, SkiaTextMeasurer>();
builder.Services.AddSingleton<LayoutEngine>();
builder.Services.AddSingleton<PhoneBookQueryService>();
builder.Services.AddSingleton<GroupManagementService>();
builder.Services.AddSingleton<EntryManagementService>();
builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<PhoneBookSearchService>();
builder.Services.AddSingleton<PhoneBookDocumentService>();
builder.Services.AddSingleton(serviceProvider => new OpenXmlPhoneBookGenerator(
    serviceProvider.GetRequiredService<ITextMeasurer>(),
    fontsPath));
builder.Services.AddSingleton(_ => new SkiaFontRegistry(fontsPath));
builder.Services.AddSingleton<SkiaPageRenderer>();
builder.Services.AddSingleton<SkiaSharpImageGenerator>();
builder.Services.AddSingleton<SkiaSharpPdfGenerator>();
builder.Services.AddSingleton<SkiaExportService>();

WebApplication app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

try
{
    await app.Services.GetRequiredService<PhoneBookSearchService>().RefreshAsync();
}
catch (Exception exception) when (exception is SqliteException or InvalidOperationException)
{
    throw new InvalidOperationException(
        "The phone book database exists but is not initialized correctly. "
        + "Run 'dotnet ef database update' before starting the application.",
        exception);
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string ResolveDatabasePath(string connectionString, string contentRootPath)
{
    SqliteConnectionStringBuilder connectionBuilder = new(connectionString);
    string dataSource = connectionBuilder.DataSource;
    if (string.IsNullOrWhiteSpace(dataSource) || dataSource == ":memory:")
    {
        throw new InvalidOperationException("The application requires a file-backed SQLite database.");
    }

    return Path.IsPathRooted(dataSource)
        ? Path.GetFullPath(dataSource)
        : Path.GetFullPath(Path.Combine(contentRootPath, dataSource));
}

public partial class Program;
