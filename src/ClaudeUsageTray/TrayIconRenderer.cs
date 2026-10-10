using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ClaudeUsageTray.Core;

namespace ClaudeUsageTray;

// Two stacked bars: session on top, week below. Windows scales the 32px icon down to the tray size.
internal static class TrayIconRenderer
{
  private const int Size = 32;
  private const int BarX = 2;
  private const int BarWidth = 28;
  private const int BarHeight = 12;

  private static readonly Color Track = ColorTranslator.FromHtml("#4b5563");

  public static Icon Render(UsageSnapshot? snapshot, DateTimeOffset now)
  {
    using var bitmap = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
    using (var g = Graphics.FromImage(bitmap))
    {
      g.SmoothingMode = SmoothingMode.AntiAlias;
      g.Clear(Color.Transparent);
      DrawBar(g, 2, snapshot?.FiveHour, now);
      DrawBar(g, 18, snapshot?.SevenDay, now);
    }

    var handle = bitmap.GetHicon();
    try
    {
      using var temp = Icon.FromHandle(handle);

      // Clone copies the icon, so the native handle can be freed straight away.
      return (Icon)temp.Clone();
    }
    finally
    {
      DestroyIcon(handle);
    }
  }

  private static void DrawBar(Graphics g, int y, UsageWindow? window, DateTimeOffset now)
  {
    var track = new Rectangle(BarX, y, BarWidth, BarHeight);
    using (var trackPath = Rounded(track, 5))
    using (var trackBrush = new SolidBrush(Track))
    {
      g.FillPath(trackBrush, trackPath);
    }

    // No window, or one that has already reset: leave the empty track rather than a stale fill.
    if (window is null || Format.HasReset(window, now))
    {
      return;
    }

    var used = Format.Clamp(window.UsedPercent);
    // A small stub keeps a low figure visible without overstating it (a full-height cap reads as ~40%).
    var width = used <= 0 ? 0 : Math.Max(3, (int)Math.Round(BarWidth * used / 100));
    if (width == 0)
    {
      return;
    }

    using var fillPath = Rounded(new Rectangle(BarX, y, width, BarHeight), Math.Min(5, width / 2));
    using var fillBrush = new SolidBrush(ColorTranslator.FromHtml(Format.ColorHex(used)));
    g.FillPath(fillBrush, fillPath);
  }

  private static GraphicsPath Rounded(Rectangle r, int radius)
  {
    var d = radius * 2;
    var path = new GraphicsPath();
    path.AddArc(r.X, r.Y, d, d, 180, 90);
    path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
    path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
    path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
    path.CloseFigure();

    return path;
  }

  [DllImport("user32.dll", SetLastError = true)]
  private static extern bool DestroyIcon(IntPtr handle);
}
