using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kestrel.Core.Session;

/// <summary>Loads and saves window/session state as JSON. A missing or corrupt file never blocks startup.</summary>
public static class AppStateStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kestrel", "state.json");

    public static AppState Load(string path)
    {
        if (!File.Exists(path)) return new AppState();
        try
        {
            var state = JsonSerializer.Deserialize<AppState>(File.ReadAllText(path), Options);
            return Sanitize(state ?? new AppState());
        }
        catch (JsonException)
        {
            SetAside(path);
            return new AppState();
        }
        catch (IOException)
        {
            return new AppState();
        }
        catch (UnauthorizedAccessException)
        {
            return new AppState();
        }
    }

    public static void Save(string path, AppState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(Sanitize(state), Options));
        File.Move(temp, path, overwrite: true);
    }

    /// <summary>Clamps values from a hand-edited or older state file into safe ranges.</summary>
    public static AppState Sanitize(AppState state)
    {
        state.Theme = state.Theme == "Light" ? "Light" : "Dark";
        state.Zoom = Math.Clamp(state.Zoom, -10, 20);
        state.OpenFiles = (state.OpenFiles ?? new List<SessionFile>())
            .Where(f => f is not null && !string.IsNullOrWhiteSpace(f.Path))
            .ToList();
        foreach (var f in state.OpenFiles)
        {
            f.CaretPosition = Math.Max(0, f.CaretPosition);
            f.FirstVisibleLine = Math.Max(0, f.FirstVisibleLine);
        }
        state.ActiveIndex = state.OpenFiles.Count == 0 ? 0 : Math.Clamp(state.ActiveIndex, 0, state.OpenFiles.Count - 1);
        return state;
    }

    private static void SetAside(string path)
    {
        try { File.Copy(path, path + ".bad", overwrite: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
