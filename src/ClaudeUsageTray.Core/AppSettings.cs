using System.Text.Json;

namespace ClaudeUsageTray.Core;

// Auto-refresh costs a little quota each time, so it is off (0) until you turn it on.
public sealed record AppSettings(int AutoRefreshMinutes = 0)
{
  public const int AutoRefreshChoice = 30;

  public bool IsAutoRefreshOn => AutoRefreshMinutes > 0;

  public static string DefaultPath() =>
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "ClaudeUsageTray",
      "settings.json");

  public static AppSettings Load(string path)
  {
    try
    {
      if (!File.Exists(path))
      {
        return new AppSettings();
      }

      var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path));

      return loaded is { AutoRefreshMinutes: >= 0 } ? loaded : new AppSettings();
    }
    catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
    {
      return new AppSettings();
    }
  }

  public void Save(string path)
  {
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, JsonSerializer.Serialize(this));
  }
}
