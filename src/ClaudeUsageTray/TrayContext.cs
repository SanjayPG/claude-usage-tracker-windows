using System.Diagnostics;
using ClaudeUsageTray.Core;

namespace ClaudeUsageTray;

internal sealed class TrayContext : ApplicationContext
{
  // The feed only changes while Claude Code runs, so a slow timer is enough beside the watcher.
  private const int PollMilliseconds = 30_000;
  private const int DebounceMilliseconds = 300;

  private readonly string _feedPath = FeedReader.DefaultPath();
  private readonly NotifyIcon _tray;
  private readonly PopupForm _popup = new();
  private readonly AlertTracker _alerts = new();
  private readonly System.Windows.Forms.Timer _poll = new() { Interval = PollMilliseconds };
  private readonly System.Windows.Forms.Timer _debounce = new() { Interval = DebounceMilliseconds };
  private readonly ToolStripMenuItem _autostart;
  private readonly Control _marshal = new();
  private FileSystemWatcher? _watcher;
  private Icon? _icon;
  private UsageSnapshot? _snapshot;

  public TrayContext()
  {
    // A handle lets the watcher thread hand work back to the UI thread.
    _ = _marshal.Handle;

    _autostart = new ToolStripMenuItem("Start with Windows") { Checked = Autostart.IsEnabled(), CheckOnClick = true };
    _autostart.CheckedChanged += (_, _) => Autostart.Set(_autostart.Checked);

    var menu = new ContextMenuStrip();
    menu.Items.Add("Refresh", null, (_, _) => Refresh());
    menu.Items.Add(_autostart);
    menu.Items.Add("Open feed folder", null, (_, _) => OpenFeedFolder());
    menu.Items.Add(new ToolStripSeparator());
    menu.Items.Add("Exit", null, (_, _) => ExitThread());

    _tray = new NotifyIcon { ContextMenuStrip = menu, Text = "Claude usage", Visible = true };
    _tray.MouseClick += (_, e) =>
    {
      if (e.Button == MouseButtons.Left)
      {
        TogglePopup();
      }
    };

    _poll.Tick += (_, _) => Refresh();
    _poll.Start();
    _debounce.Tick += (_, _) =>
    {
      _debounce.Stop();
      Refresh();
    };

    StartWatcher();
    Refresh();
  }

  private void StartWatcher()
  {
    var dir = Path.GetDirectoryName(_feedPath);
    if (dir is null || !Directory.Exists(dir))
    {
      return;
    }

    _watcher = new FileSystemWatcher(dir, Path.GetFileName(_feedPath))
    {
      NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
      EnableRaisingEvents = true,
    };

    FileSystemEventHandler onChange = (_, _) => SignalChange();
    _watcher.Changed += onChange;
    _watcher.Created += onChange;
    _watcher.Renamed += (_, _) => SignalChange();
  }

  // Watcher events arrive on a pool thread and often in bursts, so restart one short timer on the UI thread.
  private void SignalChange()
  {
    if (_marshal.IsHandleCreated && !_marshal.IsDisposed)
    {
      _marshal.BeginInvoke(() =>
      {
        _debounce.Stop();
        _debounce.Start();
      });
    }
  }

  private void Refresh()
  {
    _snapshot = FeedReader.Read(_feedPath);
    var now = DateTimeOffset.Now;

    var next = TrayIconRenderer.Render(_snapshot, now);
    var previous = _icon;
    _tray.Icon = next;
    _icon = next;
    previous?.Dispose();

    _tray.Text = Format.Tooltip(_snapshot, now, TimeZoneInfo.Local);

    if (_snapshot is not null)
    {
      foreach (var alert in _alerts.Evaluate(_snapshot, now))
      {
        _tray.ShowBalloonTip(
          5000,
          $"Claude {alert.Window.ToLowerInvariant()} usage at {alert.Threshold}%",
          $"{alert.UsedPercent:0}% used",
          ToolTipIcon.Warning);
      }
    }

    _popup.Update(_snapshot, now);
  }

  private void TogglePopup()
  {
    if (_popup.Visible)
    {
      _popup.Dismiss();

      return;
    }

    // Clicking the icon deactivates the popup first, which would otherwise reopen it straight away.
    if ((DateTime.UtcNow - _popup.LastHiddenUtc).TotalMilliseconds < 300)
    {
      return;
    }

    _popup.Update(_snapshot, DateTimeOffset.Now);
    _popup.ShowNearTray();
  }

  private void OpenFeedFolder()
  {
    var args = File.Exists(_feedPath) ? $"/select,\"{_feedPath}\"" : $"\"{Path.GetDirectoryName(_feedPath)}\"";
    Process.Start(new ProcessStartInfo("explorer.exe", args) { UseShellExecute = true });
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing)
    {
      _tray.Visible = false;
      _tray.Dispose();
      _icon?.Dispose();
      _watcher?.Dispose();
      _poll.Dispose();
      _debounce.Dispose();
      _popup.Dispose();
      _marshal.Dispose();
    }

    base.Dispose(disposing);
  }
}
