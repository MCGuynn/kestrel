using Kestrel.Core.Languages;
using Kestrel.Core.Session;

namespace Kestrel.Tests;

public sealed class AppStateStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"kestrel-tests-{Guid.NewGuid():N}");

    private string StatePath => Path.Combine(_dir, "state.json");

    [Fact]
    public void Missing_file_returns_defaults()
    {
        var state = AppStateStore.Load(StatePath);
        Assert.Equal("Dark", state.Theme);
        Assert.Empty(state.OpenFiles);
    }

    [Fact]
    public void Round_trips_state()
    {
        var state = new AppState
        {
            Theme = "Light",
            WordWrap = true,
            Zoom = 3,
            Window = new WindowBounds(10, 20, 800, 600, false),
            OpenFiles = { new SessionFile { Path = "C:/temp/a.sql", CaretPosition = 42, LanguageOverride = LanguageId.Sql } },
        };
        AppStateStore.Save(StatePath, state);

        var loaded = AppStateStore.Load(StatePath);
        Assert.Equal("Light", loaded.Theme);
        Assert.True(loaded.WordWrap);
        Assert.Equal(3, loaded.Zoom);
        Assert.Equal(state.Window, loaded.Window);
        var file = Assert.Single(loaded.OpenFiles);
        Assert.Equal(42, file.CaretPosition);
        Assert.Equal(LanguageId.Sql, file.LanguageOverride);
    }

    [Fact]
    public void Corrupt_file_is_set_aside_and_defaults_used()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(StatePath, "{ not json");
        var state = AppStateStore.Load(StatePath);
        Assert.Equal("Dark", state.Theme);
        Assert.True(File.Exists(StatePath + ".bad"));
    }

    [Fact]
    public void Sanitize_clamps_bad_values()
    {
        var state = AppStateStore.Sanitize(new AppState
        {
            Theme = "Neon",
            Zoom = 99,
            ActiveIndex = 5,
            OpenFiles = { new SessionFile { Path = "" }, new SessionFile { Path = "a.txt", CaretPosition = -4 } },
        });
        Assert.Equal("Dark", state.Theme);
        Assert.Equal(20, state.Zoom);
        var file = Assert.Single(state.OpenFiles);
        Assert.Equal(0, file.CaretPosition);
        Assert.Equal(0, state.ActiveIndex);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }
}
