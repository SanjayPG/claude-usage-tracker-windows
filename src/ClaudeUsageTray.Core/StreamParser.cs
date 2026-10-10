using System.Text.Json;

namespace ClaudeUsageTray.Core;

// Reads the `rate_limit_event` line that `claude -p --output-format stream-json --verbose` prints.
// `utilization` there is a fraction (0.04 = 4%) and `resetsAt` is Unix seconds.
public static class StreamParser
{
  public static UsageSnapshot? TryParse(string? output, DateTimeOffset now)
  {
    if (string.IsNullOrWhiteSpace(output))
    {
      return null;
    }

    UsageSnapshot? found = null;
    foreach (var raw in output.Split('\n'))
    {
      var line = raw.Trim();
      if (line.Length == 0 || line[0] != '{')
      {
        continue;
      }

      var snapshot = ParseLine(line, now);
      if (snapshot is not null)
      {
        // Keep the last event: it is the freshest reading in the stream.
        found = snapshot;
      }
    }

    return found;
  }

  private static UsageSnapshot? ParseLine(string line, DateTimeOffset now)
  {
    try
    {
      using var doc = JsonDocument.Parse(line);
      var root = doc.RootElement;
      if (root.ValueKind != JsonValueKind.Object
        || !root.TryGetProperty("type", out var type)
        || type.ValueKind != JsonValueKind.String
        || type.GetString() != "rate_limit_event"
        || !root.TryGetProperty("rate_limit_info", out var info)
        || info.ValueKind != JsonValueKind.Object
        || !info.TryGetProperty("unifiedWindows", out var windows)
        || windows.ValueKind != JsonValueKind.Object)
      {
        return null;
      }

      var five = ParseWindow(windows, "five_hour");
      var seven = ParseWindow(windows, "seven_day");

      return five is null && seven is null ? null : new UsageSnapshot(now, five, seven);
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private static UsageWindow? ParseWindow(JsonElement windows, string name)
  {
    if (!windows.TryGetProperty(name, out var window)
      || window.ValueKind != JsonValueKind.Object
      || !window.TryGetProperty("utilization", out var utilization)
      || utilization.ValueKind != JsonValueKind.Number)
    {
      return null;
    }

    DateTimeOffset? resets = null;
    if (window.TryGetProperty("resetsAt", out var at) && at.ValueKind == JsonValueKind.Number && at.TryGetInt64(out var seconds))
    {
      try
      {
        resets = DateTimeOffset.FromUnixTimeSeconds(seconds);
      }
      catch (ArgumentOutOfRangeException)
      {
        // An absurd timestamp is treated as unknown rather than failing the whole reading.
      }
    }

    return new UsageWindow(Math.Round(Format.Clamp(utilization.GetDouble() * 100), 1), resets);
  }
}
