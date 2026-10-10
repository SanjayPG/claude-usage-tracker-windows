using ClaudeUsageTray;

if (args is ["--preview", var previewFolder])
{
  ApplicationConfiguration.Initialize();
  Preview.Render(previewFolder);

  return;
}

// A second copy would just add a second tray icon, so it exits quietly.
using var mutex = new Mutex(true, @"Local\ClaudeUsageTray.SingleInstance", out var isFirst);
if (!isFirst)
{
  return;
}

ApplicationConfiguration.Initialize();
Application.Run(new TrayContext());
