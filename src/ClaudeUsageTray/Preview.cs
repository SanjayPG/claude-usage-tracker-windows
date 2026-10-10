using ClaudeUsageTray.Core;

namespace ClaudeUsageTray;

// `ClaudeUsageTray.exe --preview <folder>` writes PNGs of the popup and icon from sample data,
// so the look can be checked without waiting for a live feed.
internal static class Preview
{
  public static void Render(string folder)
  {
    Directory.CreateDirectory(folder);
    var now = DateTimeOffset.Now;

    var samples = new Dictionary<string, UsageSnapshot?>
    {
      ["low"] = Sample(now, 26, 4),
      ["high"] = Sample(now, 92, 63),
      ["reset"] = new UsageSnapshot(now.AddMinutes(-7), new UsageWindow(90, now.AddMinutes(-1)), new UsageWindow(40, now.AddDays(3))),
      ["none"] = null,
    };

    foreach (var (name, snapshot) in samples)
    {
      using var popup = new PopupForm();
      popup.Update(snapshot, now);
      _ = popup.Handle;
      using var bitmap = new Bitmap(popup.Width, popup.Height);
      popup.DrawToBitmap(bitmap, new Rectangle(Point.Empty, popup.Size));
      bitmap.Save(Path.Combine(folder, $"popup-{name}.png"));

      using var icon = TrayIconRenderer.Render(snapshot, now);
      using var iconBitmap = icon.ToBitmap();
      iconBitmap.Save(Path.Combine(folder, $"icon-{name}.png"));
    }
  }

  private static UsageSnapshot Sample(DateTimeOffset now, double five, double seven) =>
    new(now.AddMinutes(-4), new UsageWindow(five, now.AddHours(1).AddMinutes(30)), new UsageWindow(seven, now.AddDays(5).AddHours(12)));
}
