using QuestPDF.Infrastructure;
using ReleasePlanGenerator.Web.Components;
using ReleasePlanGenerator.Web.Data;
using ReleasePlanGenerator.Web.Repositories;
using ReleasePlanGenerator.Web.Services;
using Serilog;

QuestPDF.Settings.License = LicenseType.Community;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/release-plan-generator-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Release Plan Generator.");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // Data access
    builder.Services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
    builder.Services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

    // Repositories
    builder.Services.AddScoped<IReleasePlanRepository, ReleasePlanRepository>();
    builder.Services.AddScoped<IReleaseTemplateRepository, ReleaseTemplateRepository>();
    builder.Services.AddScoped<ISystemRepository, SystemRepository>();
    builder.Services.AddScoped<IServerRepository, ServerRepository>();
    builder.Services.AddScoped<IDatabaseRepository, DatabaseRepository>();
    builder.Services.AddScoped<IApplicationSettingRepository, ApplicationSettingRepository>();

    // Services
    builder.Services.AddScoped<IMarkdownGenerationService, MarkdownGenerationService>();
    builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();
    builder.Services.AddScoped<IApplicationSettingsService, ApplicationSettingsService>();
    builder.Services.AddScoped<IExternalSqlServerConnectionService, ExternalSqlServerConnectionService>();

    var app = builder.Build();

    // Run DB migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        await initializer.InitializeAsync();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseAntiforgery();
    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed.");
}
finally
{
    Log.CloseAndFlush();
}
