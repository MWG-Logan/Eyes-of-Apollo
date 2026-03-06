using System.Text.Json;
using MWG.EyesOfApollo.Desktop.Models;

namespace MWG.EyesOfApollo.Desktop.Services
{
    /// <summary>
    /// Loads and persists visualizer themes from JSON files.
    /// </summary>
    public class ThemeService
    {
        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        /// <summary>
        /// Gets the directory where themes are stored.
        /// </summary>
        private static string ThemesDirectory => Path.Combine(AppContext.BaseDirectory, "Themes");

        /// <summary>
        /// Loads themes from disk, creating a default theme if none exist.
        /// </summary>
        /// <returns>The loaded themes.</returns>
        public async Task<IReadOnlyList<ThemeDefinition>> LoadThemesAsync()
        {
            var themes = new List<ThemeDefinition>();
            string[] files;

            try
            {
                Directory.CreateDirectory(ThemeService.ThemesDirectory);
                files = Directory.GetFiles(ThemeService.ThemesDirectory, "*.json", SearchOption.TopDirectoryOnly);
            }
            catch (Exception ex)
            {
                DiagnosticsLogger.LogError("Failed to enumerate theme files.", ex);
                files = [];
            }

            if (files.Length == 0)
            {
                themes.AddRange(await LoadBundledThemesAsync());
                if (themes.Count == 0)
                {
                    themes.Add(CreateDefaultTheme());
                }

                return themes;
            }

            foreach (var file in files)
            {
                try
                {
                    await using var stream = File.OpenRead(file);
                    var theme = await JsonSerializer.DeserializeAsync<ThemeDefinition>(stream, _serializerOptions);
                    if (theme != null)
                    {
                        if (string.IsNullOrWhiteSpace(theme.Name))
                        {
                            theme.Name = Path.GetFileNameWithoutExtension(file);
                        }

                        themes.Add(theme);
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticsLogger.LogError($"Failed to load theme file '{file}'.", ex);
                }
            }

            if (themes.Count == 0)
            {
                themes.Add(CreateDefaultTheme());
            }

            return themes;
        }

        /// <summary>
        /// Saves a theme to disk.
        /// </summary>
        /// <param name="filePath">The file path to write.</param>
        /// <param name="theme">The theme to serialize.</param>
        public async Task SaveThemeAsync(string filePath, ThemeDefinition theme)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, theme, _serializerOptions);
        }

        private static ThemeDefinition CreateDefaultTheme()
        {
            return new ThemeDefinition
            {
                Name = "Aurora",
                Background = "#0B0D17",
                Primary = "#6C63FF",
                Secondary = "#F5A623",
                Mode = VisualizerMode.Bars,
                BarCount = 64,
                LineThickness = 2f,
                BarSpacing = 2f
            };
        }

        private async Task<IReadOnlyList<ThemeDefinition>> LoadBundledThemesAsync()
        {
            var themes = new List<ThemeDefinition>();
            var bundledFiles = new[] { "aurora.json", "ember.json", "neon.json" };

            foreach (var bundledFile in bundledFiles)
            {
                try
                {
                    await using var stream = await FileSystem.OpenAppPackageFileAsync(Path.Combine("Themes", bundledFile));
                    var theme = await JsonSerializer.DeserializeAsync<ThemeDefinition>(stream, _serializerOptions);
                    if (theme != null)
                    {
                        if (string.IsNullOrWhiteSpace(theme.Name))
                        {
                            theme.Name = Path.GetFileNameWithoutExtension(bundledFile);
                        }

                        themes.Add(theme);
                    }

                    try
                    {
                        var destination = Path.Combine(ThemeService.ThemesDirectory, bundledFile);
                        if (!File.Exists(destination))
                        {
                            await SaveThemeAsync(destination, theme ?? CreateDefaultTheme());
                        }
                    }
                    catch (Exception ex)
                    {
                        DiagnosticsLogger.LogError($"Failed to persist bundled theme '{bundledFile}'.", ex);
                    }
                }
                catch (Exception ex)
                {
                    DiagnosticsLogger.LogError($"Failed to load bundled theme '{bundledFile}'.", ex);
                }
            }

            return themes;
        }
    }
}
