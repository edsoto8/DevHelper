using DevHelper.Web;
using DevHelper.Web.Components;
using Microsoft.AspNetCore.DataProtection;
using DevHelper.Web.Data;
using DevHelper.Web.Repositories;
using DevHelper.Web.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog from configuration (Console + rolling File sinks).
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Options.
builder.Services.Configure<DevHelperOptions>(
    builder.Configuration.GetSection(DevHelperOptions.SectionName));

// Data Protection with persisted keys so encrypted settings survive restarts.
var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
Directory.CreateDirectory(keysDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
    .SetApplicationName("DevHelper");

// Data access.
builder.Services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();

// Repositories.
builder.Services.AddScoped<ISystemRepository, SystemRepository>();
builder.Services.AddScoped<IServerRepository, ServerRepository>();
builder.Services.AddScoped<IDatabaseRepository, DatabaseRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<IReleasePlanRepository, ReleasePlanRepository>();
builder.Services.AddScoped<IApplicationSettingRepository, ApplicationSettingRepository>();

// Services.
builder.Services.AddScoped<IApplicationSettingsService, ApplicationSettingsService>();
builder.Services.AddScoped<IMarkdownGenerationService, MarkdownGenerationService>();
builder.Services.AddScoped<IReleasePlanService, ReleasePlanService>();
builder.Services.AddScoped<IScreenshotService, ScreenshotService>();
builder.Services.AddScoped<IExternalSqlServerConnectionService, ExternalSqlServerConnectionService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Initialize the database and apply migrations before serving requests.
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
    await initializer.InitializeAsync();
}

Log.Information("DevHelper starting up.");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseSerilogRequestLogging();

app.MapStaticAssets();

// Serves screenshot previews only through the validated source directory resolver.
app.MapGet("/screenshots/preview", async (string path, IScreenshotService screenshots) =>
{
    var resolved = await screenshots.ResolveExistingAsync(path);
    if (resolved is null)
    {
        return Results.NotFound();
    }

    var contentType = Path.GetExtension(resolved).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "application/octet-stream",
    };
    return Results.File(resolved, contentType);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "DevHelper terminated unexpectedly.");
}
finally
{
    Log.Information("DevHelper shutting down.");
    Log.CloseAndFlush();
}
