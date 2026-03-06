namespace MWG.EyesOfApollo.Desktop.Services
{
    internal static class DiagnosticsLogger
    {
        private static readonly string LogPath = Path.Combine(FileSystem.AppDataDirectory, "logs", "startup.log");

        internal static void LogError(string context, Exception exception)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
                File.AppendAllText(LogPath, $"{DateTimeOffset.UtcNow:O} {context}{Environment.NewLine}{exception}{Environment.NewLine}");
            }
            catch
            {
                // ignored
            }
        }
    }
}
