using System.Drawing.Drawing2D;
using ClaudeUsageTray.Core;

namespace ClaudeUsageTray;

// A small dark card that mirrors the Claude Code mod: label, bar, percent, reset time.
internal sealed class PopupForm : Form
{
  private const float LogicalWidth = 520;
  private const float LogicalHeight = 120;

  private static readonly Color Background = ColorTranslator.FromHtml("#1b1b1b");
  private static readonly Color Border = ColorTranslator.FromHtml("#33383f");
  private static readonly Color TrackColor = ColorTranslator.FromHtml("#2d333b");
  private static readonly Color LabelColor = ColorTranslator.FromHtml("#60a5fa");
  private static readonly Color Muted = ColorTranslator.FromHtml("#8b949e");

  private readonly Font _label = new("Segoe UI Semibold", 10f);
  private readonly Font _value = new("Segoe UI Semibold", 10f);
  private readonly Font _small = new("Segoe UI", 9f);

  private UsageSnapshot? _snapshot;
  private DateTimeOffset _now = DateTimeOffset.Now;

  public PopupForm()
  {
    FormBorderStyle = FormBorderStyle.None;
    ShowInTaskbar = false;
    TopMost = true;
    StartPosition = FormStartPosition.Manual;
    BackColor = Background;
    DoubleBuffered = true;
    // Size is derived from DeviceDpi below, in step with the drawing, so WinForms must not scale it too.
    AutoScaleMode = AutoScaleMode.None;
    ClientSize = new Size((int)LogicalWidth, (int)LogicalHeight);
  }

  protected override void OnHandleCreated(EventArgs e)
  {
    base.OnHandleCreated(e);
    ApplyScale();
  }

  protected override void OnDpiChanged(DpiChangedEventArgs e)
  {
    base.OnDpiChanged(e);
    ApplyScale();
  }

  private void ApplyScale()
  {
    var s = DeviceDpi / 96f;
    // Borderless, so Size and ClientSize agree; setting Size avoids the client-area adjustment that grew the height.
    Size = new Size((int)(LogicalWidth * s), (int)(LogicalHeight * s));
  }

  public DateTime LastHiddenUtc { get; private set; } = DateTime.MinValue;

  public void Update(UsageSnapshot? snapshot, DateTimeOffset now)
  {
    _snapshot = snapshot;
    _now = now;
    if (Visible)
    {
      Invalidate();
    }
  }

  public void ShowNearTray()
  {
    // Force the handle so Width/Height are DPI-scaled before we position the card.
    _ = Handle;
    var area = Screen.FromPoint(Cursor.Position).WorkingArea;
    Location = new Point(area.Right - Width - 12, area.Bottom - Height - 12);
    Show();
    Activate();
  }

  public void Dismiss()
  {
    LastHiddenUtc = DateTime.UtcNow;
    Hide();
  }

  protected override void OnDeactivate(EventArgs e)
  {
    base.OnDeactivate(e);
    Dismiss();
  }

  protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
  {
    if (keyData == Keys.Escape)
    {
      Dismiss();

      return true;
    }

    return base.ProcessCmdKey(ref msg, keyData);
  }

  protected override void OnPaint(PaintEventArgs e)
  {
    base.OnPaint(e);
    var g = e.Graphics;
    g.SmoothingMode = SmoothingMode.AntiAlias;
    var s = DeviceDpi / 96f;

    using (var pen = new Pen(Border))
    {
      g.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
    }

    if (_snapshot is null)
    {
      DrawText(g, "No data yet. Use Claude Code once and the numbers appear here.", _small, Muted, 16 * s, 24 * s);

      return;
    }

    var y = 16f;
    DrawRow(g, s, ref y, "Current session", _snapshot.FiveHour);
    DrawRow(g, s, ref y, "This week", _snapshot.SevenDay);
    DrawText(g, Format.AgeText(_snapshot.UpdatedAt, _now), _small, Muted, 16 * s, (LogicalHeight - 30) * s);
  }

  private void DrawRow(Graphics g, float s, ref float y, string label, UsageWindow? window)
  {
    if (window is null)
    {
      return;
    }

    var top = y * s;
    var isReset = Format.HasReset(window, _now);
    var used = Format.Clamp(window.UsedPercent);
    var color = ColorTranslator.FromHtml(Format.ColorHex(used));

    DrawText(g, label, _label, LabelColor, 16 * s, top);

    // One fixed pixel column per part, so rows line up whatever the font does.
    var bar = new RectangleF(140 * s, top + 7 * s, 160 * s, 8 * s);
    FillRounded(g, bar, TrackColor);
    if (!isReset && used > 0)
    {
      var width = Math.Max(bar.Height, (float)(bar.Width * used / 100.0));
      FillRounded(g, new RectangleF(bar.X, bar.Y, width, bar.Height), color);
    }

    DrawText(g, isReset ? "reset" : $"{used:0}% used", _value, isReset ? Muted : color, 312 * s, top);
    DrawText(
      g,
      isReset ? "waiting for data" : Format.ResetText(window.ResetsAt, _now, TimeZoneInfo.Local),
      _small,
      Muted,
      388 * s,
      top + 1 * s);

    y += 36;
  }

  private static void DrawText(Graphics g, string text, Font font, Color color, float x, float y)
  {
    TextRenderer.DrawText(
      g,
      text,
      font,
      new Point((int)x, (int)y),
      color,
      TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
  }

  private static void FillRounded(Graphics g, RectangleF r, Color color)
  {
    var d = r.Height;
    using var path = new GraphicsPath();
    path.AddArc(r.X, r.Y, d, d, 180, 90);
    path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
    path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
    path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
    path.CloseFigure();
    using var brush = new SolidBrush(color);
    g.FillPath(brush, path);
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing)
    {
      _label.Dispose();
      _value.Dispose();
      _small.Dispose();
    }

    base.Dispose(disposing);
  }
}
