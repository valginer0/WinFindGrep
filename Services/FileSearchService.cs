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
        public async Task<List<SearchResult>> SearchFilesAsync(
            string searchText, 
            string[] directories, 
            string[] filters, 
            bool searchInSubFolders, 
            bool matchCase, 
            bool matchWholeWord, 
            bool useRegex, 
            bool useExtendedSearch,
            Action<string, int, int> progressCallback)
        {
            var allFiles = GetFilesToSearch(directories, filters, searchInSubFolders);
            var matches = new List<SearchResult>();
            
            progressCallback?.Invoke("Preparing search...", 0, allFiles.Count);

            await Task.Run(() =>
            {
                for (int i = 0; i < allFiles.Count; i++)
                {
                    var file = allFiles[i];
                    
                    progressCallback?.Invoke(Path.GetFileName(file), i + 1, allFiles.Count);

                    try
                    {
                        var fileMatches = SearchInFile(file, searchText, matchCase, matchWholeWord, useRegex, useExtendedSearch);
                        matches.AddRange(fileMatches);
                    }
                    catch (Exception)
                    {
                        // Skip files that can't be read (binary files, access denied, etc.)
                        continue;
                    }
                }
            });

            return matches;
        }

        internal List<string> GetFilesToSearch(string[] directories, string[] filters, bool searchInSubFolders)
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

                foreach (var filter in filters)
                {
                    try
                    {
                        var searchOption = searchInSubFolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                        var files = Directory.GetFiles(directory, filter, searchOption);
                        allFiles.AddRange(files);
                    }
                    catch (Exception)
                    {
                        // Skip if access denied or other issue
                    }
                }
            }

            return allFiles.Distinct().ToList();
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
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    bool found = false;

                    if (useRegex)
                    {
                        try
                        {
                            var regexOptions = matchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
                            found = Regex.IsMatch(line, searchText, regexOptions);
                        }
                        catch
                        {
                            // Invalid regex, treat as normal search
                            found = PerformNormalSearch(line, searchText, matchCase, matchWholeWord);
                        }
                    }
                    else if (useExtendedSearch)
                    {
                        var processedSearchText = ProcessExtendedSearch(searchText);
                        found = PerformNormalSearch(line, processedSearchText, matchCase, matchWholeWord);
                    }
                    else
                    {
                        found = PerformNormalSearch(line, searchText, matchCase, matchWholeWord);
                    }

                    if (found)
                    {
                        results.Add(new SearchResult
                        {
                            FilePath = filePath,
                            LineNumber = i + 1,
                            LineContent = line
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
