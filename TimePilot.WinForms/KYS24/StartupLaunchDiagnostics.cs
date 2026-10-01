using System.Text.Json;

namespace TimePilot.WinForms.KYS24
{
    internal static class StartupLaunchDiagnostics
    {
        private const long MaxLogBytes = 512 * 1024;
        private static readonly object SyncRoot = new();

        public static void Record(string eventName, object? details = null)
        {
            try
            {
                lock (SyncRoot)
                {
                    Directory.CreateDirectory(AppDataPaths.DataDirectory);
                    var path = Path.Combine(AppDataPaths.DataDirectory, "startup-lifecycle.jsonl");
                    if (File.Exists(path) && new FileInfo(path).Length >= MaxLogBytes)
                        File.Delete(path);

                    using var process = System.Diagnostics.Process.GetCurrentProcess();
                    var entry = JsonSerializer.Serialize(new
                    {
                        Timestamp = DateTimeOffset.Now,
                        Event = eventName,
                        ProcessId = Environment.ProcessId,
                        SessionId = Environment.ProcessId > 0
                            ? process.SessionId
                            : (int?)null,
                        UptimeSeconds = Environment.TickCount64 / 1000,
                        Details = details
                    });
                    File.AppendAllText(path, entry + Environment.NewLine);
                }
            }
            catch
            {
                // Diagnostics must never interfere with application startup or shutdown.
            }
        }
    }
}
