using System.Globalization;

namespace ClaudeUsageTray.Core;

public static class Format
{
  public const int TooltipMaxLength = 127;

  // Same thresholds and colours as the Claude Code mod, so both views agree.
  public static UsageLevel LevelFor(double used) =>
    used < 50 ? UsageLevel.Low : used < 80 ? UsageLevel.Medium : UsageLevel.High;

  public static string ColorHex(double used) => LevelFor(used) switch
  {
    UsageLevel.Low => "#4ade80",
    UsageLevel.Medium => "#fbbf24",
    _ => "#f87171",
  };

  public static double Clamp(double used) => Math.Min(100, Math.Max(0, used));

  // A window has reset once its reset time has passed; the old percentage no longer applies.
  public static bool HasReset(UsageWindow window, DateTimeOffset now) =>
    window.ResetsAt is { } at && at <= now;

  // "Resets 10:40 PM" today, "Resets Fri 7:00 AM" on another day, in the given zone.
  public static string ResetText(DateTimeOffset? resetsAt, DateTimeOffset now, TimeZoneInfo zone)
  {
    if (resetsAt is not { } at)
    {
      return string.Empty;
    }

    var local = TimeZoneInfo.ConvertTime(at, zone);
    var today = TimeZoneInfo.ConvertTime(now, zone).Date;
    var time = local.ToString("h:mm tt", CultureInfo.InvariantCulture);

    return local.Date == today
      ? $"Resets {time}"
      : $"Resets {local.ToString("ddd", CultureInfo.InvariantCulture)} {time}";
  }

  public static string AgeText(DateTimeOffset updatedAt, DateTimeOffset now)
  {
    var age = now - updatedAt;
    if (age < TimeSpan.Zero || age < TimeSpan.FromMinutes(1))
    {
      return "updated just now";
    }

    if (age < TimeSpan.FromHours(1))
    {
      return $"updated {(int)age.TotalMinutes} min ago";
    }

    return age < TimeSpan.FromDays(1)
      ? $"updated {(int)age.TotalHours} h ago"
      : $"updated {(int)age.TotalDays} d ago";
  }

  public static string Tooltip(UsageSnapshot? snapshot, DateTimeOffset now, TimeZoneInfo zone)
  {
    if (snapshot is null)
    {
      return Truncate("Claude usage: no data yet. Use Claude Code to start the feed.");
    }

    var lines = new List<string>();
    AddLine(lines, "Session", snapshot.FiveHour, now, zone);
    AddLine(lines, "Week", snapshot.SevenDay, now, zone);

    return Truncate(string.Join("\n", lines));
  }

  public static string Truncate(string text) =>
    text.Length <= TooltipMaxLength ? text : text[..(TooltipMaxLength - 1)] + "…";

  private static void AddLine(List<string> lines, string label, UsageWindow? window, DateTimeOffset now, TimeZoneInfo zone)
  {
    if (window is null)
    {
      return;
    }

    if (HasReset(window, now))
    {
      lines.Add($"{label}: reset, waiting for Claude Code");

      return;
    }

    var reset = ResetText(window.ResetsAt, now, zone);
    var used = $"{Clamp(window.UsedPercent):0}% used";
    lines.Add(reset.Length > 0 ? $"{label}: {used} - {reset}" : $"{label}: {used}");
  }
}
