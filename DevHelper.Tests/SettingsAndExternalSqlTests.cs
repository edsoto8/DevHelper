using DevHelper.Web.Repositories;
using DevHelper.Web.Services;

namespace DevHelper.Tests;

public sealed class SettingsAndExternalSqlTests
{
    private static ApplicationSettingsService SettingsService(TestDatabase db)
    {
        var repo = new ApplicationSettingRepository(db.ConnectionFactory, TestFactories.Logger<ApplicationSettingRepository>());
        return new ApplicationSettingsService(repo, TestFactories.DataProtection(),
            TestFactories.Logger<ApplicationSettingsService>());
    }

    [Fact]
    public async Task Sensitive_setting_is_encrypted_at_rest_and_decrypts_via_service()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = SettingsService(db);
        const string plaintext = "Server=sql01;Database=Tickets;User Id=sa;Password=Secret123;";

        await service.SetAsync(SettingKeys.TicketLookupSqlServerConnectionString, plaintext);

        // Raw stored value must not be plaintext, and must be flagged encrypted.
        var repo = new ApplicationSettingRepository(db.ConnectionFactory, TestFactories.Logger<ApplicationSettingRepository>());
        var stored = await repo.GetAsync(SettingKeys.TicketLookupSqlServerConnectionString);
        Assert.NotNull(stored);
        Assert.True(stored!.IsEncrypted);
        Assert.NotEqual(plaintext, stored.SettingValue);
        Assert.DoesNotContain("Password", stored.SettingValue!);

        // Service round-trips the value.
        Assert.Equal(plaintext, await service.GetAsync(SettingKeys.TicketLookupSqlServerConnectionString));
    }

    [Fact]
    public async Task Non_sensitive_setting_is_stored_in_clear()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = SettingsService(db);

        await service.SetAsync(SettingKeys.DefaultEnvironment, "QA");

        var repo = new ApplicationSettingRepository(db.ConnectionFactory, TestFactories.Logger<ApplicationSettingRepository>());
        var stored = await repo.GetAsync(SettingKeys.DefaultEnvironment);
        Assert.False(stored!.IsEncrypted);
        Assert.Equal("QA", stored.SettingValue);
    }

    [Fact]
    public async Task External_sql_test_reports_missing_connection_string()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new ExternalSqlServerConnectionService(
            SettingsService(db), TestFactories.Logger<ExternalSqlServerConnectionService>());

        Assert.False(await service.HasConnectionStringAsync());
        var result = await service.TestConnectionAsync();
        Assert.False(result.Success);
    }

    [Fact]
    public async Task External_sql_test_fails_gracefully_for_unreachable_server()
    {
        await using var db = await TestDatabase.CreateAsync();
        var service = new ExternalSqlServerConnectionService(
            SettingsService(db), TestFactories.Logger<ExternalSqlServerConnectionService>());

        // Unroutable host with a short timeout; must return false, never throw.
        var result = await service.TestConnectionAsync(
            "Server=192.0.2.1,1433;Database=x;User Id=sa;Password=x;Connect Timeout=2;TrustServerCertificate=True;");

        Assert.False(result.Success);
    }
}
