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

    private readonly IReadOnlyCollection<CopyItem> _items;

    private readonly OverwriteMode _overwrite;

    private readonly long _divider;

    private readonly IView _view;

    private Thread _worker;

    private System.Windows.Forms.Timer _abortTimer;

    public Copier(IReadOnlyCollection<CopyItem> items, OverwriteMode overwrite, long divider, IView view)
    {
        _items = items;
        _overwrite = overwrite;
        _divider = divider;
        _view = view;
    }

    public event EventHandler CopyFinished;

    internal void Start()
    {
        _worker = new Thread(this.CopyFiles)
        {
            IsBackground = true,
        };

        _worker.Start();
    }

    internal void Abort()
    {
        if (_worker != null)
        {
            _abortTimer = new System.Windows.Forms.Timer()
            {
                Interval = 100,
            };

            _abortTimer.Tick += this.AbortTimerTick;
            _abortTimer.Start();

            _worker.Abort();
        }
    }

    private void CopyFiles()
    {
        var threadAbort = false;

        _start = DateTime.UtcNow;

        try
        {
            _bytes = 0;

            foreach (var item in _items)
            {
                if (!this.CopyFile(item))
                {
                    return;
                }
            }
        }
        catch (IOException ioEx)
        {
            _view.ShowMessageBox(ioEx.Message, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return;
        }
        catch (ThreadAbortException)
        {
            _view.ShowMessageBox("The copy process was cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            threadAbort = true;

            return;
        }
        catch (Exception ex)
        {
            _view.ShowMessageBox(ex.Message, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return;
        }
        finally
        {
            if (threadAbort == false)
            {
                this.ExecuteOnUI(() =>
                {
                    _view.UpdateProgressBar(-1, _divider, _start);

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
            _view.ProgressBarMax -= (int)(item.SourceFile.Length / _divider);

            _view.UpdateProgressBar(_bytes, _divider, _start);

            _view.Refresh();
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
            _view.UpdateProgressBar(_bytes, _divider, _start);

            _view.Refresh();
        });

        return true;
    }

    private DialogResult GetOverwriteDecision(CopyItem item, FileInfo targetFile)
    {
        if (!targetFile.Exists)
        {
            return DialogResult.Yes;
        }
        else if (_overwrite == OverwriteMode.Ask)
        {
            var decision = this.ShowTimedMessageBox($"Overwrite \"{targetFile.FullName}\"\nfrom \"{item.SourceFile.FullName}\"?", "Overwrite?"
                , MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            return decision;
        }
        else if (_overwrite == OverwriteMode.Always)
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
        var openDialogTimestamp = DateTime.UtcNow;

        var decision = _view.ShowMessageBox(message, title, buttons, icon);

        var dialogTime = DateTime.UtcNow.Subtract(openDialogTimestamp);

        _start = _start.Add(dialogTime);

        return decision;
    }

    private void ExecuteOnUI(Action action)
    {
        if (_view.InvokeRequired)
        {
            _view.Invoke(action);
        }
        else
        {
            action();
        }
    }

    private void AbortTimerTick(object sender, EventArgs e)
    {
        if (!_worker.IsAlive)
        {
            _abortTimer.Stop();

            _worker = null;

            _view.UpdateProgressBar(-1, _divider, DateTime.UtcNow);

            CopyFinished?.Invoke(this, EventArgs.Empty);
        }
    }
}