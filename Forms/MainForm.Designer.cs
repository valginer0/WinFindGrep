using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFindGrep.Forms
{
    partial class MainForm
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
        
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
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
            txtFilters = new TextBox { Location = new Point(95, 72), Size = new Size(350, 23), Text = "*.txt" };

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

        #endregion
    }
}
