using System;
using System.IO;
using System.Text.Json;

namespace ORTools.Models
{
    /// <summary>
    /// Persistent per-user JSON settings manager stored in %AppData%\GOATBIM.
    /// Keeps user preferences intact across Revit sessions and add-in updates.
    /// </summary>
    public static class GoatSettingsManager
    {
        private static readonly string SettingsRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GOATBIM");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static void Save<T>(string toolKey, T settings)
        {
            try
            {
                if (!Directory.Exists(SettingsRoot))
                {
                    Directory.CreateDirectory(SettingsRoot);
                }

                string filePath = Path.Combine(SettingsRoot, $"{SanitizeFileName(toolKey)}.json");
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                // Ignore IO errors silently so workflow is never blocked
            }
        }

        public static T Load<T>(string toolKey) where T : new()
        {
            try
            {
                string filePath = Path.Combine(SettingsRoot, $"{SanitizeFileName(toolKey)}.json");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    T loaded = JsonSerializer.Deserialize<T>(json, JsonOptions);
                    if (loaded != null)
                        return loaded;
                }
            }
            catch
            {
            }

            return new T();
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }
    }
}
