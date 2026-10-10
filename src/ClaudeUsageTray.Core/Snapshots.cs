using System.Text.Json;

namespace ClaudeUsageTray.Core;

public static class Snapshots
{
  // Two sources can both hold a reading (the plugin's feed and the app's own last refresh).
  // The more recently written one is the better guess at the truth.
  public static UsageSnapshot? Newest(UsageSnapshot? a, UsageSnapshot? b)
  {
    if (a is null)
    {
      return b;
    }

    return b is null || a.UpdatedAt >= b.UpdatedAt ? a : b;
  }

  // The same shape the Claude Code plugin writes, so one parser reads both files.
  public static string ToJson(UsageSnapshot snapshot)
  {
    var feed = new Dictionary<string, object?> { ["updatedAt"] = snapshot.UpdatedAt.UtcDateTime.ToString("O") };
    AddWindow(feed, "five_hour", snapshot.FiveHour);
    AddWindow(feed, "seven_day", snapshot.SevenDay);

    return JsonSerializer.Serialize(feed);
  }

  private static void AddWindow(Dictionary<string, object?> feed, string name, UsageWindow? window)
  {
    if (window is null)
    {
      return;
    }

    feed[name] = new Dictionary<string, object?>
    {
      ["usedPercent"] = window.UsedPercent,
      ["resetsAt"] = window.ResetsAt?.UtcDateTime.ToString("O"),
    };
  }
}
