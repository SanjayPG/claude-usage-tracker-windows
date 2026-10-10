namespace ClaudeUsageTray.Core;

public static class RefreshPolicy
{
  // If Claude Code wrote the feed recently it is already running and keeping it fresh,
  // so a timer-driven refresh would only spend quota for nothing.
  public static readonly TimeSpan RecentlyUpdated = TimeSpan.FromMinutes(10);

  public static bool ShouldAutoRefresh(AppSettings settings, UsageSnapshot? snapshot, DateTimeOffset now, bool isRefreshRunning)
  {
    if (!settings.IsAutoRefreshOn || isRefreshRunning)
    {
      return false;
    }

    return snapshot is null || now - snapshot.UpdatedAt >= RecentlyUpdated;
  }

  // The timer ticks often; this decides whether a full interval has passed since the last attempt.
  public static bool IsDue(AppSettings settings, DateTimeOffset? lastAttempt, DateTimeOffset now) =>
    settings.IsAutoRefreshOn
    && (lastAttempt is null || now - lastAttempt.Value >= TimeSpan.FromMinutes(settings.AutoRefreshMinutes));
}
