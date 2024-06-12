using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace DoenaSoft.FileTransferManager;

internal partial class MainForm : Form, IMainForm
{
    private string _selectedSourcePath;

    private string _selectedTargetPath;

    private Copier _copier;

    private IEnumerable<CopyItem> SourceListBoxItems
        => SourceListBox.Items.Cast<CopyItem>().ToList();

    int IMainForm.ProgressBarMax
    {
        get => CopyProgressBar.Maximum;
        set => CopyProgressBar.Maximum = value;
    }

    public MainForm()
    {
        _selectedSourcePath = null;

        _selectedTargetPath = null;

        this.InitializeComponent();

        this.Icon = Resources.djdsoft;

        this.Text += $" {Assembly.GetExecutingAssembly().GetName().Version}";
    }

    private void OnRemoveEntryButtonClick(object sender, EventArgs e)
    {
        if (SourceListBox.SelectedIndex != -1)
        {
            var previousIndex = SourceListBox.SelectedIndex;

            SourceListBox.Items.RemoveAt(previousIndex);

            if (SourceListBox.Items.Count > previousIndex)
            {
                SourceListBox.SelectedIndex = previousIndex;
            }
            else if (SourceListBox.Items.Count > 0)
            {
                SourceListBox.SelectedIndex = SourceListBox.Items.Count - 1;
            }

            this.FormatBytes();
        }
    }

    private void OnAddFolderButtonClick(object sender, EventArgs e)
    {
        using var sourceDialog = new FolderBrowserDialog();

        sourceDialog.ShowNewFolderButton = false;
        sourceDialog.Description = "Select Source Folder to Copy";

        if (Directory.Exists(_selectedSourcePath))
        {
            sourceDialog.SelectedPath = _selectedSourcePath;
        }
        else
        {
            sourceDialog.RootFolder = Environment.SpecialFolder.MyComputer;
        }

        if (sourceDialog.ShowDialog() == DialogResult.OK)
        {
            var selectedPath = sourceDialog.SelectedPath;

            if (!Regex.IsMatch(selectedPath, @"^[a-zA-Z]:\\", RegexOptions.IgnoreCase))
            {
                this.ShowMessageBox($"'{selectedPath}' is not valid. Please choose a sub-folder of a letter drive.", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                var selectedFolder = new DirectoryInfo(selectedPath);

                if (selectedFolder.FullName.Equals(selectedFolder.Root.FullName))
                {
                    this.ShowMessageBox($"'{selectedPath}' is not valid. Please choose a sub-folder of a letter drive.", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else if (this.ShowTargetDialog(out var targetFolder))
                {
                    this.AddFolder(targetFolder, sourceDialog.SelectedPath);

                    _selectedSourcePath = sourceDialog.SelectedPath;
                }
            }
        }
    }

    private void AddFolder(DirectoryInfo targetFolder, string sourceFolderName)
    {
        var sourceFolder = new DirectoryInfo(sourceFolderName);

        SourceListBox.Items.Add(new CopyItem(sourceFolder, targetFolder));

        this.FormatBytes();
    }

    private bool ShowTargetDialog(out DirectoryInfo targetFolder)
    {
        using var targetDialog = new FolderBrowserDialog();

        targetDialog.ShowNewFolderButton = true;
        targetDialog.Description = "Select Target Folder to Copy";

        if (Directory.Exists(_selectedTargetPath))
        {
            targetDialog.SelectedPath = _selectedTargetPath;
        }
        else
        {
            targetDialog.RootFolder = Environment.SpecialFolder.MyComputer;
        }

        if (targetDialog.ShowDialog() == DialogResult.OK)
        {
            targetFolder = new DirectoryInfo(targetDialog.SelectedPath);

            _selectedTargetPath = targetDialog.SelectedPath;

            return true;
        }
        else
        {
            targetFolder = null;

            return false;
        }
    }

    private void OnAddFileButtonClick(object sender, EventArgs e)
    {
        using var sourceDialog = new OpenFileDialog();

        sourceDialog.CheckFileExists = true;
        sourceDialog.Multiselect = true;
        sourceDialog.Title = "Select File(s) to Copy";

        if (Directory.Exists(_selectedSourcePath))
        {
            sourceDialog.InitialDirectory = _selectedSourcePath;
        }
        else
        {
            sourceDialog.RestoreDirectory = true;
        }

        if (sourceDialog.ShowDialog() == DialogResult.OK
            && this.ShowTargetDialog(out var targetFolder))
        {
            this.AddFiles(targetFolder, sourceDialog.FileNames);

            _selectedSourcePath = sourceDialog.InitialDirectory;
        }
    }

    private void AddFiles(DirectoryInfo targetFolder, params string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            var sourceFile = new FileInfo(fileName);

            SourceListBox.Items.Add(new CopyItem(sourceFile, targetFolder));
        }

        this.FormatBytes();
    }

    private void OnCopyButtonClick(object sender, EventArgs e)
    {
        if (TaskbarManager.IsPlatformSupported)
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            TaskbarManager.Instance.SetProgressValue(0, CopyProgressBar.Maximum);
        }

        CopyProgressBar.Value = 0;

        var targetItems = new List<CopyItem>();

        foreach (var sourceItems in this.SourceListBoxItems)
        {
            if (sourceItems.SourceFolder?.Exists == true)
            {
                Helper.AddFolder(targetItems, sourceItems, WithSubFoldersCheckBox.Checked);
            }
            else if (sourceItems.SourceFile?.Exists == true)
            {
                targetItems.Add(sourceItems);
            }
            else
            {
                this.ShowMessageBox("Something is weird about\n" + sourceItems, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }
        }

        var (allBytes, divider) = Helper.CheckDriveSize(targetItems, this);

        if (allBytes <= 0)
        {
            return;
        }

        CopyProgressBar.Maximum = (int)(allBytes / divider);

        targetItems.Sort((left, right) => left.SourceFile.FullName.CompareTo(right.SourceFile.FullName));

        _copier = new Copier(targetItems.AsReadOnly(), OverwriteComboBox.Text, divider, this);

        _copier.CopyFinished += this.OnMainFormCopyFinished;

        this.SwitchUI(false);

        _copier.Start();
    }

    private void SwitchUI(bool enable)
    {
        var inverse = enable == false;

        AddFileButton.Enabled = enable;

        AddFolderButton.Enabled = enable;

        RemoveEntryButton.Enabled = enable;

        ClearListButton.Enabled = enable;

        SourceListBox.Enabled = enable;

        CopyButton.Enabled = enable;

        CopyButton.Visible = enable;

        WithSubFoldersCheckBox.Enabled = enable;

        OverwriteComboBox.Enabled = enable;

        ImportListButton.Enabled = enable;

        this.UseWaitCursor = inverse;

        AbortButton.Enabled = inverse;
        AbortButton.Visible = inverse;
    }

    private void OnMainFormCopyFinished(object sender, EventArgs e)
    {
        this.SwitchUI(true);

        _copier.CopyFinished -= this.OnMainFormCopyFinished;
    }

    public DialogResult ShowMessageBox(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        var func = new Func<DialogResult>(() => MessageBox.Show(this, message, title, buttons, icon));

        var result = this.InvokeRequired
            ? (DialogResult)this.Invoke(func)
            : func();

        return result;
    }

    private void OnMainFormLoad(object sender, EventArgs e)
        => OverwriteComboBox.SelectedIndex = 0;

    private void OnClearListButtonClick(object sender, EventArgs e)
    {
        SourceListBox.Items.Clear();

        this.FormatBytes();
    }

    public void UpdateProgressBar(long bytes, DateTime start, long divider)
    {
        if (bytes == -1)
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            }

            CopyProgressBar.Value = CopyProgressBar.Maximum;

            RemaingLabel.Text = ".....";
        }
        else
        {
            var value = (int)(bytes / divider);

            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressValue(value, CopyProgressBar.Maximum);
            }

            CopyProgressBar.Value = value;

            if (CopyProgressBar.Value != 0)
            {
                this.SetRemaingLabelText(start, bytes);
            }
        }

        CopyProgressBar.Update();
        CopyProgressBar.Refresh();
    }

    private void SetRemaingLabelText(DateTime start, long bytes)
    {
        var span = DateTime.UtcNow.Subtract(start);

        var completeTimeTicks = (decimal)(CopyProgressBar.Maximum) / CopyProgressBar.Value * span.Ticks;

        var remainingTime = new TimeSpan(Convert.ToInt64(completeTimeTicks) - span.Ticks);

        var speed = bytes * 1000m / (decimal)(span.TotalMilliseconds);

        var speedText = $" ({Helper.FormatBytes(Convert.ToInt64(speed))}/s)";

        if (remainingTime.Hours > 0)
        {
            var minutes = remainingTime.Minutes;

            if (remainingTime.Seconds > 30)
            {
                minutes++;
            }

            RemaingLabel.Text = $"est. {remainingTime.Hours} hours, {minutes} minutes remaining{speedText}";
        }
        else if (remainingTime.Minutes > 0)
        {
            RemaingLabel.Text = $"est. {remainingTime.Minutes} minutes, {remainingTime.Seconds} seconds remaining{speedText}";
        }
        else
        {
            RemaingLabel.Text = $"est. {remainingTime.Seconds} seconds remaining{speedText}";
        }
    }

    private void OnAbortButtonClick(object sender, EventArgs e)
        => _copier.Abort();

    private void OnAbortButtonMouseEnter(object sender, EventArgs e)
    {
        if (AbortButton.Visible)
        {
            this.UseWaitCursor = false;
        }
    }

    private void OnAbortButtonMouseLeave(object sender, EventArgs e)
    {
        if (AbortButton.Visible)
        {
            this.UseWaitCursor = true;
        }
    }

    private void FormatBytes()
        => SizeLabel.Text = SourceListBox.Items.Count > 0
            ? Helper.FormatBytes(Helper.CalculateBytes(this.SourceListBoxItems, WithSubFoldersCheckBox.Checked))
            : "0 Byte";

    private void OnWithSubFoldersCheckBoxCheckedChanged(object sender, EventArgs e)
        => this.FormatBytes();

    private void OnImportListButtonClick(object sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog();

        ofd.CheckFileExists = true;
        ofd.Filter = "Text files|*.lst;*.txt";
        ofd.Multiselect = false;
        ofd.RestoreDirectory = true;

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                using var sr = new StreamReader(ofd.FileName, Encoding.GetEncoding(1252));

                while (sr.EndOfStream == false)
                {
                    var itemParts = sr.ReadLine().Split(';');

                    if (itemParts.Length != 2)
                    {
                        continue;
                    }
                    else if (Directory.Exists(itemParts[0]))
                    {
                        this.AddFolder(new DirectoryInfo(itemParts[1]), itemParts[0]);
                    }
                    else if (File.Exists(itemParts[0]))
                    {
                        this.AddFiles(new DirectoryInfo(itemParts[1]), itemParts[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageBox(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void OnSourceListBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            this.OnRemoveEntryButtonClick(sender, e);
        }
    }
}