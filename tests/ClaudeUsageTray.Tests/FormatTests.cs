using ClaudeUsageTray.Core;
using Xunit;

namespace ClaudeUsageTray.Tests;

public class FormatTests
{
  private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
  private static readonly DateTimeOffset Now = new(2026, 10, 10, 18, 0, 0, TimeSpan.Zero);

  [Theory]
  [InlineData(0, "#4ade80")]
  [InlineData(49.9, "#4ade80")]
  [InlineData(50, "#fbbf24")]
  [InlineData(79.9, "#fbbf24")]
  [InlineData(80, "#f87171")]
  [InlineData(100, "#f87171")]
  public void Colour_follows_thresholds(double used, string hex)
  {
    Assert.Equal(hex, Format.ColorHex(used));
  }

  [Theory]
  [InlineData(-5, 0)]
  [InlineData(42, 42)]
  [InlineData(130, 100)]
  public void Clamp_keeps_percent_in_range(double input, double expected)
  {
    Assert.Equal(expected, Format.Clamp(input));
  }

  [Fact]
  public void Reset_today_shows_time_only()
  {
    var at = new DateTimeOffset(2026, 10, 10, 22, 40, 0, TimeSpan.Zero);

    Assert.Equal("Resets 10:40 PM", Format.ResetText(at, Now, Utc));
  }

  [Fact]
  public void Reset_on_another_day_shows_weekday()
  {
    var at = new DateTimeOffset(2026, 10, 16, 7, 0, 0, TimeSpan.Zero);

    Assert.Equal("Resets Fri 7:00 AM", Format.ResetText(at, Now, Utc));
  }

  [Theory]
  [InlineData(0, 0, "Resets 12:00 AM")]
  [InlineData(0, 5, "Resets 12:05 AM")]
  [InlineData(12, 0, "Resets 12:00 PM")]
  [InlineData(13, 30, "Resets 1:30 PM")]
  public void Twelve_hour_clock_edges(int hour, int minute, string expected)
  {
    var now = new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);
    var at = new DateTimeOffset(2026, 10, 10, hour, minute, 0, TimeSpan.Zero);

    Assert.Equal(expected, Format.ResetText(at, now, Utc));
  }

  [Fact]
  public void Reset_uses_the_given_zone_for_today()
  {
    // 23:30 UTC on the 10th is already the 11th in UTC+2, so it is not "today" there.
    var zone = TimeZoneInfo.CreateCustomTimeZone("plus2", TimeSpan.FromHours(2), "plus2", "plus2");
    var now = new DateTimeOffset(2026, 10, 10, 10, 0, 0, TimeSpan.Zero);
    var at = new DateTimeOffset(2026, 10, 10, 23, 30, 0, TimeSpan.Zero);

    Assert.Equal("Resets Sun 1:30 AM", Format.ResetText(at, now, zone));
  }

  [Fact]
  public void Missing_reset_gives_empty_text()
  {
    Assert.Equal(string.Empty, Format.ResetText(null, Now, Utc));
  }

  [Fact]
  public void Window_has_reset_only_after_its_time()
  {
    Assert.True(Format.HasReset(new UsageWindow(50, Now.AddMinutes(-1)), Now));
    Assert.True(Format.HasReset(new UsageWindow(50, Now), Now));
    Assert.False(Format.HasReset(new UsageWindow(50, Now.AddMinutes(1)), Now));
    Assert.False(Format.HasReset(new UsageWindow(50, null), Now));
  }

  [Theory]
  [InlineData(10, "updated just now")]
  [InlineData(59, "updated just now")]
  [InlineData(60, "updated 1 min ago")]
  [InlineData(4 * 60, "updated 4 min ago")]
  [InlineData(3600, "updated 1 h ago")]
  [InlineData(5 * 3600, "updated 5 h ago")]
  [InlineData(2 * 86400, "updated 2 d ago")]
  public void Age_wording(int seconds, string expected)
  {
    Assert.Equal(expected, Format.AgeText(Now.AddSeconds(-seconds), Now));
  }

  [Fact]
  public void Age_in_the_future_reads_as_just_now()
  {
    Assert.Equal("updated just now", Format.AgeText(Now.AddMinutes(5), Now));
  }

  [Fact]
  public void Tooltip_lists_both_windows()
  {
    var snapshot = new UsageSnapshot(
      Now,
      new UsageWindow(26, new DateTimeOffset(2026, 10, 10, 22, 40, 0, TimeSpan.Zero)),
      new UsageWindow(4, new DateTimeOffset(2026, 10, 16, 7, 0, 0, TimeSpan.Zero)));

    Assert.Equal(
      "Session: 26% used - Resets 10:40 PM\nWeek: 4% used - Resets Fri 7:00 AM",
      Format.Tooltip(snapshot, Now, Utc));
  }

  [Fact]
  public void Tooltip_says_waiting_after_a_reset()
  {
    var snapshot = new UsageSnapshot(Now, new UsageWindow(90, Now.AddMinutes(-1)), null);

    Assert.Equal("Session: reset, waiting for Claude Code", Format.Tooltip(snapshot, Now, Utc));
  }

  [Fact]
  public void Tooltip_without_data_explains_how_to_start()
  {
    Assert.Contains("no data", Format.Tooltip(null, Now, Utc));
  }

  [Fact]
  public void Tooltip_never_exceeds_the_notify_icon_limit()
  {
    Assert.Equal(Format.TooltipMaxLength, Format.Truncate(new string('x', 500)).Length);
    Assert.True(Format.Tooltip(null, Now, Utc).Length <= Format.TooltipMaxLength);
  }
}
