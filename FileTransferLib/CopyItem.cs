using DoenaSoft.AbstractionLayer.IOServices;

namespace DoenaSoft.FileTransferManager;

public sealed class CopyItem
{
    public IFolderInfo SourceFolder { get; }

    public IFileInfo SourceFile { get; }

    public IFolderInfo TargetFolder { get; }

    public string DisplayText
    {
        get
        {
            var source = this.SourceFolder != null
                ? this.SourceFolder.FullName
                : this.SourceFile.FullName;

            return $"{source} → {this.TargetFolder.FullName}";
        }
    }

    public CopyItem(IFolderInfo sourceFolder, IFolderInfo targetFolder)
    {
        this.SourceFolder = sourceFolder;
        this.SourceFile = null;
        this.TargetFolder = targetFolder;
    }

    public CopyItem(IFileInfo sourceFile, IFolderInfo targetFolder)
    {
        this.SourceFolder = null;
        this.SourceFile = sourceFile;
        this.TargetFolder = targetFolder;
    }

    public override string ToString()
        => this.DisplayText;
}