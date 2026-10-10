using ClaudeUsageTray.Core;
using Xunit;

namespace ClaudeUsageTray.Tests;

public class AlertTrackerTests
{
  private static readonly DateTimeOffset Now = new(2026, 10, 10, 18, 0, 0, TimeSpan.Zero);
  private static readonly DateTimeOffset Reset = Now.AddHours(2);

  private static UsageSnapshot Session(double used, DateTimeOffset? resets = null) =>
    new(Now, new UsageWindow(used, resets ?? Reset), null);

  [Fact]
  public void First_reading_never_alerts()
  {
    var tracker = new AlertTracker();

    Assert.Empty(tracker.Evaluate(Session(95), Now));
  }

  [Fact]
  public void Crossing_80_then_90_alerts_once_each()
  {
    var tracker = new AlertTracker();
    tracker.Evaluate(Session(10), Now);

    Assert.Empty(tracker.Evaluate(Session(79), Now));
    Assert.Equal(80, Assert.Single(tracker.Evaluate(Session(81), Now)).Threshold);
    Assert.Empty(tracker.Evaluate(Session(85), Now));
    Assert.Equal(90, Assert.Single(tracker.Evaluate(Session(91), Now)).Threshold);
    Assert.Empty(tracker.Evaluate(Session(99), Now));
  }

  [Fact]
  public void A_jump_past_both_thresholds_alerts_for_each()
  {
    var tracker = new AlertTracker();
    tracker.Evaluate(Session(10), Now);

    Assert.Equal(2, tracker.Evaluate(Session(95), Now).Count);
  }

  [Fact]
  public void A_new_reset_period_alerts_again()
  {
    var tracker = new AlertTracker();
    tracker.Evaluate(Session(10), Now);
    tracker.Evaluate(Session(85), Now);

    var nextPeriod = Session(85, Reset.AddHours(5));

    Assert.Single(tracker.Evaluate(nextPeriod, Now));
  }

  [Fact]
  public void A_window_that_has_reset_never_alerts()
  {
    var tracker = new AlertTracker();
    tracker.Evaluate(Session(10), Now);

    Assert.Empty(tracker.Evaluate(Session(95, Now.AddMinutes(-1)), Now));
  }
}
