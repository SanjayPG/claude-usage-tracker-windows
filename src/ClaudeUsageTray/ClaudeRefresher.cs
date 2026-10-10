using System.Diagnostics;

namespace ClaudeUsageTray;

// Runs one tiny request through the official Claude Code CLI. Claude Code reports your usage with
// every reply, and the usage-tracker mod writes it to the feed, so no login token is ever touched here.
internal static class ClaudeRefresher
{
  private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(120);

  public static string? FindClaude()
  {
    var dirs = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
      .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
      .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin"));

    foreach (var dir in dirs)
    {
      try
      {
        var candidate = Path.Combine(dir.Trim('"'), "claude.exe");
        if (File.Exists(candidate))
        {
          return candidate;
        }
      }
      catch (ArgumentException)
      {
        // A malformed PATH entry; skip it.
      }
    }

    return null;
  }

  public static async Task<bool> RunAsync()
  {
    var exe = FindClaude();
    if (exe is null)
    {
      return false;
    }

    var info = new ProcessStartInfo(exe)
    {
      UseShellExecute = false,
      CreateNoWindow = true,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      // Home, not a project folder, so no project hooks or settings get involved.
      WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    };
    info.ArgumentList.Add("-p");
    info.ArgumentList.Add("Reply with just: ok");

    using var process = Process.Start(info);
    if (process is null)
    {
      return false;
    }

    process.StandardInput.Close();

    // Drain both pipes so a full buffer can never stall the child.
    _ = process.StandardOutput.ReadToEndAsync();
    _ = process.StandardError.ReadToEndAsync();

    using var timeout = new CancellationTokenSource(Timeout);
    try
    {
      await process.WaitForExitAsync(timeout.Token);
    }
    catch (OperationCanceledException)
    {
      try
      {
        process.Kill(entireProcessTree: true);
      }
      catch (InvalidOperationException)
      {
        // It exited between the timeout and the kill.
      }

      return false;
    }

    return process.ExitCode == 0;
  }
}
