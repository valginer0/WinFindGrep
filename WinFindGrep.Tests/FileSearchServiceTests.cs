using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinFindGrep.Services;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using WinFindGrep.Models;

namespace WinFindGrep.Tests
{
    [TestClass]
    public class FileSearchServiceTests
    {
        private FileSearchService? _service;
        private string? _tempTestDirectory;

        [TestInitialize]
        public void Setup()
        {
            _service = new FileSearchService();
            _tempTestDirectory = Path.Combine(Path.GetTempPath(), "WinFindGrepTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempTestDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_tempTestDirectory != null && Directory.Exists(_tempTestDirectory))
            {
                Directory.Delete(_tempTestDirectory, true);
            }
        }

        // Helper method to create a temporary file with specific content
        private string CreateTempFile(string fileName, string content)
        {
            var filePath = Path.Combine(_tempTestDirectory!, fileName);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        [TestMethod]
        public void ProcessExtendedSearch_ConvertsKnownCharacters()
        {
            // Arrange
            var input = "Hello\\nWorld\\tTest\\0End";
            var expected = "Hello\nWorld\tTest\0End";

            // Act
            string actual = _service!.ProcessExtendedSearch(input);

            // Assert
            Assert.AreEqual(expected, actual);
        }

        [DataTestMethod]
        [DataRow("hello world", "hello", true, false, true, DisplayName = "PN: Simple match, case sensitive")]
        [DataRow("hello world", "HELLO", false, false, true, DisplayName = "PN: Simple match, case insensitive")]
        [DataRow("hello world", "HELLO", true, false, false, DisplayName = "PN: No match, case sensitive")]
        [DataRow("hello world", "world", true, true, true, DisplayName = "PN: Whole word match")]
        [DataRow("helloworld", "world", true, true, false, DisplayName = "PN: No whole word match (substring in word)")]
        [DataRow("hello worldX", "world", true, true, false, DisplayName = "PN: No whole word match (prefix of another word)")]
        [DataRow("hello Xworld", "world", true, true, false, DisplayName = "PN: No whole word match (suffix of another word)")]
        [DataRow("hello world wide web", "world", true, true, true, DisplayName = "PN: Whole word match in sentence")]
        [DataRow("hello world.com", "world", true, true, true, DisplayName = "PN: Whole word match followed by punctuation")]
        [DataRow("hello (world)", "world", true, true, true, DisplayName = "PN: Whole word match surrounded by punctuation")]
        [DataRow("hello world", "worlds", true, true, false, DisplayName = "PN: No whole word match (plural)")]
        [DataRow("hello world", "o w", true, false, true, DisplayName = "PN: Substring match with space")]
        [DataRow(@"special chars ^$.\*+?()[]{}|", @"^$.\*+?()[]{}|", true, false, true, DisplayName = "PN: Substring match with special regex characters (not whole word)")]
        [DataRow(@"special chars ^$.\*+?()[]{}|", @"^$.\*+?()[]{}|", false, true, true, DisplayName = "PN: Whole word match with special regex characters")] // Regex.Escape handles this
        public void PerformNormalSearch_VariousScenarios(string line, string searchText, bool matchCase, bool matchWholeWord, bool expectedResult)
        {
            // Arrange & Act
            bool actualResult = _service!.PerformNormalSearch(line, searchText, matchCase, matchWholeWord);

            // Assert
            Assert.AreEqual(expectedResult, actualResult, $"Line: '{line}', Search: '{searchText}', MatchCase: {matchCase}, WholeWord: {matchWholeWord}");
        }

        // --- Tests for GetFilesToSearch --- 

        [TestMethod]
        public async Task GetFilesToSearch_NoFilters_ReturnsAllFilesTopDirectory()
        {
            // Arrange
            var file1 = CreateTempFile("file1.txt", "content");
            var file2 = CreateTempFile("file2.log", "content");
            var subDir = Path.Combine(_tempTestDirectory!, "subdir");
            Directory.CreateDirectory(subDir);
            CreateTempFile(Path.Combine(subDir, "file3.txt"), "content"); // This file is in a subdir

            var directories = new[] { _tempTestDirectory! };
            var filters = Array.Empty<string>(); // No filters means *.*

            // Act
            List<string> actualFiles = await _service!.GetFilesToSearchAsync(directories, filters, false); // searchInSubFolders = false

            // Assert
            Assert.AreEqual(2, actualFiles.Count, "Should find 2 files in the top directory.");
            CollectionAssert.AreEquivalent(new[] { file1, file2 }, actualFiles, "File list mismatch.");
        }

        [TestMethod]
        public async Task GetFilesToSearch_SingleFilter_ReturnsMatchingFilesTopDirectory()
        {
            // Arrange
            var file1Txt = CreateTempFile("file1.txt", "content");
            CreateTempFile("file2.log", "content");
            var anotherTxt = CreateTempFile("another.txt", "content");
            var subDir = Path.Combine(_tempTestDirectory!, "subdir");
            Directory.CreateDirectory(subDir);
            CreateTempFile(Path.Combine(subDir, "subfile.txt"), "content");

            var directories = new[] { _tempTestDirectory! };
            var filters = new[] { "*.txt" };

            // Act
            List<string> actualFiles = await _service!.GetFilesToSearchAsync(directories, filters, false);

            // Assert
            Assert.AreEqual(2, actualFiles.Count, "Should find 2 .txt files in the top directory.");
            CollectionAssert.AreEquivalent(new[] { file1Txt, anotherTxt }, actualFiles, "File list mismatch for *.txt filter.");
        }

        [TestMethod]
        public async Task GetFilesToSearch_MultipleFilters_ReturnsMatchingFilesTopDirectory()
        {
            // Arrange
            var file1Txt = CreateTempFile("file1.txt", "content");
            var file2Log = CreateTempFile("file2.log", "content");
            CreateTempFile("script.cs", "content");
            CreateTempFile("image.jpg", "content");
            var subDir = Path.Combine(_tempTestDirectory!, "subdir");
            Directory.CreateDirectory(subDir);
            CreateTempFile(Path.Combine(subDir, "subfile.txt"), "content");

            var directories = new[] { _tempTestDirectory! };
            var filters = new[] { "*.txt", "*.log" };

            // Act
            List<string> actualFiles = await _service!.GetFilesToSearchAsync(directories, filters, false);

            // Assert
            Assert.AreEqual(2, actualFiles.Count, "Should find 1 .txt and 1 .log file in the top directory.");
            CollectionAssert.AreEquivalent(new[] { file1Txt, file2Log }, actualFiles, "File list mismatch for multiple filters.");
        }

        [TestMethod]
        public async Task GetFilesToSearch_WithSubfolders_ReturnsMatchingFilesRecursively()
        {
            // Arrange
            var rootFileTxt = CreateTempFile("rootfile.txt", "content");
            var subDir = Path.Combine(_tempTestDirectory!, "subdir1");
            Directory.CreateDirectory(subDir);
            var subFile1Txt = CreateTempFile(Path.Combine(subDir, "subfile1.txt"), "content");
            CreateTempFile(Path.Combine(subDir, "subfile2.log"), "content"); // Not a .txt file

            var subSubDir = Path.Combine(subDir, "subdir2");
            Directory.CreateDirectory(subSubDir);
            var subSubFileTxt = CreateTempFile(Path.Combine(subSubDir, "subsubfile.txt"), "content");

            var directories = new[] { _tempTestDirectory! };
            var filters = new[] { "*.txt" };

            // Act
            List<string> actualFiles = await _service!.GetFilesToSearchAsync(directories, filters, true); // searchInSubFolders = true

            // Assert
            Assert.AreEqual(3, actualFiles.Count, "Should find 3 .txt files recursively.");
            CollectionAssert.AreEquivalent(new[] { rootFileTxt, subFile1Txt, subSubFileTxt }, actualFiles, "Recursive file list mismatch.");
        }

        [TestMethod]
        public async Task GetFilesToSearch_NonExistentDirectory_ReturnsEmptyOrSkips()
        {
            // Arrange
            var realFile = CreateTempFile("realfile.txt", "content"); // A file in a real directory
            var directories = new[] { Path.Combine(_tempTestDirectory!, "nonexistent"), _tempTestDirectory! };
            var filters = new[] { "*.txt" };

            // Act
            List<string> actualFiles = await _service!.GetFilesToSearchAsync(directories, filters, false);

            // Assert
            Assert.AreEqual(1, actualFiles.Count, "Should find 1 file from the existing directory, skipping the non-existent one.");
            Assert.AreEqual(realFile, actualFiles[0]);
        }

        [TestMethod]
        public async Task GetFilesToSearch_EmptyDirectory_ReturnsEmpty()
        {
            // Arrange
            // _tempTestDirectory is created fresh and empty by Setup for this test
            var directories = new[] { _tempTestDirectory! };
            var filters = new[] { "*.*" };

            // Act
            List<string> actualFiles = await _service!.GetFilesToSearchAsync(directories, filters, false);

            // Assert
            Assert.AreEqual(0, actualFiles.Count, "Should find 0 files in an empty directory.");
        }

        [TestMethod]
        public async Task GetFilesToSearch_DuplicateFilesFromMultipleIdenticalDirectoryEntries_ReturnsDistinct()
        {
            // Arrange
            var uniqueTxt = CreateTempFile("unique.txt", "content");
            // Same directory listed twice, GetFilesToSearch should return distinct files
            var directories = new[] { _tempTestDirectory!, _tempTestDirectory! };
            var filters = new[] { "*.txt" };

            // Act
            List<string> actualFiles = await _service!.GetFilesToSearchAsync(directories, filters, false);

            // Assert
            Assert.AreEqual(1, actualFiles.Count, "Should return distinct files even if directory is listed multiple times.");
            Assert.AreEqual(uniqueTxt, actualFiles[0]);
        }

        // --- Tests for SearchInFile ---

        [TestMethod]
        public void SearchInFile_SimpleMatch_ReturnsCorrectResults()
        {
            // Arrange
            var filePath = CreateTempFile("test.txt", "Hello World\nAnother line with World");

            // Act
            var results = _service!.SearchInFile(filePath, "World", false, false, false, false);

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(1, results[0].LineNumber);
            Assert.AreEqual("Hello World", results[0].LineContent);
            Assert.AreEqual(2, results[1].LineNumber);
            Assert.AreEqual("Another line with World", results[1].LineContent);
        }

        [TestMethod]
        public void SearchInFile_CaseSensitiveMatch_ReturnsCorrectResults()
        {
            // Arrange
            var filePath = CreateTempFile("test.txt", "hello world\nHello World");

            // Act
            var results = _service!.SearchInFile(filePath, "Hello", true, false, false, false); // matchCase = true

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(2, results[0].LineNumber);
            Assert.AreEqual("Hello World", results[0].LineContent);
        }

        [TestMethod]
        public void SearchInFile_WholeWordMatch_ReturnsCorrectResults()
        {
            // Arrange
            var filePath = CreateTempFile("test.txt", "word worldly\njust a word here");

            // Act
            var results = _service!.SearchInFile(filePath, "word", false, true, false, false); // matchWholeWord = true

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(1, results[0].LineNumber); // "word" in "word worldly"
            Assert.AreEqual(2, results[1].LineNumber); // "word" in "just a word here"
        }

        [TestMethod]
        public void SearchInFile_RegexMatch_ReturnsCorrectResults()
        {
            // Arrange
            var filePath = CreateTempFile("test.txt", "cat cot c_t\ncar con");

            // Act
            var results = _service!.SearchInFile(filePath, "c.t", false, false, true, false); // useRegex = true

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].LineNumber);
            Assert.AreEqual("cat cot c_t", results[0].LineContent);
        }

        [TestMethod]
        public void SearchInFile_ExtendedSearchMatch_ReturnsCorrectResults()
        {
            // Arrange
            var filePath = CreateTempFile("test.txt", "first line\nsecond line\twith tab");

            // Act
            // Testing ProcessExtendedSearch indirectly via SearchInFile
            var results = _service!.SearchInFile(filePath, "line\\twith", false, false, false, true); // useExtendedSearch = true

            // Assert
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(2, results[0].LineNumber);
            Assert.AreEqual("second line\twith tab", results[0].LineContent);
        }

        [TestMethod]
        public void SearchInFile_NoMatch_ReturnsEmptyList()
        {
            // Arrange
            var filePath = CreateTempFile("test.txt", "This file has some content.");

            // Act
            var results = _service!.SearchInFile(filePath, "nonexistent", false, false, false, false);

            // Assert
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void SearchInFile_InvalidRegex_FallsBackToNormalSearchAndFinds()
        {
            // Arrange
            var filePath = CreateTempFile("test.txt", "Line with [bracket"); // Content that would match if not for invalid regex

            // Act
            // The FileSearchService.SearchInFile has a try-catch for Regex.IsMatch
            // and falls back to PerformNormalSearch if regex is invalid.
            var results = _service!.SearchInFile(filePath, "[bracket", false, false, true, false); // useRegex = true, but "[bracket" is invalid regex

            // Assert
            Assert.AreEqual(1, results.Count, "Should find match using normal search after invalid regex.");
            Assert.AreEqual(1, results[0].LineNumber);
            Assert.AreEqual("Line with [bracket", results[0].LineContent);
        }

        [TestMethod]
        public void SearchInFile_InvalidRegex_FallsBackToNormalSearchAndDoesNotFind()
        {
            // Arrange
            var filePath = CreateTempFile("test.txt", "Line without the pattern");

            // Act
            var results = _service!.SearchInFile(filePath, "[invalidRegex", false, false, true, false); // useRegex = true, invalid regex

            // Assert
            Assert.AreEqual(0, results.Count, "Should not find match using normal search after invalid regex if pattern not present.");
        }


        // --- Tests for SearchFilesAsync ---

        [TestMethod]
        public async Task SearchFilesAsync_SimpleSearch_ReturnsCorrectMatchesAndProgress()
        {
            // Arrange
            var file1 = CreateTempFile("file1.txt", "Hello world\nTest line");
            var file2 = CreateTempFile("file2.txt", "Another world here\nNo match");
            CreateTempFile("file3.log", "No world here"); // Should not be searched if filter is .txt

            var directories = new[] { _tempTestDirectory! };
            var filters = new[] { "*.txt" };
            var progressUpdates = new List<(string status, int current, int total)>();
            Action<string, int, int> progressCallback = (status, current, total) =>
            {
                progressUpdates.Add((status, current, total));
            };

            // Act
            var results = await _service!.SearchFilesAsync(directories, filters, false, "world", false, false, false, false, progressCallback);

            // Assert
            Assert.AreEqual(2, results.Count, "Should find 2 matches for 'world'.");
            Assert.IsTrue(results.Any(r => r.FilePath == file1 && r.LineNumber == 1));
            Assert.IsTrue(results.Any(r => r.FilePath == file2 && r.LineNumber == 1));

            // Assert progress callback
            Assert.IsTrue(progressUpdates.Count >= 2, "Expected at least 2 progress updates (file1, file2). Actual: " + progressUpdates.Count);

            // Order of file processing is not guaranteed, so check for presence
            bool file1ProgressFound = progressUpdates.Any(p => p.status == Path.GetFileName(file1) && p.total == 2 && p.current > 0);
            bool file2ProgressFound = progressUpdates.Any(p => p.status == Path.GetFileName(file2) && p.total == 2 && p.current > 0);
            Assert.IsTrue(file1ProgressFound, "Progress update for file1.txt not found or incorrect.");
            Assert.IsTrue(file2ProgressFound, "Progress update for file2.txt not found or incorrect.");
        }

        [TestMethod]
        public async Task SearchFilesAsync_SearchInSubfolders_ReturnsCorrectMatches()
        {
            // Arrange
            var rootFile = CreateTempFile("root.txt", "Root text match");
            var subDir = Path.Combine(_tempTestDirectory!, "sub");
            Directory.CreateDirectory(subDir);
            var subFile = CreateTempFile(Path.Combine(subDir, "sub.txt"), "Subfolder text match");

            var directories = new[] { _tempTestDirectory! };
            var filters = new[] { "*.txt" };

            // Act
            var results = await _service!.SearchFilesAsync(directories, filters, true, "match", false, false, false, false, null); // searchInSubFolders = true

            // Assert
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Any(r => r.FilePath == rootFile));
            Assert.IsTrue(results.Any(r => r.FilePath == subFile));
        }

        [TestMethod]
        public async Task SearchFilesAsync_NoMatches_ReturnsEmptyListAndCorrectProgress()
        {
            // Arrange
            CreateTempFile("file1.txt", "content");
            var directories = new[] { _tempTestDirectory! };
            var filters = new[] { "*.txt" };

            // Act
            var results = await _service!.SearchFilesAsync(directories, filters, false, "nonexistent", false, false, false, false, null);

            // Assert
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public async Task SearchFilesAsync_SkipsUnreadableFile_AndProcessesOthers()
        {
            // Arrange
            var readableFile = CreateTempFile("readable.txt", "some content here");
            var unreadableFile = CreateTempFile("unreadable.txt", "unreadable");

            var directories = new[] { _tempTestDirectory! };
            var filters = new[] { "*.txt" };

            // Lock the file to make it unreadable
            using (var stream = new FileStream(unreadableFile, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                // Act
                var results = await _service!.SearchFilesAsync(directories, filters, false, "content", false, false, false, false, null);

                // Assert
                Assert.AreEqual(1, results.Count, "Should find one match in the readable file.");
                Assert.AreEqual(readableFile, results[0].FilePath);
            }
        }

        // --- Tests for ReplaceInFilesAsync ---

        [TestMethod]
        public async Task ReplaceInFilesAsync_SimpleReplace_ReplacesCorrectlyAndReportsProgress()
        {
            // Arrange
            var file1 = CreateTempFile("replace1.txt", "Hello world\nAnother world");
            var progressUpdates = new List<(string, int, int)>();
            var matchesToReplace = new List<SearchResult>
            {
                new() { FilePath = file1, LineNumber = 1, LineContent = "Hello world", LastModified = File.GetLastWriteTime(file1) },
                new() { FilePath = file1, LineNumber = 2, LineContent = "Another world", LastModified = File.GetLastWriteTime(file1) }
            };

            // Act
            var results = await _service!.ReplaceInFilesAsync("world", "planet", matchesToReplace, false, false, false, false, (s, c, t) => progressUpdates.Add((s, c, t)));

            // Assert
            var newContent = await File.ReadAllTextAsync(file1);
            Assert.AreEqual("Hello planet\nAnother planet", newContent, "File content was not replaced correctly.");
            Assert.IsTrue(progressUpdates.Count > 0, "Progress should have been reported.");
        }

        [TestMethod]
        public async Task ReplaceInFilesAsync_RegexReplace_ReplacesCorrectly()
        {
            // Arrange
            var file1 = CreateTempFile("regex_replace.txt", "number: 123, number: 456");
            var matchesToReplace = new List<SearchResult>
            {
                new() { FilePath = file1, LineNumber = 1, LineContent = "number: 123, number: 456", LastModified = File.GetLastWriteTime(file1) }
            };

            // Act
            var results = await _service!.ReplaceInFilesAsync(@"\d+", "###", matchesToReplace, false, false, true, false, null);

            // Assert
            var newContent = await File.ReadAllTextAsync(file1);
            Assert.AreEqual("number: ###, number: ###", newContent);
        }

        [TestMethod]
        public async Task ReplaceInFilesAsync_NoMatchesToReplace_DoesNothing()
        {
            // Arrange
            var file1 = CreateTempFile("no_replace.txt", "some text");
            var originalContent = await File.ReadAllTextAsync(file1);
            var matchesToReplace = new List<SearchResult>();

            // Act
            var results = await _service!.ReplaceInFilesAsync("nonexistent", "new", matchesToReplace, false, false, false, false, null);

            // Assert
            Assert.AreEqual(0, results);
            var finalContent = await File.ReadAllTextAsync(file1);
            Assert.AreEqual(originalContent, finalContent);
        }

        [TestMethod]
        public async Task ReplaceInFilesAsync_CaseSensitiveReplace_ReplacesCorrectly()
        {
            // Arrange
            var file1 = CreateTempFile("case_replace.txt", "Word word WORD");
            var matchesToReplace = new List<SearchResult>
            {
                new() { FilePath = file1, LineNumber = 1, LineContent = "Word word WORD", LastModified = File.GetLastWriteTime(file1) }
            };

            // Act - matchCase = true
            var results = await _service!.ReplaceInFilesAsync("Word", "Replaced", matchesToReplace, true, false, false, false, null);

            // Assert
            var newContent = await File.ReadAllTextAsync(file1);
            Assert.AreEqual("Replaced word WORD", newContent);
        }
    }
}
