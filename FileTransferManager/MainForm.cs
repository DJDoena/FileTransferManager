using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using DoenaSoft.AbstractionLayer.IOServices;
using DoenaSoft.AbstractionLayer.UIServices;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace DoenaSoft.FileTransferManager;

internal partial class MainForm : Form, IMainWindowView
{
    private readonly MainWindowController _controller;

    private Copier _copier;

    private readonly IIOServices _ioServices;

    private readonly IUIServices _uiServices;

    private IEnumerable<CopyItem> SourceListBoxItems
        => SourceListBox.Items.Cast<CopyItem>().ToList();

    int IMainWindowView.ProgressBarMax
    {
        get => CopyProgressBar.Maximum;
        set => CopyProgressBar.Maximum = value;
    }

    public MainForm(IIOServices iOServices
        , IUIServices formUIServices)
    {
        _ioServices = iOServices;
        _uiServices = formUIServices;

        _controller = new MainWindowController(_ioServices, _uiServices);

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
        var options = new FolderBrowserDialogOptions()
        {
            ShowNewFolderButton = false,
            Description = "Select Source Folder to Copy",
            SelectedPath = Directory.Exists(_controller.SelectedSourcePath?.FullName)
                ? _controller.SelectedSourcePath.FullName
                : null,
            RootFolder = Environment.SpecialFolder.MyComputer
        };

        if (_uiServices.ShowFolderBrowserDialog(options, out var selectedFolder))
        {
            this.TryAddFolder(selectedFolder);
        }
    }

    private void TryAddFolder(string selectedPath)
    {
        if (_controller.TryCreateCopyItemFromFolder(selectedPath, out var item))
        {
            this.AddFolder(item.TargetFolder, item.SourceFolder);
        }
        else
        {
            this.ShowNotOnLetterDriveErrror(selectedPath);
        }
    }

    private void ShowNotOnLetterDriveErrror(string selectedPath)
        => this.ShowMessageBox($"'{selectedPath}' is not valid.{Environment.NewLine}Please choose file / folder on a letter drive.", "Invalid", MessageButtons.OK, MessageIcon.Warning);

    // Drive checks and target dialog moved to MainController

    private void AddFolder(IFolderInfo targetFolder, IFolderInfo sourceFolder)
    {
        SourceListBox.Items.Add(new CopyItem(sourceFolder, targetFolder));

        this.FormatBytes();
    }
    // Target folder selection is handled by MainController

    private void OnAddFileButtonClick(object sender, EventArgs e)
    {
        var options = new OpenFileDialogOptions()
        {
            CheckFileExists = true,
            Title = "Select File(s) to Copy",
            InitialFolder = Directory.Exists(_controller.SelectedSourcePath?.FullName)
                ? _controller.SelectedSourcePath.FullName
                : null,
            RestoreFolder = true,
        };

        if (_uiServices.ShowOpenFileDialog(options, out string[] fileNames))
        {
            this.TryAddFiles(fileNames);
        }
    }

    private void TryAddFiles(string[] fileNames)
    {
        var fileName = fileNames.First();

        if (_controller.TryCreateCopyItemsFromFiles(fileNames, out var items))
        {
            foreach (var item in items)
            {
                SourceListBox.Items.Add(item);
            }

            this.FormatBytes();
        }
        else if (!MainWindowController.IsOnLetterDrive(fileName))
        {
            this.ShowNotOnLetterDriveErrror(fileName);
        }
    }

    private void AddFiles(IFolderInfo targetFolder, params string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            var sourceFile = _ioServices.GetFile(fileName);

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

        foreach (var sourceItem in this.SourceListBoxItems)
        {
            if (sourceItem.SourceFolder?.Exists == true)
            {
                var folderItems = Helper.AddFolder(sourceItem, WithSubFoldersCheckBox.Checked);

                targetItems.AddRange(folderItems);
            }
            else if (sourceItem.SourceFile?.Exists == true)
            {
                targetItems.Add(sourceItem);
            }
            else
            {
                this.ShowMessageBox($"Something is weird about{Environment.NewLine}{sourceItem}", "?!?", MessageButtons.OK, MessageIcon.Error);
            }
        }

        var (allBytes, divider) = Helper.CheckDriveSize(targetItems, this, _ioServices);

        if (allBytes <= 0)
        {
            return;
        }

        CopyProgressBar.Maximum = (int)(allBytes / divider);

        this.SwitchUI(false);

        targetItems.Sort((left, right) => left.SourceFile.FullName.CompareTo(right.SourceFile.FullName));

        var overwrite = (OverwriteMode)Enum.Parse(typeof(OverwriteMode), OverwriteComboBox.Text);

        _copier = new Copier(targetItems.AsReadOnly(), overwrite, divider, this, _ioServices);

        _copier.CopyFinished += this.OnCopyFinished;

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

    private void OnCopyFinished(object sender, EventArgs e)
    {
        _copier.CopyFinished -= this.OnCopyFinished;

        _copier = null;

        this.SwitchUI(true);
    }

    public Result ShowMessageBox(string message, string title, MessageButtons buttons, MessageIcon icon)
    {
        var func = new Func<Result>(() => _uiServices.ShowMessageBox(message, title, buttons, icon));

        var result = this.InvokeRequired
            ? (Result)this.Invoke(func)
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

    public void UpdateProgressBar(long bytes, long divider, DateTime start)
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
                this.SetRemaingLabelText(bytes, start);
            }
        }

        CopyProgressBar.Update();
        CopyProgressBar.Refresh();
    }

    private void SetRemaingLabelText(long bytes, DateTime start)
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
                        this.AddFolder(_ioServices.GetFolder(itemParts[1]), _ioServices.GetFolder(itemParts[0]));
                    }
                    else if (File.Exists(itemParts[0]))
                    {
                        this.AddFiles(_ioServices.GetFolder(itemParts[1]), itemParts[0]);
                    }
                }
            }
            catch (Exception ex)
            {
                this.ShowMessageBox(ex.Message, "Error", MessageButtons.OK, MessageIcon.Error);
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