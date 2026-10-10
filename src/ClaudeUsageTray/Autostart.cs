using Microsoft.Win32;

namespace ClaudeUsageTray;

internal static class Autostart
{
  private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
  private const string ValueName = "ClaudeUsageTray";

  public static bool IsEnabled()
  {
    using var key = Registry.CurrentUser.OpenSubKey(RunKey);

    return key?.GetValue(ValueName) is string;
  }

  public static void Set(bool isEnabled)
  {
    using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
    if (key is null)
    {
      return;
    }

    if (isEnabled && Environment.ProcessPath is { } path)
    {
      key.SetValue(ValueName, $"\"{path}\"");
    }
    else
    {
      key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
  }
}
