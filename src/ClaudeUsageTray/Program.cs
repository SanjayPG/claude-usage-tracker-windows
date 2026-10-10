using ClaudeUsageTray;

if (args is ["--preview", var previewFolder])
{
  ApplicationConfiguration.Initialize();
  Preview.Render(previewFolder);

  return;
}

// Runs the Claude Code refresh once and reports the result in the exit code (0 = ok). For testing.
if (args is ["--refresh-once"])
{
  Environment.Exit(ClaudeRefresher.RunAsync().GetAwaiter().GetResult() ? 0 : 1);
}

// A second copy would just add a second tray icon, so it exits quietly.
using var mutex = new Mutex(true, @"Local\ClaudeUsageTray.SingleInstance", out var isFirst);
if (!isFirst)
{
  return;
}

ApplicationConfiguration.Initialize();
Application.Run(new TrayContext());
