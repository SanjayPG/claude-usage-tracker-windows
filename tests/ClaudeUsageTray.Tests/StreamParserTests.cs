using ClaudeUsageTray.Core;
using Xunit;

namespace ClaudeUsageTray.Tests;

public class StreamParserTests
{
  private static readonly DateTimeOffset Now = new(2026, 10, 10, 22, 0, 0, TimeSpan.Zero);

  // The event line as Claude Code prints it (captured from a real run).
  private const string Event = """
    {"type":"rate_limit_event","rate_limit_info":{"status":"allowed","resetsAt":1791686400,"rateLimitType":"five_hour","overageStatus":"rejected","isUsingOverage":false,"unifiedWindows":{"five_hour":{"utilization":0.04,"resetsAt":1791686400},"seven_day":{"utilization":0.08,"resetsAt":1792130400}}}}
    """;

  private static string Stream(params string[] lines) => string.Join("\n", lines);

  [Fact]
  public void Reads_both_windows_from_a_real_stream()
  {
    var output = Stream(
      """{"type":"system","subtype":"init"}""",
      """{"type":"assistant","message":{}}""",
      Event,
      """{"type":"result","subtype":"success"}""");

    var s = StreamParser.TryParse(output, Now);

    Assert.NotNull(s);
    Assert.Equal(4, s!.FiveHour!.UsedPercent);
    Assert.Equal(8, s.SevenDay!.UsedPercent);
    Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1791686400), s.FiveHour.ResetsAt);
    Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1792130400), s.SevenDay.ResetsAt);
    Assert.Equal(Now, s.UpdatedAt);
  }

  [Fact]
  public void Utilization_is_a_fraction_so_one_means_a_hundred_percent()
  {
    var line = """{"type":"rate_limit_event","rate_limit_info":{"unifiedWindows":{"five_hour":{"utilization":1.0,"resetsAt":1791686400}}}}""";

    Assert.Equal(100, StreamParser.TryParse(line, Now)!.FiveHour!.UsedPercent);
  }

  [Fact]
  public void Rounds_to_one_decimal_and_clamps()
  {
    var line = """{"type":"rate_limit_event","rate_limit_info":{"unifiedWindows":{"five_hour":{"utilization":0.3456,"resetsAt":1},"seven_day":{"utilization":3.0,"resetsAt":1}}}}""";

    var s = StreamParser.TryParse(line, Now)!;

    Assert.Equal(34.6, s.FiveHour!.UsedPercent);
    Assert.Equal(100, s.SevenDay!.UsedPercent);
  }

  [Fact]
  public void Accepts_one_window_only()
  {
    var line = """{"type":"rate_limit_event","rate_limit_info":{"unifiedWindows":{"seven_day":{"utilization":0.5,"resetsAt":1792130400}}}}""";

    var s = StreamParser.TryParse(line, Now)!;

    Assert.Null(s.FiveHour);
    Assert.Equal(50, s.SevenDay!.UsedPercent);
  }

  [Fact]
  public void The_last_event_in_the_stream_wins()
  {
    var early = Event.Replace("0.04", "0.10");

    Assert.Equal(4, StreamParser.TryParse(Stream(early, Event), Now)!.FiveHour!.UsedPercent);
  }

  [Fact]
  public void An_absurd_reset_time_is_ignored_not_fatal()
  {
    var line = """{"type":"rate_limit_event","rate_limit_info":{"unifiedWindows":{"five_hour":{"utilization":0.2,"resetsAt":99999999999999}}}}""";

    var s = StreamParser.TryParse(line, Now)!;

    Assert.Equal(20, s.FiveHour!.UsedPercent);
    Assert.Null(s.FiveHour.ResetsAt);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("not json at all")]
  [InlineData("""{"type":"result","subtype":"success"}""")]
  [InlineData("""{"type":"rate_limit_event"}""")]
  [InlineData("""{"type":"rate_limit_event","rate_limit_info":{}}""")]
  [InlineData("""{"type":"rate_limit_event","rate_limit_info":{"unifiedWindows":{}}}""")]
  [InlineData("""{"type":"rate_limit_event","rate_limit_info":{"unifiedWindows":{"five_hour":{"utilization":"x"}}}}""")]
  [InlineData("""{"type":"rate_limit_event","rate_limit_info":{"status":"allowed"}}""")]
  public void Returns_null_when_there_is_no_usable_reading(string? output)
  {
    Assert.Null(StreamParser.TryParse(output, Now));
  }

  [Fact]
  public void Skips_junk_lines_around_a_good_one()
  {
    var s = StreamParser.TryParse(Stream("warning: something", "{ broken", Event, ""), Now);

    Assert.NotNull(s);
  }

  [Fact]
  public void Written_json_round_trips_through_the_feed_parser()
  {
    var original = StreamParser.TryParse(Event, Now)!;

    var read = FeedParser.TryParse(Snapshots.ToJson(original))!;

    Assert.Equal(original.FiveHour, read.FiveHour);
    Assert.Equal(original.SevenDay, read.SevenDay);
    Assert.Equal(original.UpdatedAt, read.UpdatedAt);
  }

  [Fact]
  public void Newest_prefers_the_later_reading_and_handles_missing_ones()
  {
    var older = new UsageSnapshot(Now.AddMinutes(-5), new UsageWindow(1, null), null);
    var newer = new UsageSnapshot(Now, new UsageWindow(2, null), null);

    Assert.Same(newer, Snapshots.Newest(older, newer));
    Assert.Same(newer, Snapshots.Newest(newer, older));
    Assert.Same(older, Snapshots.Newest(older, null));
    Assert.Same(older, Snapshots.Newest(null, older));
    Assert.Null(Snapshots.Newest(null, null));
  }

  [Fact]
  public void Reader_skips_a_missing_file_without_waiting()
  {
    var started = DateTime.UtcNow;

    Assert.Null(FeedReader.Read(Path.Combine(Path.GetTempPath(), $"nope-{Guid.NewGuid():N}.json")));
    Assert.True(DateTime.UtcNow - started < TimeSpan.FromMilliseconds(100));
  }
}
