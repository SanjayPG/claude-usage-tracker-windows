using ClaudeUsageTray.Core;
using Xunit;

namespace ClaudeUsageTray.Tests;

public class FeedParserTests
{
  private const string Full = """
    {"updatedAt":"2026-10-10T20:59:15.263Z",
     "five_hour":{"usedPercent":34,"resetsAt":"2026-10-10T21:40:00.000Z"},
     "seven_day":{"usedPercent":5,"resetsAt":"2026-10-16T06:00:00.000Z"}}
    """;

  [Fact]
  public void Parses_both_windows()
  {
    var s = FeedParser.TryParse(Full);

    Assert.NotNull(s);
    Assert.Equal(34, s!.FiveHour!.UsedPercent);
    Assert.Equal(5, s.SevenDay!.UsedPercent);
    Assert.Equal(new DateTimeOffset(2026, 10, 10, 21, 40, 0, TimeSpan.Zero), s.FiveHour.ResetsAt);
  }

  [Fact]
  public void Accepts_one_window_missing()
  {
    var s = FeedParser.TryParse("""{"updatedAt":"2026-10-10T20:59:15Z","five_hour":{"usedPercent":10,"resetsAt":null}}""");

    Assert.NotNull(s);
    Assert.Null(s!.SevenDay);
    Assert.Null(s.FiveHour!.ResetsAt);
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  [InlineData("{")]
  [InlineData("[]")]
  [InlineData("""{"updatedAt":"2026-10-10T20:59:15Z"}""")]
  [InlineData("""{"five_hour":{"usedPercent":10}}""")]
  [InlineData("""{"updatedAt":"not a date","five_hour":{"usedPercent":10}}""")]
  [InlineData("""{"updatedAt":"2026-10-10T20:59:15Z","five_hour":{"usedPercent":"ten"}}""")]
  public void Rejects_unusable_input(string? json)
  {
    Assert.Null(FeedParser.TryParse(json));
  }

  [Fact]
  public void Reader_returns_null_for_missing_file()
  {
    var path = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json");

    Assert.Null(FeedReader.Read(path));
  }

  [Fact]
  public void Reader_reads_a_file_another_process_still_has_open()
  {
    var path = Path.Combine(Path.GetTempPath(), $"feed-{Guid.NewGuid():N}.json");
    try
    {
      using var writer = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
      var bytes = System.Text.Encoding.UTF8.GetBytes(Full);
      writer.Write(bytes, 0, bytes.Length);
      writer.Flush();

      Assert.NotNull(FeedReader.Read(path));
    }
    finally
    {
      File.Delete(path);
    }
  }
}
