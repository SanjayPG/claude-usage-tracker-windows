namespace ClaudeUsageTray.Core;

public sealed record UsageWindow(double UsedPercent, DateTimeOffset? ResetsAt);

public sealed record UsageSnapshot(
  DateTimeOffset UpdatedAt,
  UsageWindow? FiveHour,
  UsageWindow? SevenDay);

public enum UsageLevel
{
  Low,
  Medium,
  High,
}
