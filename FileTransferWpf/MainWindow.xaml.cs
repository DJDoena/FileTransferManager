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
using DoenaSoft.FileTransferManager;

namespace DoenaSoft.FileTransferManager;

public partial class MainWindow : Window, IView
{
    private readonly IIOServices _ioServices;
    private readonly IUIServices _uiServices;
    private readonly MainController _controller;
    private Copier _copier;

    public MainWindow(IIOServices ioServices, IUIServices uiServices)
    {
        _ioServices = ioServices;
        _uiServices = uiServices;
        _controller = new MainController(_ioServices, _uiServices);

        this.Icon = ToImageSource(FileTransferManager.Resources.djdsoft);

        InitializeComponent();
        // ensure UI parity with WinForms: add overwrite combo if XAML doesn't contain one
        try
        {
            var root = this.Content as Grid;

            if (root != null)
            {
                var topPanel = root.Children.OfType<StackPanel>().FirstOrDefault();

                if (topPanel != null && topPanel.Children.OfType<ComboBox>().All(c => c.Name != "OverwriteComboBox"))
                {
                    var combo = new ComboBox() { Name = "OverwriteComboBox", Width = 120, Margin = new Thickness(8, 0, 0, 0), SelectedIndex = 0 };
                    combo.Items.Add(new ComboBoxItem() { Content = "Ask" });
                    combo.Items.Add(new ComboBoxItem() { Content = "Always" });
                    combo.Items.Add(new ComboBoxItem() { Content = "Never" });

                    // Register the name so FindName works
                    try { this.RegisterName(combo.Name, combo); } catch { }

                    topPanel.Children.Add(combo);
                }
            }
        }
        catch
        {
            // ignore any UI composition errors
        }

        this.Title += " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
    }

    private IEnumerable<CopyItem> SourceListBoxItems => SourceListBox.Items.Cast<CopyItem>().ToList();

    public bool InvokeRequired => false;

    public int ProgressBarMax
    {
        get => (int)CopyProgressBar.Maximum;
        set => CopyProgressBar.Maximum = value;
    }

    public object Invoke(Delegate method)
    {
        return this.Dispatcher.Invoke(method);
    }

    public Result ShowMessageBox(string message, string title, Buttons buttons, Icon icon)
    {
        return _uiServices.ShowMessageBox(message, title, buttons, icon);
    }

    public void UpdateProgressBar(long bytes, long divider, DateTime start)
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
        // WPF doesn't have a direct Refresh; force layout update on the dispatcher
        this.Dispatcher.Invoke(() =>
        {
            this.InvalidateVisual();
            this.UpdateLayout();
        });
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
                FormatBytes();
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

                FormatBytes();
            }
        }
    }

    private void OnRemoveEntryClick(object sender, RoutedEventArgs e)
    {
        if (SourceListBox.SelectedIndex >= 0)
        {
            var idx = SourceListBox.SelectedIndex;
            SourceListBox.Items.RemoveAt(idx);
            if (SourceListBox.Items.Count > idx)
                SourceListBox.SelectedIndex = idx;
            else if (SourceListBox.Items.Count > 0)
                SourceListBox.SelectedIndex = SourceListBox.Items.Count - 1;

            FormatBytes();
        }
    }

    private void OnClearListClick(object sender, RoutedEventArgs e)
    {
        SourceListBox.Items.Clear();
        FormatBytes();
    }

    private void OnWithSubFoldersCheckedChanged(object sender, RoutedEventArgs e)
    {
        FormatBytes();
    }

    private void OnCopyClick(object sender, RoutedEventArgs e)
    {
        var targetItems = new List<CopyItem>();

        foreach (var sourceItem in SourceListBoxItems)
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
                ShowMessageBox($"Something is weird about\r\n{sourceItem}", "?!?", Buttons.OK, DoenaSoft.AbstractionLayer.UIServices.Icon.Error);
            }
        }

        var (allBytes, divider) = Helper.CheckDriveSize(targetItems, this, _ioServices);

        if (allBytes <= 0)
            return;

        CopyProgressBar.Maximum = (int)(allBytes / divider);

        targetItems.Sort((l, r) => l.SourceFile.FullName.CompareTo(r.SourceFile.FullName));

        // determine overwrite mode from UI (OverwriteComboBox)
        OverwriteMode overwrite = OverwriteMode.Ask;

        try
        {
            ComboBox overwriteCb = this.FindName("OverwriteComboBox") as ComboBox;

            if (overwriteCb == null)
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

        _copier.CopyFinished += OnCopyFinished;

        _copier.Start();
    }

    private void OnAbortClick(object sender, RoutedEventArgs e)
    {
        _copier?.Abort();
    }

    private void OnCopyFinished(object sender, EventArgs e)
    {
        _copier.CopyFinished -= OnCopyFinished;
        _copier = null;
    }

    private void FormatBytes()
    {
        SizeLabel.Content = SourceListBox.Items.Count > 0
            ? Helper.FormatBytes(Helper.CalculateBytes(SourceListBoxItems, WithSubFoldersCheckBox.IsChecked == true))
            : "0 Byte";
    }

    private static ImageSource ToImageSource(System.Drawing.Icon icon)
    {
        ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        return imageSource;
    }
}
