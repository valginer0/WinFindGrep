using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WinFindGrep.Models;

namespace WinFindGrep.Services
{
    public class FileSearchService
    {
        public event Action<string> OnStatusUpdate;
        public event Action<int> OnProgressUpdate;
        public event Action<string> OnErrorOccurred;

        public async Task<List<SearchResult>> SearchFilesAsync(
            string[] directories,
            string[] filters,
            bool searchInSubFolders,
            string searchText,
            bool matchCase,
            bool matchWholeWord,
            bool useRegex,
            bool useExtendedSearch,
            Action<string, int, int> progressCallback)
        {
            var allFiles = await GetFilesToSearchAsync(directories, filters, searchInSubFolders);
            var matches = new List<SearchResult>();
            
            OnStatusUpdate?.Invoke("Searching files...");
            OnProgressUpdate?.Invoke(0); // Initial progress

            // Give back the UI thread immediately; the rest will execute on a thread-pool thread.
            await Task.Yield();

            if (allFiles == null || !allFiles.Any())
            {
                OnStatusUpdate?.Invoke("No files found to search.");
                OnProgressUpdate?.Invoke(100); // No files, so 100% complete
                return matches;
            }

            var filesToSearch = allFiles.Distinct().ToList();
            int totalFiles = filesToSearch.Count;
            int filesProcessed = 0;

            foreach (var file in filesToSearch)
            {
                // Invoke the progressCallback with current file details once per file
                progressCallback?.Invoke(Path.GetFileName(file), filesProcessed + 1, totalFiles);

                try
                {
                    var fileResults = SearchInFile(file, searchText, matchCase, matchWholeWord, useRegex, useExtendedSearch);
                    if (fileResults.Any())
                    {
                        matches.AddRange(fileResults);
                    }
                }
                catch (Exception ex)
                {
                    OnErrorOccurred?.Invoke($"Error searching file {file}: {ex.Message}");
                }

                filesProcessed++;
                int currentProgress = (int)((double)filesProcessed / totalFiles * 100);
                OnProgressUpdate?.Invoke(currentProgress);
            }

            OnStatusUpdate?.Invoke($"Search complete. Found {matches.Count} matches in {filesProcessed} files.");
            OnProgressUpdate?.Invoke(100);
            return matches;
        }

        internal async Task<List<string>> GetFilesToSearchAsync(string[] directories, string[] filters, bool searchInSubFolders)
        {
            var allFiles = new List<string>();
            if (filters.Length == 0)
            {
                filters = new[] { "*.*" };
            }

            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                try
                {
                    foreach (var filter in filters)
                    {
                        var filesInCurrentDir = await Task.Run(() => Directory.GetFiles(directory, filter, SearchOption.TopDirectoryOnly));
                        allFiles.AddRange(filesInCurrentDir);
                    }

                    if (searchInSubFolders)
                    {
                        var subDirectories = await Task.Run(() => Directory.GetDirectories(directory));
                        foreach (var subDir in subDirectories)
                        {
                            allFiles.AddRange(await GetFilesRecursivelyAsync(subDir, filters));
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip top-level directory if we can't access it
                }
                catch (Exception)
                {
                    // Handle other potential exceptions
                }
            }

            return allFiles.Distinct().ToList();
        }

        private async Task<List<string>> GetFilesRecursivelyAsync(string directory, string[] filters)
        {
            var files = new List<string>();

            try
            {
                foreach (var filter in filters)
                {
                    var filesInCurrentDir = await Task.Run(() => Directory.GetFiles(directory, filter, SearchOption.TopDirectoryOnly));
                    files.AddRange(filesInCurrentDir);
                }

                var subDirectories = await Task.Run(() => Directory.GetDirectories(directory));
                foreach (var subDir in subDirectories)
                {
                    files.AddRange(await GetFilesRecursivelyAsync(subDir, filters));
                }
            }
            catch (UnauthorizedAccessException)
            {
                // This is expected for system folders, so we just skip them and continue.
            }
            catch (Exception)
            {
                // Log or handle other unexpected errors if necessary, but continue.
            }

            return files;
        }

        internal List<SearchResult> SearchInFile(
            string filePath, 
            string searchText, 
            bool matchCase, 
            bool matchWholeWord, 
            bool useRegex, 
            bool useExtendedSearch)
        {
            var results = new List<SearchResult>();

            try
            {
                // Prepare helpers once per file
                string searchTextProcessed = useExtendedSearch ? ProcessExtendedSearch(searchText) : searchText;

                Regex? regex = null;
                if (useRegex)
                {
                    try
                    {
                        var regexOptions = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                        regex = new Regex(searchText, regexOptions | RegexOptions.Compiled);
                    }
                    catch
                    {
                        // Invalid regex – fall back to normal search
                        useRegex = false;
                    }
                }

                using var reader = new StreamReader(filePath, Encoding.UTF8);
                string? line;
                int lineNumber = 0;

                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    bool found;

                    if (useRegex)
                    {
                        found = regex!.IsMatch(line);
                    }
                    else
                    {
                        found = PerformNormalSearch(line, searchTextProcessed, matchCase, matchWholeWord);
                    }

                    if (found)
                    {
                        results.Add(new SearchResult
                        {
                            FilePath = filePath,
                            LineNumber = lineNumber,
                            LineContent = line,
                            LastModified = File.GetLastWriteTime(filePath)
                        });
                    }
                }
            }
            catch (Exception)
            {
                // File might be binary or inaccessible, skip it
            }

            return results;
        }

        internal bool PerformNormalSearch(string line, string searchText, bool matchCase, bool matchWholeWord)
        {
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (matchWholeWord)
            {
                var escapedSearchText = Regex.Escape(searchText);
                // This pattern uses negative lookarounds to ensure the search text is not preceded or
                // followed by a word character (\w), making it a "whole word". This is more robust
                // than \b in some edge cases involving punctuation.
                var pattern = $@"(?<!\w){escapedSearchText}(?!\w)";
                var options = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                return Regex.IsMatch(line, pattern, options);
            }

            return line.IndexOf(searchText, comparison) >= 0;
        }

        internal string ProcessExtendedSearch(string searchText)
        {
            return searchText
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\0", "\0");
        }
        
        // Placeholder for replace functionality that can be implemented later
        public async Task<int> ReplaceInFilesAsync(
            string searchText, 
            string replaceText, 
            List<SearchResult> matchesToReplace, 
            bool matchCase, 
            bool matchWholeWord, 
            bool useRegex, 
            bool useExtendedSearch,
            Action<string, int, int> progressCallback)
        {
            int replacedCount = 0;
            
            // Group by file path to process each file once
            var fileGroups = matchesToReplace.GroupBy(m => m.FilePath).ToList();
            
            progressCallback?.Invoke("Preparing replacement...", 0, fileGroups.Count);
            
            await Task.Run(() =>
            {
                for (int i = 0; i < fileGroups.Count; i++)
                {
                    var filePath = fileGroups[i].Key;
                    progressCallback?.Invoke(Path.GetFileName(filePath), i + 1, fileGroups.Count);
                    
                    try
                    {
                        string content = File.ReadAllText(filePath);
                        
                        if (useRegex)
                        {
                            try
                            {
                                var regexOptions = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                                var regex = new Regex(searchText, regexOptions);
                                content = regex.Replace(content, replaceText);
                                replacedCount += regex.Matches(content).Count;
                            }
                            catch
                            {
                                // Invalid regex, skip this file
                            }
                        }
                        else
                        {
                            string textToFind = useExtendedSearch ? ProcessExtendedSearch(searchText) : searchText;
                            string textToReplace = useExtendedSearch ? ProcessExtendedSearch(replaceText) : replaceText;
                            
                            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                            
                            // Simple string replacement for now
                            // In a real implementation, this would need more work to handle whole word matching
                            content = content.Replace(textToFind, textToReplace, comparison);
                            replacedCount++; // This is not accurate, would need to count actual replacements
                        }
                        
                        File.WriteAllText(filePath, content);
                    }
                    catch (Exception)
                    {
                        // Skip if file can't be read or written
                    }
                }
            });
            
            return replacedCount;
        }
    }
}
