using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace WinFindGrep.Services
{
    public class HistoryService
    {
        private readonly string _historyFilePath;
        private const int MaxHistoryItems = 20;

        public HistoryService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "WinFindGrep");
            Directory.CreateDirectory(appFolder);
            _historyFilePath = Path.Combine(appFolder, "search_history.json");
        }

        public List<string> LoadHistory()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
            }
            catch
            {
                // Ignore errors loading history
            }

            return new List<string>();
        }

        public void AddToHistory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) return;

            var history = LoadHistory();
            
            // Remove if exists to move it to the top
            history.RemoveAll(h => h.Equals(directory, StringComparison.OrdinalIgnoreCase));
            
            // Add to top
            history.Insert(0, directory);

            // Trim
            if (history.Count > MaxHistoryItems)
            {
                history = history.Take(MaxHistoryItems).ToList();
            }

            SaveHistory(history);
        }

        private void SaveHistory(List<string> history)
        {
            try
            {
                var json = JsonSerializer.Serialize(history);
                File.WriteAllText(_historyFilePath, json);
            }
            catch
            {
                // Ignore errors saving history
            }
        }
    }
}
