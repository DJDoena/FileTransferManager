using System;
using DoenaSoft.AbstractionLayer.UIServices;

namespace DoenaSoft.FileTransferManager;

public interface IMainWindowView
{
    bool InvokeRequired { get; }

    int ProgressBarMax { get; set; }

    object Invoke(Delegate method);

    Result ShowMessageBox(string message, string title, MessageButtons buttons, MessageIcon icon);

    void UpdateProgressBar(long bytes, long divider, DateTime start);

    void Refresh();
}