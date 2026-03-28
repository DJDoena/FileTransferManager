using System;
using System.Collections.Generic;
using System.Threading;
using DoenaSoft.AbstractionLayer.IOServices;
using DoenaSoft.AbstractionLayer.UIServices;

namespace DoenaSoft.FileTransferManager;

public sealed class Copier
{
    private DateTime _start;

    private long _bytes;

    private readonly IReadOnlyCollection<CopyItem> _items;

    private readonly OverwriteMode _overwrite;

    private readonly long _divider;

    private readonly IView _view;

    private readonly IIOServices _ioServices;

    private Thread _worker;

    private System.Timers.Timer _abortTimer;

    public Copier(IReadOnlyCollection<CopyItem> items
        , OverwriteMode overwrite
        , long divider
        , IView view
        , IIOServices ioServices)
    {
        _items = items;
        _overwrite = overwrite;
        _divider = divider;
        _view = view;
        _ioServices = ioServices;
    }

    public event EventHandler CopyFinished;

    public void Start()
    {
        _worker = new Thread(this.CopyFiles)
        {
            IsBackground = true,
        };

        _worker.Start();
    }

    public void Abort()
    {
        if (_worker != null)
        {
            _abortTimer = new System.Timers.Timer()
            {
                Interval = 100,
            };

            _abortTimer.Elapsed += this.AbortTimerTick;
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
        catch (System.IO.IOException ioEx)
        {
            _view.ShowMessageBox(ioEx.Message, "?!?", MessageButtons.OK, MessageIcon.Error);

            return;
        }
        catch (ThreadAbortException)
        {
            _view.ShowMessageBox("The copy process was cancelled.", "Cancelled", MessageButtons.OK, MessageIcon.Warning);

            threadAbort = true;

            return;
        }
        catch (Exception ex)
        {
            _view.ShowMessageBox(ex.Message, "?!?", MessageButtons.OK, MessageIcon.Error);

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

    private bool CopyFile(CopyItem item
        )
    {
        if (!item.TargetFolder.Exists)
        {
            item.TargetFolder.Create();
        }

        var targetFile = _ioServices.GetFile(_ioServices.Path.Combine(item.TargetFolder.FullName, item.SourceFile.Name));

        var overwriteDecision = this.GetOverwriteDecision(item, targetFile);

        if (overwriteDecision == Result.Cancel)
        {
            return false;
        }
        else if (overwriteDecision == Result.No)
        {
            this.SkipFile(item);

            return true;
        }
        else if (overwriteDecision == Result.Yes)
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
            _view.ProgressBarMax -= (int)(((long)item.SourceFile.Length) / _divider);

            _view.UpdateProgressBar(_bytes, _divider, _start);

            _view.Refresh();
        });
    }

    private bool OverwriteFile(CopyItem item, IFileInfo targetFile)
    {
        try
        {
            _ioServices.File.Copy(item.SourceFile.FullName, targetFile.FullName, true);
        }
        catch (System.IO.IOException ioEx)
        {
            var continueDecision = this.ShowTimedMessageBox($"{ioEx.Message}\nContinue?", "Continue?", MessageButtons.YesNo, MessageIcon.Question);

            return continueDecision == Result.Yes;
        }

        _bytes += (long)item.SourceFile.Length;

        this.ExecuteOnUI(() =>
        {
            _view.UpdateProgressBar(_bytes, _divider, _start);

            _view.Refresh();
        });

        return true;
    }

    private Result GetOverwriteDecision(CopyItem item, IFileInfo targetFile)
    {
        if (!targetFile.Exists)
        {
            return Result.Yes;
        }
        else if (_overwrite == OverwriteMode.Ask)
        {
            var decision = this.ShowTimedMessageBox($"Overwrite \"{targetFile.FullName}\"\nfrom \"{item.SourceFile.FullName}\"?", "Overwrite?"
                , MessageButtons.YesNoCancel, MessageIcon.Question);

            return decision;
        }
        else if (_overwrite == OverwriteMode.Always)
        {
            return Result.Yes;
        }
        else
        {
            return Result.No;
        }
    }

    private Result ShowTimedMessageBox(string message, string title, MessageButtons buttons, MessageIcon icon)
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