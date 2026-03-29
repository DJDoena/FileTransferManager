using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DoenaSoft.AbstractionLayer.IOServices;
using DoenaSoft.AbstractionLayer.UIServices;

namespace DoenaSoft.FileTransferManager;

public partial class MainWindow : Window, IMainWindowView
{
    private readonly IIOServices _ioServices;

    private readonly IUIServices _uiServices;

    private readonly MainWindowController _controller;

    private Copier _copier;

    public MainWindow(IIOServices ioServices
        , IUIServices uiServices)
    {
        _ioServices = ioServices;
        _uiServices = uiServices;

        _controller = new MainWindowController(_ioServices, _uiServices);

        this.InitializeComponent();

        this.Icon = ToImageSource(FileTransferManager.Resources.djdsoft);

        this.Title += " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
    }

    private IEnumerable<CopyItem> SourceListBoxItems
        => [.. SourceListBox.Items.Cast<CopyItem>()];

    public bool InvokeRequired
        => !this.Dispatcher.CheckAccess();

    public int ProgressBarMax
    {
        get => (int)CopyProgressBar.Maximum;
        set => CopyProgressBar.Maximum = value;
    }

    public object Invoke(Delegate method)
    {
        // If we're already on the UI thread invoke directly to avoid unnecessary
        // cross-thread marshalling. Otherwise dispatch to the UI thread.
        var result = this.Dispatcher.CheckAccess()
            ? method.DynamicInvoke()
            : this.Dispatcher.Invoke(method);

        return result;
    }

    public Result ShowMessageBox(string message
        , string title
        , MessageButtons buttons, MessageIcon icon)
        => _uiServices.ShowMessageBox(message, title, buttons, icon);

    public void UpdateProgressBar(long bytes
        , long divider
        , DateTime start)
    {
        if (bytes == -1)
        {
            CopyProgressBar.Value = CopyProgressBar.Maximum;

            RemainingLabel.Content = ".....";

            return;
        }

        var value = (int)(bytes / divider);

        CopyProgressBar.Value = value;
    }

    public void Refresh()
    {
        Action refresh = () =>
        {
            this.InvalidateVisual();
            this.UpdateLayout();
        };

        this.Invoke(refresh);
    }

    private void OnAddFolderClick(object sender, RoutedEventArgs e)
    {
        var options = new FolderBrowserDialogOptions()
        {
            ShowNewFolderButton = false,
            Description = "Select Source Folder to Copy",
            SelectedPath = System.IO.Directory.Exists(_controller.SelectedSourcePath?.FullName)
                ? _controller.SelectedSourcePath.FullName
                : null,
            RootFolder = Environment.SpecialFolder.MyComputer
        };

        if (_uiServices.ShowFolderBrowserDialog(options, out var selected))
        {
            if (_controller.TryCreateCopyItemFromFolder(selected, out var item))
            {
                SourceListBox.Items.Add(item);
                this.FormatBytes();
            }
        }
    }

    private void OnAddFileClick(object sender, RoutedEventArgs e)
    {
        var options = new OpenFileDialogOptions()
        {
            CheckFileExists = true,
            Title = "Select File(s) to Copy",
            InitialFolder = System.IO.Directory.Exists(_controller.SelectedSourcePath?.FullName)
                ? _controller.SelectedSourcePath.FullName
                : null,
            RestoreFolder = true,
        };

        if (_uiServices.ShowOpenFileDialog(options, out string[] files))
        {
            if (_controller.TryCreateCopyItemsFromFiles(files, out var items))
            {
                foreach (var it in items)
                {
                    SourceListBox.Items.Add(it);
                }

                this.FormatBytes();
            }
        }
    }

    private void OnRemoveEntryClick(object sender, RoutedEventArgs e)
    {
        if (SourceListBox.SelectedIndex >= 0)
        {
            var index = SourceListBox.SelectedIndex;

            SourceListBox.Items.RemoveAt(index);

            if (SourceListBox.Items.Count > index)
            {
                SourceListBox.SelectedIndex = index;
            }
            else if (SourceListBox.Items.Count > 0)
            {
                SourceListBox.SelectedIndex = SourceListBox.Items.Count - 1;
            }

            this.FormatBytes();
        }
    }

    private void OnClearListClick(object sender, RoutedEventArgs e)
    {
        SourceListBox.Items.Clear();

        this.FormatBytes();
    }

    private void OnWithSubFoldersCheckedChanged(object sender, RoutedEventArgs e)
    {
        this.FormatBytes();
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        var targetItems = new List<CopyItem>();

        foreach (var sourceItem in this.SourceListBoxItems)
        {
            if (sourceItem.SourceFolder?.Exists == true)
            {
                var folderItems = Helper.AddFolder(sourceItem, WithSubFoldersCheckBox.IsChecked == true);

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
            return;

        CopyProgressBar.Maximum = (int)(allBytes / divider);

        targetItems.Sort((l, r) => l.SourceFile.FullName.CompareTo(r.SourceFile.FullName));

        // determine overwrite mode from UI (OverwriteComboBox)
        var overwrite = OverwriteMode.Ask;

        try
        {
            if (this.FindName("OverwriteComboBox") is not ComboBox overwriteCb)
            {
                var root = this.Content as Grid;
                overwriteCb = root?.Children.OfType<StackPanel>().FirstOrDefault()?.Children.OfType<ComboBox>().FirstOrDefault(c => c.Name == "OverwriteComboBox");
            }

            if (overwriteCb != null && overwriteCb.SelectedItem != null)
            {
                string text = null;
                if (overwriteCb.SelectedItem is ComboBoxItem cbi)
                    text = cbi.Content?.ToString();
                else
                    text = overwriteCb.SelectedItem.ToString();

                if (!string.IsNullOrEmpty(text))
                {
                    overwrite = (OverwriteMode)Enum.Parse(typeof(OverwriteMode), text);
                }
            }
        }
        catch
        {
            overwrite = OverwriteMode.Ask;
        }

        _copier = new Copier(targetItems.AsReadOnly(), overwrite, divider, this, _ioServices);

        _copier.CopyFinished += this.OnCopyFinished;

        _copier.Start();
    }

    private void OnAbortClick(object sender, RoutedEventArgs e)
    {
        _copier?.Abort();
    }

    private void OnCopyFinished(object sender, EventArgs e)
    {
        _copier.CopyFinished -= this.OnCopyFinished;
        _copier = null;
    }

    private void FormatBytes()
    {
        SizeLabel.Content = SourceListBox.Items.Count > 0
            ? Helper.FormatBytes(Helper.CalculateBytes(this.SourceListBoxItems, WithSubFoldersCheckBox.IsChecked == true))
            : "0 Byte";
    }

    private static ImageSource ToImageSource(System.Drawing.Icon icon)
    {
        var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        // Freeze so the ImageSource is immutable and can be used across threads
        bmpSrc.Freeze();

        return bmpSrc;
    }
}
