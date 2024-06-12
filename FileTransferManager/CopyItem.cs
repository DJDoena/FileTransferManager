using System.IO;

namespace DoenaSoft.FileTransferManager;

internal sealed class CopyItem
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