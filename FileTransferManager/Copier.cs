using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace DoenaSoft.FileTransferManager;

internal sealed class Copier
{
    private DateTime _start;

    private long _bytes;

    private readonly IReadOnlyCollection<CopyItem> _targetItems;

    private readonly string _overwrite;

    private readonly long _divider;

    private readonly IMainForm _mainForm;

    private Thread _copyThread;

    private System.Windows.Forms.Timer _abortTimer;

    public Copier(IReadOnlyCollection<CopyItem> targetItems, string overwrite, long divider, IMainForm mainForm)
    {
        _targetItems = targetItems;
        _overwrite = overwrite;
        _divider = divider;
        _mainForm = mainForm;
    }

    public event EventHandler CopyFinished;

    internal void Start()
    {
        _copyThread = new Thread(this.CopyFiles)
        {
            IsBackground = true,
        };

        _copyThread.Start();
    }

    internal void Abort()
    {
        if (_copyThread != null)
        {
            _abortTimer = new System.Windows.Forms.Timer()
            {
                Interval = 100,
            };

            _abortTimer.Tick += this.AbortTimerTick;
            _abortTimer.Start();

            _copyThread.Abort();
        }
    }

    private void CopyFiles()
    {
        var threadAbort = false;

        _start = DateTime.UtcNow;

        try
        {
            _bytes = 0;

            foreach (var item in _targetItems)
            {
                if (!this.CopyFile(item))
                {
                    return;
                }
            }
        }
        catch (IOException ioEx)
        {
            _mainForm.ShowMessageBox(ioEx.Message, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return;
        }
        catch (ThreadAbortException)
        {
            _mainForm.ShowMessageBox("The copy process was cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            threadAbort = true;

            return;
        }
        catch (Exception ex)
        {
            _mainForm.ShowMessageBox(ex.Message, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return;
        }
        finally
        {
            if (threadAbort == false)
            {
                this.ExecuteOnUI(() =>
                {
                    _mainForm.UpdateProgressBar(-1, _start, _divider);

                    CopyFinished?.Invoke(this, EventArgs.Empty);
                });
            }
        }
    }

    private bool CopyFile(CopyItem item)
    {
        if (!item.TargetFolder.Exists)
        {
            item.TargetFolder.Create();
        }

        var targetFile = new FileInfo(Path.Combine(item.TargetFolder.FullName, item.SourceFile.Name));

        var overwriteDecision = this.GetOverwriteDecision(item, targetFile);

        if (overwriteDecision == DialogResult.Cancel)
        {
            return false;
        }
        else if (overwriteDecision == DialogResult.No)
        {
            this.SkipFile(item);

            return true;
        }
        else if (overwriteDecision == DialogResult.Yes)
        {
            var continueDecision = this.OverwriteFile(item, targetFile);

            return continueDecision;
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    private void SkipFile(CopyItem item)
    {
        this.ExecuteOnUI(() =>
        {
            _mainForm.ProgressBarMax -= (int)(item.SourceFile.Length / _divider);

            _mainForm.UpdateProgressBar(_bytes, _start, _divider);

            _mainForm.Refresh();
        });
    }

    private bool OverwriteFile(CopyItem item, FileInfo targetFile)
    {
        try
        {
            File.Copy(item.SourceFile.FullName, targetFile.FullName, true);
        }
        catch (IOException ioEx)
        {
            var continueDecision = this.ShowTimedMessageBox($"{ioEx.Message}\nContinue?", "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            return continueDecision == DialogResult.Yes;
        }

        _bytes += item.SourceFile.Length;

        this.ExecuteOnUI(() =>
        {
            _mainForm.UpdateProgressBar(_bytes, _start, _divider);

            _mainForm.Refresh();
        });

        return true;
    }

    private DialogResult GetOverwriteDecision(CopyItem item, FileInfo targetFile)
    {
        if (!targetFile.Exists)
        {
            return DialogResult.Yes;
        }
        else if (_overwrite == "ask")
        {
            var decision = this.ShowTimedMessageBox($"Overwrite \"{targetFile.FullName}\"\nfrom \"{item.SourceFile.FullName}\"?", "Overwrite?"
                , MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            return decision;
        }
        else if (_overwrite == "always")
        {
            return DialogResult.Yes;
        }
        else
        {
            return DialogResult.No;
        }
    }

    private DialogResult ShowTimedMessageBox(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        var start = DateTime.UtcNow;

        var decision = _mainForm.ShowMessageBox(message, title, buttons, icon);

        var span = DateTime.UtcNow.Subtract(start);

        _start = _start.Add(span);

        return decision;
    }

    private void ExecuteOnUI(Action action)
    {
        if (_mainForm.InvokeRequired)
        {
            _mainForm.Invoke(action);
        }
        else
        {
            action();
        }
    }

    private void AbortTimerTick(object sender, EventArgs e)
    {
        if (!_copyThread.IsAlive)
        {
            _abortTimer.Stop();

            _copyThread = null;

            _mainForm.UpdateProgressBar(-1, DateTime.UtcNow, _divider);

            CopyFinished?.Invoke(this, EventArgs.Empty);
        }
    }
}