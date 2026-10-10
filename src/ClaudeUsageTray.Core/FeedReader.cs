namespace ClaudeUsageTray.Core;

public static class FeedReader
{
  public static string DefaultPath() =>
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".claude",
      "usage-feed.json");

  // The app's own copy of the last reading it fetched itself by running Claude Code.
  public static string RefreshPath() =>
    Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
      "ClaudeUsageTray",
      "last-refresh.json");

  // Claude Code may be mid-write, so one short retry covers a torn read.
  public static UsageSnapshot? Read(string path)
  {
    // A file that is simply absent is the normal case for a source you do not use; do not wait on it.
    if (!File.Exists(path))
    {
      return null;
    }

    for (var attempt = 0; attempt < 2; attempt++)
    {
      try
      {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        var snapshot = FeedParser.TryParse(reader.ReadToEnd());
        if (snapshot is not null)
        {
          return snapshot;
        }
      }
      catch (IOException)
      {
        // Missing or locked; fall through to the retry.
      }
      catch (UnauthorizedAccessException)
      {
        return null;
      }

      if (attempt == 0)
      {
        Thread.Sleep(120);
      }
    }

    return null;
  }
}
