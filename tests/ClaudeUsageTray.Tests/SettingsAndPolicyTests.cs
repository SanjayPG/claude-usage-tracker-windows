using ClaudeUsageTray.Core;
using Xunit;

namespace ClaudeUsageTray.Tests;

public class SettingsAndPolicyTests
{
  private static readonly DateTimeOffset Now = new(2026, 10, 10, 18, 0, 0, TimeSpan.Zero);

  private static UsageSnapshot Snapshot(TimeSpan age) =>
    new(Now - age, new UsageWindow(10, Now.AddHours(1)), null);

  private static string TempPath() => Path.Combine(Path.GetTempPath(), $"settings-{Guid.NewGuid():N}", "settings.json");

  [Fact]
  public void Defaults_to_auto_refresh_off()
  {
    Assert.False(new AppSettings().IsAutoRefreshOn);
  }

  [Fact]
  public void Missing_file_loads_defaults()
  {
    Assert.Equal(new AppSettings(), AppSettings.Load(TempPath()));
  }

  [Theory]
  [InlineData("not json")]
  [InlineData("")]
  [InlineData("""{"AutoRefreshMinutes": -5}""")]
  [InlineData("[]")]
  public void Bad_file_loads_defaults(string content)
  {
    var path = TempPath();
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, content);
    try
    {
      Assert.Equal(new AppSettings(), AppSettings.Load(path));
    }
    finally
    {
      Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }
  }

  [Fact]
  public void Saved_settings_round_trip()
  {
    var path = TempPath();
    try
    {
      new AppSettings(AppSettings.AutoRefreshChoice).Save(path);

      var loaded = AppSettings.Load(path);

      Assert.True(loaded.IsAutoRefreshOn);
      Assert.Equal(AppSettings.AutoRefreshChoice, loaded.AutoRefreshMinutes);
    }
    finally
    {
      Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }
  }

  [Fact]
  public void Auto_refresh_never_runs_when_switched_off()
  {
    Assert.False(RefreshPolicy.ShouldAutoRefresh(new AppSettings(), Snapshot(TimeSpan.FromHours(5)), Now, false));
  }

  [Fact]
  public void Auto_refresh_skips_a_recently_updated_feed()
  {
    var on = new AppSettings(30);

    Assert.False(RefreshPolicy.ShouldAutoRefresh(on, Snapshot(TimeSpan.FromMinutes(9)), Now, false));
    Assert.True(RefreshPolicy.ShouldAutoRefresh(on, Snapshot(TimeSpan.FromMinutes(10)), Now, false));
  }

  [Fact]
  public void Auto_refresh_runs_when_there_is_no_feed_yet()
  {
    Assert.True(RefreshPolicy.ShouldAutoRefresh(new AppSettings(30), null, Now, false));
  }

  [Fact]
  public void Auto_refresh_waits_for_a_running_refresh_to_finish()
  {
    Assert.False(RefreshPolicy.ShouldAutoRefresh(new AppSettings(30), null, Now, isRefreshRunning: true));
  }

  [Fact]
  public void Interval_is_due_after_the_configured_minutes()
  {
    var on = new AppSettings(30);

    Assert.True(RefreshPolicy.IsDue(on, null, Now));
    Assert.False(RefreshPolicy.IsDue(on, Now.AddMinutes(-29), Now));
    Assert.True(RefreshPolicy.IsDue(on, Now.AddMinutes(-30), Now));
    Assert.False(RefreshPolicy.IsDue(new AppSettings(), null, Now));
  }
}
