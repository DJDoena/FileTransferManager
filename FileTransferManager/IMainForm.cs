using System;
using System.Windows.Forms;

namespace DoenaSoft.FileTransferManager;

internal interface IMainForm
{
    bool InvokeRequired { get; }

    int ProgressBarMax { get; set; }

    object Invoke(Delegate method);

    DialogResult ShowMessageBox(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon);

    void UpdateProgressBar(long bytes, DateTime start, long divider);

    void Refresh();
}