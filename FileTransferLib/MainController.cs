using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using DoenaSoft.AbstractionLayer.IOServices;
using DoenaSoft.AbstractionLayer.UIServices;

namespace DoenaSoft.FileTransferManager;

public sealed class MainController
{
    private readonly IIOServices _ioServices;

    private readonly IUIServices _uiServices;

    private IFolderInfo _selectedSourcePath;

    private IFolderInfo _selectedTargetPath;

    public MainController(IIOServices ioServices, IUIServices uiServices)
    {
        _ioServices = ioServices ?? throw new ArgumentNullException(nameof(ioServices));
        _uiServices = uiServices ?? throw new ArgumentNullException(nameof(uiServices));
    }

    public IFolderInfo SelectedSourcePath => _selectedSourcePath;

    public IFolderInfo SelectedTargetPath => _selectedTargetPath;

    public static bool IsOnLetterDrive(string selectedPath)
    {
        return Regex.IsMatch(selectedPath, @"^[a-zA-Z]:\\", RegexOptions.IgnoreCase);
    }

    public bool TryCreateCopyItemFromFolder(string selectedPath, out CopyItem item)
    {
        item = null;

        if (!IsOnLetterDrive(selectedPath))
        {
            return false;
        }

        var selectedFolder = _ioServices.GetFolder(selectedPath);

        if (selectedFolder.FullName.Equals(selectedFolder.Root.FullName))
        {
            return false;
        }

        if (!this.ShowTargetDialog(out var targetFolder))
        {
            return false;
        }

        item = new CopyItem(selectedFolder, targetFolder);

        _selectedSourcePath = selectedFolder;

        return true;
    }

    public bool TryCreateCopyItemsFromFiles(string[] fileNames, out List<CopyItem> items)
    {
        items = null;

        if (fileNames == null || fileNames.Length == 0)
        {
            return false;
        }

        var first = fileNames[0];

        if (!IsOnLetterDrive(first))
        {
            return false;
        }

        if (!this.ShowTargetDialog(out var targetFolder))
        {
            return false;
        }

        items = new List<CopyItem>(fileNames.Length);

        foreach (var fileName in fileNames)
        {
            var sourceFile = _ioServices.GetFile(fileName);

            items.Add(new CopyItem(sourceFile, targetFolder));
        }

        _selectedSourcePath = _ioServices.GetFile(first).Folder;

        return true;
    }

    private bool ShowTargetDialog(out IFolderInfo targetFolder)
    {
        var options = new FolderBrowserDialogOptions()
        {
            ShowNewFolderButton = true,
            Description = "Select Target Folder to Copy",
            SelectedPath = Directory.Exists(_selectedTargetPath?.FullName)
                ? _selectedTargetPath.FullName
                : null,
            RootFolder = Environment.SpecialFolder.MyComputer
        };

        if (_uiServices.ShowFolderBrowserDialog(options, out var selectedFolder))
        {
            targetFolder = _ioServices.GetFolder(selectedFolder);

            _selectedTargetPath = targetFolder;

            return true;
        }

        targetFolder = null;

        return false;
    }
}
