using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFindGrep
{
    public partial class MainForm : Form
    {
        private TextBox txtFindWhat;
        private TextBox txtReplaceWith;
        private TextBox txtFilters;
        private TextBox txtDirectories;
        private CheckBox chkMatchWholeWord;
        private CheckBox chkMatchCase;
        private CheckBox chkFollowCurrentDoc;
        private CheckBox chkInAllSubFolders;
        private CheckBox chkInHiddenFolders;
        private RadioButton rbNormal;
        private RadioButton rbExtended;
        private RadioButton rbRegularExpression;
        private CheckBox chkTransparency;
        private RadioButton rbOnLosingFocus;
        private RadioButton rbAlways;
        private TrackBar tbTransparency;
        private Button btnFindAll;
        private Button btnReplaceInFiles;
        private Button btnClose;
        private Button btnBrowse;
        private ListView lvResults;
        private ProgressBar progressBar;
        private Label lblStatus;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form properties
            this.Text = "Multi-Directory File Search";
            this.Size = new Size(650, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimumSize = new Size(650, 500);

            // Find what
            var lblFindWhat = new Label { Text = "Find what:", Location = new Point(12, 15), Size = new Size(80, 23) };
            txtFindWhat = new TextBox { Location = new Point(95, 12), Size = new Size(350, 23) };

            // Replace with
            var lblReplaceWith = new Label { Text = "Replace with:", Location = new Point(12, 45), Size = new Size(80, 23) };
            txtReplaceWith = new TextBox { Location = new Point(95, 42), Size = new Size(350, 23) };

            // Filters
            var lblFilters = new Label { Text = "Filters:", Location = new Point(12, 75), Size = new Size(50, 23) };
            txtFilters = new TextBox { Location = new Point(95, 72), Size = new Size(350, 23), Text = "*.*" };

            // Directories (modified to support multiple)
            var lblDirectories = new Label { Text = "Directories:", Location = new Point(12, 105), Size = new Size(80, 23) };
            txtDirectories = new TextBox { Location = new Point(95, 102), Size = new Size(300, 23), Text = "C:\\" };
            btnBrowse = new Button { Text = "...", Location = new Point(400, 102), Size = new Size(30, 23) };
            btnBrowse.Click += BtnBrowse_Click;

            // Add help label for multiple directories
            var lblDirectoryHelp = new Label 
            { 
                Text = "Separate multiple directories with commas", 
                Location = new Point(95, 125), 
                Size = new Size(300, 15),
                ForeColor = Color.Gray,
                Font = new Font(Font.FontFamily, 7.5f)
            };

            // Search options
            chkMatchWholeWord = new CheckBox { Text = "Match whole word only", Location = new Point(12, 150), Size = new Size(150, 20) };
            chkMatchCase = new CheckBox { Text = "Match case", Location = new Point(12, 175), Size = new Size(100, 20) };
            chkFollowCurrentDoc = new CheckBox { Text = "Follow current doc.", Location = new Point(200, 150), Size = new Size(140, 20) };
            chkInAllSubFolders = new CheckBox { Text = "In all sub-folders", Location = new Point(200, 175), Size = new Size(120, 20), Checked = true };
            chkInHiddenFolders = new CheckBox { Text = "In hidden folders", Location = new Point(350, 175), Size = new Size(120, 20) };

            // Search mode
            var gbSearchMode = new GroupBox { Text = "Search Mode", Location = new Point(12, 200), Size = new Size(200, 90) };
            rbNormal = new RadioButton { Text = "Normal", Location = new Point(10, 20), Size = new Size(80, 20), Checked = true };
            rbExtended = new RadioButton { Text = "Extended (\\n, \\r, \\t, \\0, \\x...)", Location = new Point(10, 45), Size = new Size(180, 20) };
            rbRegularExpression = new RadioButton { Text = "Regular expression", Location = new Point(10, 65), Size = new Size(130, 20) };
            gbSearchMode.Controls.AddRange(new Control[] { rbNormal, rbExtended, rbRegularExpression });

            // Transparency
            var gbTransparency = new GroupBox { Text = "Transparency", Location = new Point(220, 200), Size = new Size(200, 90) };
            chkTransparency = new CheckBox { Text = "Transparency", Location = new Point(10, 20), Size = new Size(100, 20) };
            rbOnLosingFocus = new RadioButton { Text = "On losing focus", Location = new Point(10, 40), Size = new Size(110, 20), Checked = true };
            rbAlways = new RadioButton { Text = "Always", Location = new Point(10, 60), Size = new Size(70, 20) };
            tbTransparency = new TrackBar { Location = new Point(120, 40), Size = new Size(70, 40), Minimum = 20, Maximum = 100, Value = 80 };
            gbTransparency.Controls.AddRange(new Control[] { chkTransparency, rbOnLosingFocus, rbAlways, tbTransparency });

            // Buttons
            btnFindAll = new Button { Text = "Find All", Location = new Point(450, 12), Size = new Size(80, 30) };
            btnFindAll.Click += BtnFindAll_Click;
            btnReplaceInFiles = new Button { Text = "Replace in Files", Location = new Point(450, 50), Size = new Size(80, 30) };
            btnReplaceInFiles.Click += BtnReplaceInFiles_Click;
            btnClose = new Button { Text = "Close", Location = new Point(450, 260), Size = new Size(80, 30) };
            btnClose.Click += (s, e) => Close();

            // Results list
            lvResults = new ListView 
            { 
                Location = new Point(12, 300), 
                Size = new Size(610, 120), 
                View = View.Details, 
                FullRowSelect = true, 
                GridLines = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            lvResults.Columns.Add("File", 250);
            lvResults.Columns.Add("Line", 50);
            lvResults.Columns.Add("Content", 300);
            lvResults.DoubleClick += LvResults_DoubleClick;

            // Progress bar
            progressBar = new ProgressBar 
            { 
                Location = new Point(12, 430), 
                Size = new Size(500, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            // Status label
            lblStatus = new Label 
            { 
                Text = "Ready", 
                Location = new Point(520, 430), 
                Size = new Size(100, 20),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            // Add all controls to form
            this.Controls.AddRange(new Control[] {
                lblFindWhat, txtFindWhat,
                lblReplaceWith, txtReplaceWith,
                lblFilters, txtFilters,
                lblDirectories, txtDirectories, btnBrowse, lblDirectoryHelp,
                chkMatchWholeWord, chkMatchCase, chkFollowCurrentDoc, chkInAllSubFolders, chkInHiddenFolders,
                gbSearchMode, gbTransparency,
                btnFindAll, btnReplaceInFiles, btnClose,
                lvResults, progressBar, lblStatus
            });

            this.ResumeLayout(false);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a directory to search in";
                if (!string.IsNullOrEmpty(txtDirectories.Text))
                {
                    var firstDir = txtDirectories.Text.Split(',')[0].Trim();
                    if (Directory.Exists(firstDir))
                        dialog.SelectedPath = firstDir;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (string.IsNullOrEmpty(txtDirectories.Text))
                        txtDirectories.Text = dialog.SelectedPath;
                    else
                        txtDirectories.Text += "," + dialog.SelectedPath;
                }
            }
        }

        private async void BtnFindAll_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFindWhat.Text))
            {
                MessageBox.Show("Please enter text to search for.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtDirectories.Text))
            {
                MessageBox.Show("Please specify at least one directory to search in.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnFindAll.Enabled = false;
            btnReplaceInFiles.Enabled = false;
            lvResults.Items.Clear();
            progressBar.Value = 0;
            lblStatus.Text = "Searching...";

            try
            {
                await SearchFiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during search: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnFindAll.Enabled = true;
                btnReplaceInFiles.Enabled = true;
                lblStatus.Text = $"Search completed. Found {lvResults.Items.Count} matches.";
                progressBar.Value = 0;
            }
        }

        private async Task SearchFiles()
        {
            var searchText = txtFindWhat.Text;
            var directories = txtDirectories.Text.Split(',').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)).ToArray();
            var filters = txtFilters.Text.Split(',').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f)).ToArray();
            
            if (filters.Length == 0) filters = new[] { "*.*" };

            var allFiles = new List<string>();

            // Get all files from all directories
            foreach (var directory in directories)
            {
                if (!Directory.Exists(directory))
                {
                    MessageBox.Show($"Directory does not exist: {directory}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                foreach (var filter in filters)
                {
                    try
                    {
                        var searchOption = chkInAllSubFolders.Checked ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                        var files = Directory.GetFiles(directory, filter, searchOption);
                        allFiles.AddRange(files);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error accessing directory {directory}: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            allFiles = allFiles.Distinct().ToList();
            progressBar.Maximum = allFiles.Count;

            var matches = new List<SearchResult>();

            await Task.Run(() =>
            {
                for (int i = 0; i < allFiles.Count; i++)
                {
                    var file = allFiles[i];
                    
                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = $"Searching in: {Path.GetFileName(file)}";
                        progressBar.Value = i + 1;
                    }));

                    try
                    {
                        var fileMatches = SearchInFile(file, searchText);
                        matches.AddRange(fileMatches);
                    }
                    catch (Exception ex)
                    {
                        // Skip files that can't be read (binary files, access denied, etc.)
                        continue;
                    }
                }
            });

            // Update UI with results
            foreach (var match in matches)
            {
                var item = new ListViewItem(match.FilePath);
                item.SubItems.Add(match.LineNumber.ToString());
                item.SubItems.Add(match.LineContent.Trim());
                item.Tag = match;
                lvResults.Items.Add(item);
            }
        }

        private List<SearchResult> SearchInFile(string filePath, string searchText)
        {
            var results = new List<SearchResult>();

            try
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    bool found = false;

                    if (rbRegularExpression.Checked)
                    {
                        try
                        {
                            var regexOptions = chkMatchCase.Checked ? RegexOptions.None : RegexOptions.IgnoreCase;
                            found = Regex.IsMatch(line, searchText, regexOptions);
                        }
                        catch
                        {
                            // Invalid regex, treat as normal search
                            found = PerformNormalSearch(line, searchText);
                        }
                    }
                    else if (rbExtended.Checked)
                    {
                        var processedSearchText = ProcessExtendedSearch(searchText);
                        found = PerformNormalSearch(line, processedSearchText);
                    }
                    else
                    {
                        found = PerformNormalSearch(line, searchText);
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

        private bool PerformNormalSearch(string line, string searchText)
        {
            var comparison = chkMatchCase.Checked ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            
            if (chkMatchWholeWord.Checked)
            {
                var words = line.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                return words.Any(word => string.Equals(word, searchText, comparison));
            }
            else
            {
                return line.IndexOf(searchText, comparison) >= 0;
            }
        }

        private string ProcessExtendedSearch(string searchText)
        {
            return searchText
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\0", "\0");
        }

        private void BtnReplaceInFiles_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFindWhat.Text) || txtReplaceWith.Text == null)
            {
                MessageBox.Show("Please enter both search and replace text.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"This will replace all occurrences of '{txtFindWhat.Text}' with '{txtReplaceWith.Text}' in all matching files. Continue?", 
                "Replace in Files", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                // Implementation for replace functionality would go here
                MessageBox.Show("Replace functionality would be implemented here.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void LvResults_DoubleClick(object sender, EventArgs e)
        {
            if (lvResults.SelectedItems.Count > 0)
            {
                var match = (SearchResult)lvResults.SelectedItems[0].Tag;
                try
                {
                    System.Diagnostics.Process.Start("notepad.exe", match.FilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    public class SearchResult
    {
        public string FilePath { get; set; }
        public int LineNumber { get; set; }
        public string LineContent { get; set; }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}