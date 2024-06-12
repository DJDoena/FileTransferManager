using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DoenaSoft.FileTransferManager;

internal static class Helper
{
    internal static IEnumerable<CopyItem> AddFolder(CopyItem sourceFolderItem, bool withSubFolders)
    {
        var option = withSubFolders
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var files = sourceFolderItem.SourceFolder.GetFiles("*.*", option);

        var result = files
            .Select(file => AddFolderFile(sourceFolderItem, file))
            .ToList();

        return result;
    }

    private static CopyItem AddFolderFile(CopyItem sourceFolderItem, FileInfo sourceFile)
    {
        var sourceFolderBase = sourceFolderItem.SourceFolder.Parent; //SourceFolder can never be the drive letter

        var relativeSourcePath = sourceFile.Directory.FullName.Substring(sourceFolderBase.FullName.Length);

        var targetPath = new DirectoryInfo(Path.Combine(sourceFolderItem.TargetFolder.FullName, relativeSourcePath));

        return new CopyItem(sourceFile, targetPath);
    }

    internal static long CalculateBytes(IEnumerable<CopyItem> copyItems, bool withSubFolders)
    {
        try
        {
            long bytes = 0;

            foreach (var item in copyItems)
            {
                if (item.SourceFolder?.Exists == true)
                {
                    var option = withSubFolders
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

            return bytes;
        }
        catch
        {
            return -1;
        }
    }

    internal static string FormatBytes(long bytes)
    {
        decimal? roundBytes;
        string bytesPower;
        if (bytes < 0)
        {
            roundBytes = null;
            bytesPower = "? Byte";
        }
        else if (bytes / Math.Pow(2, 40) > 1)
        {
            roundBytes = Math.Round(bytes / (decimal)Math.Pow(2, 40), 1, MidpointRounding.AwayFromZero);
            bytesPower = " TiB";
        }
        else if (bytes / Math.Pow(2, 30) > 1)
        {
            roundBytes = Math.Round(bytes / (decimal)(Math.Pow(2, 30)), 1, MidpointRounding.AwayFromZero);
            bytesPower = " GiB";
        }
        else if (bytes / Math.Pow(2, 20) > 1)
        {
            roundBytes = Math.Round(bytes / (decimal)(Math.Pow(2, 20)), 1, MidpointRounding.AwayFromZero);
            bytesPower = " MiB";
        }
        else if (bytes / Math.Pow(2, 10) > 1)
        {
            roundBytes = Math.Round(bytes / (decimal)(Math.Pow(2, 10)), 1, MidpointRounding.AwayFromZero);
            bytesPower = " KiB";
        }
        else
        {
            roundBytes = bytes;
            bytesPower = " Byte";
        }

        return $"{roundBytes}{bytesPower}";
    }

    internal static (long bytes, long divider) CheckDriveSize(IEnumerable<CopyItem> items, IView view)
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
                view.ShowMessageBox($"Target is Full!{Environment.NewLine}Available: {Helper.FormatBytes(drive.AvailableFreeSpace)}{Environment.NewLine}Needed: {Helper.FormatBytes(driveBytes)}"
                    , "Target Full", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return (-1, 1);
            }

            bytes.Add(driveGroup.Key, driveBytes);
        }

        var allBytes = bytes.Sum(kvp => kvp.Value);

        long divider;
        if (allBytes >= Math.Pow(2, 40) * 2)
        {
            divider = 1000000;
        }
        else if (allBytes >= Math.Pow(2, 30) * 2)
        {
            divider = 1000;
        }
        else
        {
            divider = 1;
        }

        return (allBytes, divider);
    }
}