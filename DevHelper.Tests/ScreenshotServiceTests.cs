using DevHelper.Web.Services;

namespace DevHelper.Tests;

public sealed class ScreenshotServiceTests : IDisposable
{
    private readonly string _sourceDir;
    private readonly ScreenshotService _service;

    public ScreenshotServiceTests()
    {
        _sourceDir = Path.Combine(Path.GetTempPath(), "devhelper-shots", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_sourceDir);
        _service = new ScreenshotService(new FakeScreenshotSettings(_sourceDir),
            TestFactories.Logger<ScreenshotService>());
    }

    private string CreateFile(string name)
    {
        var path = Path.Combine(_sourceDir, name);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "fake-image-bytes");
        return path;
    }

    [Fact]
    public async Task Lists_only_supported_image_extensions()
    {
        CreateFile("a.png");
        CreateFile("b.jpg");
        CreateFile("c.txt");
        CreateFile("d.gif");

        var files = await _service.ListAvailableAsync();

        Assert.Equal(2, files.Count);
        Assert.Contains(files, f => f.FileName == "a.png");
        Assert.Contains(files, f => f.FileName == "b.jpg");
    }

    [Fact]
    public async Task Validation_stores_relative_path()
    {
        CreateFile(Path.Combine("sub", "shot.png"));

        var result = await _service.ValidateForStorageAsync(Path.Combine("sub", "shot.png"));

        Assert.True(result.IsValid);
        Assert.Equal(Path.Combine("sub", "shot.png"), result.RelativePath);
    }

    [Fact]
    public async Task Validation_rejects_unsupported_extension()
    {
        CreateFile("notes.txt");
        var result = await _service.ValidateForStorageAsync("notes.txt");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validation_rejects_missing_file()
    {
        var result = await _service.ValidateForStorageAsync("does-not-exist.png");
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validation_rejects_path_outside_source_directory()
    {
        var outside = Path.Combine(Path.GetTempPath(), "outside-" + Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllText(outside, "x");
        try
        {
            var result = await _service.ValidateForStorageAsync(Path.Combine("..", Path.GetFileName(outside)));
            Assert.False(result.IsValid);
        }
        finally
        {
            File.Delete(outside);
        }
    }

    [Fact]
    public async Task Resolve_returns_null_for_missing_and_does_not_delete()
    {
        var path = CreateFile("present.png");

        Assert.NotNull(await _service.ResolveExistingAsync("present.png"));
        Assert.Null(await _service.ResolveExistingAsync("absent.png"));

        // Resolving never deletes the underlying file.
        Assert.True(File.Exists(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir))
        {
            Directory.Delete(_sourceDir, recursive: true);
        }
    }

    private sealed class FakeScreenshotSettings : IApplicationSettingsService
    {
        private readonly string _dir;
        public FakeScreenshotSettings(string dir) => _dir = dir;

        public Task<string?> GetAsync(string key) => Task.FromResult<string?>(_dir);
        public Task SetAsync(string key, string? value) => Task.CompletedTask;
        public Task<string?> GetScreenshotSourceDirectoryAsync() => Task.FromResult<string?>(_dir);
        public Task<string?> GetDefaultEnvironmentAsync() => Task.FromResult<string?>(null);
        public Task<string?> GetTicketLookupConnectionStringAsync() => Task.FromResult<string?>(null);
    }
}
