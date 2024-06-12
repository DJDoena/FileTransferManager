using System;
using System.Windows.Forms;

namespace DoenaSoft.FileTransferManager;

internal interface IView
{
    bool InvokeRequired { get; }

    int ProgressBarMax { get; set; }

    object Invoke(Delegate method);

    DialogResult ShowMessageBox(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon);

    void UpdateProgressBar(long bytes, long divider, DateTime start);

    void Refresh();
}