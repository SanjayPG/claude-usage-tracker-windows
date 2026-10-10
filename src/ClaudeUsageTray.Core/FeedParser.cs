using System.Text.Json;

namespace ClaudeUsageTray.Core;

public static class FeedParser
{
  // Returns null for anything that is not a usable feed, so a half-written file never throws.
  public static UsageSnapshot? TryParse(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return null;
    }

    try
    {
      using var doc = JsonDocument.Parse(json);
      var root = doc.RootElement;
      if (root.ValueKind != JsonValueKind.Object)
      {
        return null;
      }

      if (!root.TryGetProperty("updatedAt", out var updated) || !TryDate(updated, out var updatedAt))
      {
        return null;
      }

      var five = ParseWindow(root, "five_hour");
      var seven = ParseWindow(root, "seven_day");

      return five is null && seven is null ? null : new UsageSnapshot(updatedAt, five, seven);
    }
    catch (JsonException)
    {
      return null;
    }
  }

  private static UsageWindow? ParseWindow(JsonElement root, string name)
  {
    if (!root.TryGetProperty(name, out var win) || win.ValueKind != JsonValueKind.Object)
    {
      return null;
    }

    if (!win.TryGetProperty("usedPercent", out var used) || used.ValueKind != JsonValueKind.Number)
    {
      return null;
    }

    DateTimeOffset? resets = null;
    if (win.TryGetProperty("resetsAt", out var r) && TryDate(r, out var parsed))
    {
      resets = parsed;
    }

    return new UsageWindow(used.GetDouble(), resets);
  }

  private static bool TryDate(JsonElement e, out DateTimeOffset value)
  {
    value = default;

    return e.ValueKind == JsonValueKind.String
      && DateTimeOffset.TryParse(
        e.GetString(),
        System.Globalization.CultureInfo.InvariantCulture,
        System.Globalization.DateTimeStyles.AssumeUniversal,
        out value);
  }
}
