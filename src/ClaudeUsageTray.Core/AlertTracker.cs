namespace ClaudeUsageTray.Core;

public sealed record UsageAlert(string Window, int Threshold, double UsedPercent);

// Fires each threshold once per window per reset. The first reading only seeds state, so
// starting the app at 85% does not pop an alert for something you already knew.
public sealed class AlertTracker
{
  private static readonly int[] Thresholds = [80, 90];

  private readonly HashSet<string> _seen = [];
  private bool _isSeeded;

  public IReadOnlyList<UsageAlert> Evaluate(UsageSnapshot snapshot, DateTimeOffset now)
  {
    var alerts = new List<UsageAlert>();
    Check(alerts, "Session", snapshot.FiveHour, now);
    Check(alerts, "Week", snapshot.SevenDay, now);
    _isSeeded = true;

    return alerts;
  }

  private void Check(List<UsageAlert> alerts, string name, UsageWindow? window, DateTimeOffset now)
  {
    if (window is null || Format.HasReset(window, now))
    {
      return;
    }

    var used = Format.Clamp(window.UsedPercent);
    foreach (var threshold in Thresholds)
    {
      if (used < threshold)
      {
        continue;
      }

      var key = $"{name}|{window.ResetsAt:O}|{threshold}";
      if (_seen.Add(key) && _isSeeded)
      {
        alerts.Add(new UsageAlert(name, threshold, used));
      }
    }
  }
}
