using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFindGrep.Models;
using WinFindGrep.Services;

namespace WinFindGrep.Forms
{
    public partial class MainForm : Form
    {
        private readonly FileSearchService _searchService;
        private readonly HistoryService _historyService;
        private CancellationTokenSource? _cts;

        public MainForm()
        {
            InitializeComponent();
            _searchService = new FileSearchService();
            _historyService = new HistoryService();
            LoadSearchHistory();
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
                    var selectedPath = dialog.SelectedPath;
                    
                    // Add to history/dropdown immediately so it's visible
                    if (!txtDirectories.Items.Contains(selectedPath))
                    {
                        txtDirectories.Items.Insert(0, selectedPath);
                    }
                    
                    // If text is empty or just default, replace it. Otherwise append.
                    if (string.IsNullOrEmpty(txtDirectories.Text) || txtDirectories.Text == "C:\\")
                    {
                        txtDirectories.Text = selectedPath;
                    }
                    else
                    {
                        // Check if already in the text list to avoid duplicates
                        var currentDirs = txtDirectories.Text.Split(',').Select(d => d.Trim()).ToList();
                        if (!currentDirs.Contains(selectedPath, StringComparer.OrdinalIgnoreCase))
                        {
                            txtDirectories.Text += "," + selectedPath;
                        }
                    }
                }
            }
        }

        private async void BtnFindAll_Click(object sender, EventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                return;
            }

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

            _cts = new CancellationTokenSource();
            btnFindAll.Text = "Stop";
            btnReplaceInFiles.Enabled = false;
            lvResults.Items.Clear();
            progressBar.Value = 0;
            lblStatus.Text = "Searching...";

            // Save history
            _historyService.AddToHistory(txtDirectories.Text);
            LoadSearchHistory(); // Refresh list to show new item at top

            try
            {
                await SearchFiles(_cts.Token);
                lblStatus.Text = $"Search completed. Found {lvResults.Items.Count} matches.";
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Search canceled.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during search: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "Error occurred.";
            }
            finally
            {
                btnFindAll.Text = "Find All";
                btnFindAll.Enabled = true;
                btnReplaceInFiles.Enabled = true;
                progressBar.Value = 0;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task SearchFiles(CancellationToken token = default)
        {
            var searchText = txtFindWhat.Text;
            var directories = txtDirectories.Text.Split(',').Select(d => d.Trim()).Where(d => !string.IsNullOrEmpty(d)).ToArray();
            var filters = txtFilters.Text.Split(',').Select(f => f.Trim()).Where(f => !string.IsNullOrEmpty(f)).ToArray();
            
            var matches = await _searchService.SearchFilesAsync(
                directories,
                filters,
                chkInAllSubFolders.Checked,
                searchText,
                chkMatchCase.Checked,
                chkMatchWholeWord.Checked,
                rbRegularExpression.Checked,
                rbExtended.Checked,
                (status, current, total) =>
                {
                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = $"Searching in: {status}";
                        if (total > 0)
                        {
                            progressBar.Maximum = total;
                            progressBar.Value = current;
                        }
                    }));
                },
                token);

            // Update UI with results
            lvResults.Items.Clear();

            // Ensure the "Modified" column exists (added programmatically to avoid designer changes)
            if (lvResults.Columns.Count < 4)
            {
                lvResults.Columns.Add("Modified", 120);
            }

            progressBar.Value = 0;
            foreach (var match in matches)
            {
                var item = new ListViewItem(match.FilePath);
                item.SubItems.Add(match.LineNumber.ToString());
                item.SubItems.Add(match.LineContent.Trim());
                item.SubItems.Add(match.LastModified.ToString("yyyy-MM-dd HH:mm"));
                item.Tag = match;
                lvResults.Items.Add(item);
            }
        }

        private async void BtnReplaceInFiles_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFindWhat.Text) || txtReplaceWith.Text == null)
            {
                MessageBox.Show("Please enter both search and replace text.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lvResults.Items.Count == 0)
            {
                MessageBox.Show("Please perform a search first to find matches to replace.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"This will replace all occurrences of '{txtFindWhat.Text}' with '{txtReplaceWith.Text}' in all matching files. Continue?", 
                "Replace in Files", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                btnFindAll.Enabled = false;
                btnReplaceInFiles.Enabled = false;
                progressBar.Value = 0;
                lblStatus.Text = "Replacing...";

                try
                {
                    // Get all search results from the list view
                    var matchesToReplace = new List<SearchResult>();
                    foreach (ListViewItem item in lvResults.Items)
                    {
                        matchesToReplace.Add((SearchResult)item.Tag);
                    }

                    var replacedCount = await _searchService.ReplaceInFilesAsync(
                        txtFindWhat.Text,
                        txtReplaceWith.Text,
                        matchesToReplace,
                        chkMatchCase.Checked,
                        chkMatchWholeWord.Checked,
                        rbRegularExpression.Checked,
                        rbExtended.Checked,
                        (status, current, total) =>
                        {
                            this.Invoke(new Action(() =>
                            {
                                lblStatus.Text = $"Replacing in: {status}";
                                if (total > 0)
                                {
                                    progressBar.Maximum = total;
                                    progressBar.Value = current;
                                }
                            }));
                        });

                    MessageBox.Show($"Replacement completed. Modified {replacedCount} occurrences.", "Replace", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Refresh the search results after replacing
                    lvResults.Items.Clear();
                    await SearchFiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error during replacement: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    btnFindAll.Enabled = true;
                    btnReplaceInFiles.Enabled = true;
                    lblStatus.Text = "Ready";
                    progressBar.Value = 0;
                }
            }
        }

        private void LvResults_DoubleClick(object sender, EventArgs e)
        {
            if (lvResults.SelectedItems.Count > 0)
            {
                var match = (SearchResult)lvResults.SelectedItems[0].Tag;
                try
                {
                    // Try to open with the default application for this file type
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = match.FilePath,
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        // If that fails, fall back to Notepad
                        System.Diagnostics.Process.Start("notepad.exe", match.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void LoadSearchHistory()
        {
            var currentText = txtDirectories.Text;
            var history = _historyService.LoadHistory();
            txtDirectories.Items.Clear();
            txtDirectories.Items.AddRange(history.ToArray());
            txtDirectories.Text = currentText; // Restore current text
        }
    }
}
