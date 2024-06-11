using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace DoenaSoft.FileTransferManager;

internal partial class MainForm : Form
{
    private string _selectedTargetPath;

    private long _divider;

    private Thread _copyThread;

    private event EventHandler CopyFinished;

    private System.Windows.Forms.Timer _abortTimer;

    public MainForm()
    {
        _selectedTargetPath = null;

        _divider = 1;

        _copyThread = null;

        this.InitializeComponent();

        this.Icon = Resources.djdsoft;
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

    private void OnAddFolderButtonClick(object sender
        , EventArgs e)
    {
        using var sourceDialog = new FolderBrowserDialog();

        sourceDialog.ShowNewFolderButton = false;
        sourceDialog.Description = "Select Source Folder to Copy";
        sourceDialog.RootFolder = Environment.SpecialFolder.MyComputer;

        if (sourceDialog.ShowDialog() == DialogResult.OK
            && this.ShowTargetDialog(out var targetFolder))
        {
            SourceListBox.Items.Add(new CopyItem(new DirectoryInfo(sourceDialog.SelectedPath), targetFolder));

            this.FormatBytes();
        }
    }

    private bool ShowTargetDialog(out DirectoryInfo targetFolder)
    {
        using var targetDialog = new FolderBrowserDialog();

        targetDialog.ShowNewFolderButton = true;
        targetDialog.Description = "Select Target Folder to Copy";
        targetDialog.RootFolder = Environment.SpecialFolder.MyComputer;

        if (_selectedTargetPath != null && Directory.Exists(_selectedTargetPath))
        {
            targetDialog.SelectedPath = _selectedTargetPath;
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
        sourceDialog.RestoreDirectory = true;
        sourceDialog.Title = "Select File(s) to Copy";

        if (sourceDialog.ShowDialog() == DialogResult.OK
            && this.ShowTargetDialog(out var targetFolder))
        {
            foreach (var fileName in sourceDialog.FileNames)
            {
                SourceListBox.Items.Add(new CopyItem(new FileInfo(fileName), targetFolder));
            }

            this.FormatBytes();
        }
    }

    private void OnCopyButtonClick(object sender, EventArgs e)
    {
        if (TaskbarManager.IsPlatformSupported)
        {
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
            TaskbarManager.Instance.SetProgressValue(0, ProgressBar.Maximum);
        }

        ProgressBar.Value = 0;

        var items = new List<CopyItem>();
        foreach (CopyItem item in SourceListBox.Items)
        {
            if (item.SourceFolder?.Exists == true)
            {
                var option = WithSubFoldersCheckBox.Checked
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                var files = item.SourceFolder.GetFiles("*.*", option);

                foreach (var file in files)
                {
                    var fileFolderName = file.DirectoryName;

                    CopyItem newItem;
                    if (!string.Equals(fileFolderName, item.SourceFolder.FullName, StringComparison.InvariantCulture))
                    {
                        var relativeSourcePath = fileFolderName.Substring(item.SourceFolder.FullName.Length + 1);

                        var targetPath = new DirectoryInfo(Path.Combine(item.TargetFolder.FullName, relativeSourcePath));

                        newItem = new CopyItem(file, targetPath);
                    }
                    else
                    {
                        newItem = new CopyItem(file, item.TargetFolder);
                    }

                    items.Add(newItem);
                }
            }
            else if (item.SourceFile?.Exists == true)
            {
                items.Add(item);
            }
            else
            {
                MessageBox.Show("Something is weird about\n" + item, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }
        }

        var allBytes = this.CheckDriveSize(items);

        if (allBytes <= 0)
        {
            return;
        }

        ProgressBar.Maximum = (int)(allBytes / _divider);

        CopyFinished += this.OnMainFormCopyFinished;

        this.SwitchUI(false);

        _copyThread = new Thread(new ParameterizedThreadStart(this.ThreadRun))
        {
            IsBackground = true,
        };

        _copyThread.Start(new object[] { items, OverwriteComboBox.Text });
    }

    private long CheckDriveSize(List<CopyItem> items)
    {
        var driveGroups = items
            .GroupBy(item => item.TargetFolder.Root.Name.Substring(0, 1))
            .ToList();

        var bytes = new Dictionary<string, long>(driveGroups.Count);

        foreach (var driveGroup in driveGroups)
        {
            long driveBytes = 0;

            foreach (var item in driveGroup)
            {
                driveBytes += item.SourceFile.Length;
            }

            var drive = new DriveInfo(driveGroup.Key);

            if (drive.AvailableFreeSpace <= driveBytes)
            {
                MessageBox.Show($"Target is Full!{Environment.NewLine}Available: {FormatBytes(drive.AvailableFreeSpace)}{Environment.NewLine}Needed: {FormatBytes(driveBytes)}"
                    , "Target Full", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return -1;
            }

            bytes.Add(driveGroup.Key, driveBytes);
        }

        var allBytes = bytes.Sum(kvp => kvp.Value);

        if (allBytes >= Math.Pow(2, 40) * 2)
        {
            _divider = 1000000;
        }
        else if (allBytes >= Math.Pow(2, 30) * 2)
        {
            _divider = 1000;
        }
        else
        {
            _divider = 1;
        }

        return allBytes;
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

        CopyFinished -= this.OnMainFormCopyFinished;
    }

    private void ThreadRun(object parameter)
    {
        var threadAbort = false;

        var start = DateTime.Now;

        try
        {
            var parameters = (object[])parameter;

            var items = (List<CopyItem>)parameters[0];

            items.Sort((left, right) => left.SourceFile.FullName.CompareTo(right.SourceFile.FullName));

            var overwrite = (string)parameters[1];

            long bytes = 0;

            foreach (var item in items)
            {
                if (!item.TargetFolder.Exists)
                {
                    item.TargetFolder.Create();
                }

                var targetFile = new FileInfo(Path.Combine(item.TargetFolder.FullName, item.SourceFile.Name));

                var result = DialogResult.Yes;

                if (targetFile.Exists)
                {
                    result = DialogResult.No;

                    if (overwrite == "ask")
                    {
                        var startTicks = DateTime.Now.Ticks;

                        result = MessageBox.Show($"Overwrite \"{targetFile.FullName}\"\nfrom \"{item.SourceFile.FullName}\"?", "Overwrite?"
                            , MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                        var endTicks = DateTime.Now.Ticks;

                        var span = new TimeSpan(endTicks - startTicks);

                        start = start.Add(span);
                    }
                    else if (overwrite == "always")
                    {
                        result = DialogResult.Yes;
                    }
                }

                if (result == DialogResult.Cancel)
                {
                    return;
                }
                else if (result == DialogResult.No)
                {
                    this.Invoke(new Action(() =>
                    {
                        ProgressBar.Maximum -= (int)(item.SourceFile.Length / _divider);

                        this.UpdateProgressBar(bytes, start);

                        this.Refresh();
                    }));

                    continue;
                }
                else if (result == DialogResult.Yes)
                {
                    try
                    {
                        File.Copy(item.SourceFile.FullName, targetFile.FullName, true);
                    }
                    catch (IOException ioEx)
                    {
                        long startTicks = DateTime.Now.Ticks;

                        if (MessageBox.Show(ioEx.Message + "\nContinue?", "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            long endTicks = DateTime.Now.Ticks;

                            TimeSpan span = new TimeSpan(endTicks - startTicks);

                            start = start.Add(span);

                            continue;
                        }
                        else
                        {
                            return;
                        }
                    }

                    bytes += item.SourceFile.Length;

                    this.Invoke(new Action(() =>
                    {
                        this.UpdateProgressBar(bytes, start);

                        this.Refresh();
                    }));
                }
            }
        }
        catch (IOException ioEx)
        {
            this.Invoke(new Action(() =>
            {
                MessageBox.Show(ioEx.Message, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }));

            return;
        }
        catch (ThreadAbortException)
        {
            this.Invoke(new Action(() =>
            {
                MessageBox.Show("The copy process was cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }));

            threadAbort = true;

            return;
        }
        catch (Exception ex)
        {
            this.Invoke(new Action(() =>
            {
                MessageBox.Show(ex.Message, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }));

            return;
        }
        finally
        {
            if (threadAbort == false)
            {
                this.Invoke(new Action(() =>
                {
                    this.UpdateProgressBar(-1, start);

                    CopyFinished?.Invoke(this, EventArgs.Empty);
                }));
            }
        }
    }

    private void OnMainFormLoad(object sender, EventArgs e)
        => OverwriteComboBox.SelectedIndex = 0;

    private void OnClearListButtonClick(object sender
        , EventArgs e)
    {
        SourceListBox.Items.Clear();

        this.FormatBytes();
    }

    private void UpdateProgressBar(long bytes, DateTime start)
    {
        if (bytes == -1)
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            }

            ProgressBar.Value = ProgressBar.Maximum;

            RemaingLabel.Text = ".....";
        }
        else
        {
            var now = DateTime.Now;

            var value = (int)(bytes / _divider);

            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressValue(value, ProgressBar.Maximum);
            }

            ProgressBar.Value = value;

            if (ProgressBar.Value != 0)
            {
                var span = now.Subtract(start);

                var completeTimeTicks = (decimal)(ProgressBar.Maximum) / ProgressBar.Value * span.Ticks;

                var remainingTime = new TimeSpan(Convert.ToInt64(completeTimeTicks) - span.Ticks);

                var speed = bytes * 1000m / (decimal)(span.TotalMilliseconds);

                var speedText = $" ({FormatBytes(Convert.ToInt64(speed))}/s)";

                if (remainingTime.Hours > 0)
                {
                    int minutes = remainingTime.Minutes;

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
        }

        ProgressBar.Update();
        ProgressBar.Refresh();
    }

    private void OnAbortButtonClick(object sender, EventArgs e)
    {
        if (_copyThread != null)
        {
            _abortTimer = new System.Windows.Forms.Timer
            {
                Interval = 100,
            };

            _abortTimer.Tick += new EventHandler(this.AbortTimerTick);
            _abortTimer.Start();

            _copyThread.Abort();
        }
    }

    private void AbortTimerTick(object sender, EventArgs e)
    {
        if (_copyThread.IsAlive == false)
        {
            _abortTimer.Stop();

            _copyThread = null;

            this.UpdateProgressBar(-1, DateTime.Now);

            CopyFinished?.Invoke(this, EventArgs.Empty);
        }
    }

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
            ? FormatBytes(this.CalculateBytes())
            : "0 Byte";

    private long CalculateBytes()
    {
        long bytes = 0;

        foreach (CopyItem item in SourceListBox.Items)
        {
            if (item.SourceFolder?.Exists == true)
            {
                var option = WithSubFoldersCheckBox.Checked
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                var files = item.SourceFolder.GetFiles("*.*", option);

                foreach (var file in files)
                {
                    bytes += file.Length;
                }
            }
            else if (item.SourceFile?.Exists == true)
            {
                bytes += item.SourceFile.Length;
            }
        }

        return (bytes);
    }

    private static string FormatBytes(long bytes)
    {
        decimal roundBytes;
        string bytesPower;
        if (bytes / Math.Pow(2, 40) > 1)
        {
            roundBytes = Math.Round(bytes / (decimal)Math.Pow(2, 40), 1, MidpointRounding.AwayFromZero);
            bytesPower = " TByte";
        }
        if (bytes / Math.Pow(2, 30) > 1)
        {
            roundBytes = Math.Round(bytes / (decimal)(Math.Pow(2, 30)), 1, MidpointRounding.AwayFromZero);
            bytesPower = " GByte";
        }
        else if (bytes / Math.Pow(2, 20) > 1)
        {
            roundBytes = Math.Round(bytes / (decimal)(Math.Pow(2, 20)), 1, MidpointRounding.AwayFromZero);
            bytesPower = " MByte";
        }
        else if (bytes / Math.Pow(2, 10) > 1)
        {
            roundBytes = Math.Round(bytes / (decimal)(Math.Pow(2, 10)), 1, MidpointRounding.AwayFromZero);
            bytesPower = " KByte";
        }
        else
        {
            roundBytes = bytes;
            bytesPower = " Byte";
        }

        return roundBytes + bytesPower;
    }

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
            using var sr = new StreamReader(ofd.FileName, Encoding.GetEncoding(1252));

            while (sr.EndOfStream == false)
            {
                string file = sr.ReadLine();

                if (File.Exists(file))
                {
                    SourceListBox.Items.Add(file);

                    this.FormatBytes();
                }
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

    private sealed class CopyItem
    {
        internal DirectoryInfo SourceFolder { get; }

        internal FileInfo SourceFile { get; }

        internal DirectoryInfo TargetFolder { get; }

        internal CopyItem(DirectoryInfo sourceFolder, DirectoryInfo targetFolder)
        {
            this.SourceFolder = sourceFolder;
            this.SourceFile = null;
            this.TargetFolder = targetFolder;
        }

        internal CopyItem(FileInfo sourceFile, DirectoryInfo targetFolder)
        {
            this.SourceFolder = null;
            this.SourceFile = sourceFile;
            this.TargetFolder = targetFolder;
        }

        public override string ToString()
        {
            var source = this.SourceFolder != null
                ? this.SourceFolder.FullName
                : this.SourceFile.FullName;

            return $"{source} --> {this.TargetFolder.FullName}";
        }
    }
}