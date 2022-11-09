using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DoenaSoft.FileTransferManager.Properties;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace DoenaSoft.FileTransferManager
{
    public partial class MainForm : Form
    {
        private String m_LastFolder = String.Empty;

        private Int64 m_Divider = 1;

        private Thread m_CopyThread = null;

        private delegate void ProgressBarDelegate(Int64 bytes, DateTime start);

        private delegate void FormDelegate();

        private delegate void ShowMessageBoxDelegate(String message, String title, MessageBoxButtons buttons, MessageBoxIcon icon);

        private event EventHandler CopyFinished;

        private System.Windows.Forms.Timer AbortTimer { get; set; }

        private class SourceTarget
        {
            internal DirectoryInfo SourceFolder;

            internal FileInfo SourceFile;

            internal DirectoryInfo TargetFolder;

            internal SourceTarget(DirectoryInfo sourceFolder, FileInfo sourceFile)
            {
                SourceFolder = sourceFolder;
                SourceFile = sourceFile;
            }
        }

        public MainForm()
        {
            InitializeComponent();
        }

        private void OnRemoveEntryButtonClick(Object sender
            , EventArgs e)
        {
            if (SourceListBox.SelectedIndex != -1)
            {
                Int32 previousIndex = SourceListBox.SelectedIndex;

                SourceListBox.Items.RemoveAt(previousIndex);

                if (SourceListBox.Items.Count > previousIndex)
                {
                    SourceListBox.SelectedIndex = previousIndex;
                }
                else if (SourceListBox.Items.Count > 0)
                {
                    SourceListBox.SelectedIndex = SourceListBox.Items.Count - 1;
                }

                FormatBytes();
            }
        }

        private void OnAddFolderButtonClick(Object sender
            , EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.ShowNewFolderButton = false;
                fbd.SelectedPath = m_LastFolder;
                fbd.Description = "Select Folder to Copy";
                fbd.RootFolder = Environment.SpecialFolder.MyComputer;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    SourceListBox.Items.Add(fbd.SelectedPath);

                    m_LastFolder = fbd.SelectedPath;

                    FormatBytes();
                }
            }
        }

        private void OnAddFileButtonClick(Object sender
            , EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.Multiselect = true;
                ofd.InitialDirectory = m_LastFolder;
                ofd.Title = "Select File(s) to Copy";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    SourceListBox.Items.AddRange(ofd.FileNames);

                    m_LastFolder = new FileInfo(ofd.FileNames[0]).Directory.FullName;

                    FormatBytes();
                }
            }
        }

        private void OnTargetLocationButtonClick(Object sender
            , EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.ShowNewFolderButton = true;
                fbd.SelectedPath = TargetLocationTextBox.Text;
                fbd.Description = "Select Target Folder";
                fbd.RootFolder = Environment.SpecialFolder.MyComputer;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    TargetLocationTextBox.Text = fbd.SelectedPath;
                }
            }
        }

        private void OnCopyButtonClick(Object sender
            , EventArgs e)
        {
            if (TaskbarManager.IsPlatformSupported)
            {
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                TaskbarManager.Instance.SetProgressValue(0, ProgressBar.Maximum);
            }

            ProgressBar.Value = 0;

            if (String.IsNullOrEmpty(TargetLocationTextBox.Text))
            {
                MessageBox.Show("No Target Selected!", "No Target", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            List<SourceTarget> fileInfos = new List<SourceTarget>();

            foreach (String entry in SourceListBox.Items)
            {
                if (Directory.Exists(entry))
                {
                    DirectoryInfo di = new DirectoryInfo(entry);

                    SearchOption option = (WithSubFoldersCheckBox.Checked) ? (SearchOption.AllDirectories) : (SearchOption.TopDirectoryOnly);

                    FileInfo[] fis = di.GetFiles("*.*", option);

                    foreach (FileInfo fi in fis)
                    {
                        if (di.FullName == di.Root.FullName)
                        {
                            fileInfos.Add(new SourceTarget(new DirectoryInfo(di.FullName + "\\!Drive" + di.FullName.Substring(0, 1)), fi));
                        }
                        else
                        {
                            fileInfos.Add(new SourceTarget(di.Parent, fi));
                        }
                    }
                }
                else if (File.Exists(entry))
                {
                    FileInfo fi = new FileInfo(entry);

                    fileInfos.Add(new SourceTarget(null, fi));
                }
                else
                {
                    MessageBox.Show("Something is weird about\n" + entry, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    return;
                }
            }

            Int64 bytes = 0;

            foreach (SourceTarget fileInfo in fileInfos)
            {
                bytes += fileInfo.SourceFile.Length;
            }

            DriveInfo driveInfo = new DriveInfo((new DirectoryInfo(TargetLocationTextBox.Text)).Root.Name.Substring(0, 1));

            if (driveInfo.AvailableFreeSpace <= bytes)
            {
                MessageBox.Show("Target is Full!" + Environment.NewLine + "Available: " + FormatBytes(driveInfo.AvailableFreeSpace)
                    + Environment.NewLine + "Needed: " + FormatBytes(bytes), "Target Full", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return;
            }

            if (bytes >= Math.Pow(2, 40) * 2)
            {
                m_Divider = 1000000;
            }
            else if (bytes >= Math.Pow(2, 30) * 2)
            {
                m_Divider = 1000;
            }
            else
            {
                m_Divider = 1;
            }

            ProgressBar.Maximum = (Int32)(bytes / m_Divider);

            CopyFinished += OnMainFormCopyFinished;

            SwitchUI(false);

            m_CopyThread = new Thread(new ParameterizedThreadStart(ThreadRun));

            m_CopyThread.IsBackground = true;
            m_CopyThread.Start(new Object[] { fileInfos, TargetLocationTextBox.Text, OverwriteComboBox.Text });
        }

        private void SwitchUI(Boolean enable)
        {
            Boolean inverse = (enable == false);

            TargetLocationTextBox.Enabled = enable;

            TargetLocationButton.Enabled = enable;

            AddFileButton.Enabled = enable;

            AddFolderButton.Enabled = enable;

            RemoveEntryButton.Enabled = enable;

            ClearListButton.Enabled = enable;

            SourceListBox.Enabled = enable;

            CopyButton.Enabled = enable;

            CopyButton.Visible = enable;

            WithSubFoldersCheckBox.Enabled = enable;

            OverwriteComboBox.Enabled = enable;

            ImportListButton.Enabled = enable;

            UseWaitCursor = inverse;

            AbortButton.Enabled = inverse;
            AbortButton.Visible = inverse;
        }

        private void OnMainFormCopyFinished(Object sender
            , EventArgs e)
        {
            SwitchUI(true);

            CopyFinished -= OnMainFormCopyFinished;
        }

        private void ThreadRun(Object fileInfosObject)
        {
            Boolean threadAbort = false;

            DateTime start = DateTime.Now;

            try
            {
                List<SourceTarget> fileInfos = (List<SourceTarget>)(((Object[])fileInfosObject)[0]);

                fileInfos.Sort((left, right) => left.SourceFile.FullName.CompareTo(right.SourceFile.FullName));

                String targetLocation = (String)(((Object[])fileInfosObject)[1]);

                String overwrite = (String)(((Object[])fileInfosObject)[2]);

                Int64 bytes = 0;

                foreach (SourceTarget fileInfo in fileInfos)
                {
                    if (fileInfo.SourceFolder != null)
                    {
                        String truncatedPath = fileInfo.SourceFile.FullName.Replace(fileInfo.SourceFolder.FullName, String.Empty);

                        String[] splitPath = truncatedPath.Split('\\');

                        if (splitPath.Length > 2)
                        {
                            String newPath = targetLocation;

                            for (Int32 i = 1; i < splitPath.Length - 1; i++)
                            {
                                newPath += "\\" + splitPath[i];

                                if (Directory.Exists(newPath) == false)
                                {
                                    Directory.CreateDirectory(newPath);
                                }
                            }

                            fileInfo.TargetFolder = new DirectoryInfo(newPath);
                        }
                    }

                    if (fileInfo.TargetFolder == null)
                    {
                        fileInfo.TargetFolder = new DirectoryInfo(targetLocation);
                    }
                }

                foreach (SourceTarget fileInfo in fileInfos)
                {
                    FileInfo targetFileInfo = new FileInfo(fileInfo.TargetFolder.FullName + "\\" + fileInfo.SourceFile.Name);

                    DialogResult result = DialogResult.Yes;

                    if (targetFileInfo.Exists)
                    {
                        result = DialogResult.No;

                        if (overwrite == "ask")
                        {
                            Int64 startTicks = DateTime.Now.Ticks;

                            result = MessageBox.Show("Overwrite \"" + targetFileInfo.FullName + "\"\nfrom \"" + fileInfo.SourceFile.FullName + "\"?", "Overwrite?"
                                , MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                            Int64 endTicks = DateTime.Now.Ticks;

                            TimeSpan span = new TimeSpan(endTicks - startTicks);

                            start = start.Add(span);
                        }
                        else if (overwrite == "always")
                        {
                            result = DialogResult.Yes;
                        }
                    }

                    if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                    else if (result == DialogResult.No)
                    {
                        Invoke((Action<Int64>)delegate (Int64 reducer)
                        { ProgressBar.Maximum -= (Int32)(reducer / m_Divider); }, fileInfo.SourceFile.Length);
                        Invoke(new ProgressBarDelegate(UpdateProgressBar), bytes, start);
                        Invoke(new FormDelegate(UpdateForm));

                        continue;
                    }
                    else if (result == DialogResult.Yes)
                    {
                        try
                        {
                            File.Copy(fileInfo.SourceFile.FullName, targetFileInfo.FullName, true);
                        }
                        catch (IOException ioEx)
                        {
                            Int64 startTicks = DateTime.Now.Ticks;

                            if (MessageBox.Show(ioEx.Message + "\nContinue?", "Continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                Int64 endTicks = DateTime.Now.Ticks;

                                TimeSpan span = new TimeSpan(endTicks - startTicks);

                                start = start.Add(span);

                                continue;
                            }
                            else
                            {
                                return;
                            }
                        }

                        bytes += fileInfo.SourceFile.Length;

                        Invoke(new ProgressBarDelegate(UpdateProgressBar), bytes, start);
                        Invoke(new FormDelegate(UpdateForm));
                    }
                }
            }
            catch (IOException ioEx)
            {
                Invoke(new ShowMessageBoxDelegate(ShowMessageBox), new Object[] { ioEx.Message, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error });

                return;
            }
            catch (ThreadAbortException)
            {
                Invoke(new ShowMessageBoxDelegate(ShowMessageBox), new Object[] { "Der Kopiervorgang wurde abgebrochen.", "Abbruch", MessageBoxButtons.OK, MessageBoxIcon.Warning });

                threadAbort = true;

                return;
            }
            catch (Exception ex)
            {
                Invoke(new ShowMessageBoxDelegate(ShowMessageBox), new Object[] { ex.Message, "?!?", MessageBoxButtons.OK, MessageBoxIcon.Error });

                return;
            }
            finally
            {
                if (threadAbort == false)
                {
                    Invoke(new ProgressBarDelegate(UpdateProgressBar), -1, start);

                    if (CopyFinished != null)
                    {
                        Invoke(CopyFinished, new Object[] { this, EventArgs.Empty });
                    }
                }
            }
        }

        private void ShowMessageBox(String message
            , String title
            , MessageBoxButtons buttons
            , MessageBoxIcon icon)
        {
            MessageBox.Show(message, title, buttons, icon);
        }

        private void OnMainFormLoad(Object sender
            , EventArgs e)
        {
            OverwriteComboBox.SelectedIndex = 0;

            String lastTarget = Settings.Default.LastTarget;

            if (String.IsNullOrEmpty(lastTarget) == false)
            {
                if (Directory.Exists(lastTarget))
                {
                    TargetLocationTextBox.Text = lastTarget;
                }
            }
        }

        private void OnClearListButtonClick(Object sender
            , EventArgs e)
        {
            SourceListBox.Items.Clear();

            FormatBytes();
        }

        private void UpdateProgressBar(Int64 bytes
            , DateTime start)
        {
            if (bytes == -1)
            {
                if (TaskbarManager.IsPlatformSupported)
                {
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                }

                ProgressBar.Value = ProgressBar.Maximum;

                RemaingLabel.Text = ".....";
            }
            else
            {
                DateTime now = DateTime.Now;

                Int32 value = (Int32)(bytes / m_Divider);

                if (TaskbarManager.IsPlatformSupported)
                {
                    TaskbarManager.Instance.SetProgressValue(value, ProgressBar.Maximum);
                }

                ProgressBar.Value = value;

                if (ProgressBar.Value != 0)
                {
                    TimeSpan span = now.Subtract(start);

                    Decimal completeTimeTicks = (Decimal)(ProgressBar.Maximum) / ProgressBar.Value * span.Ticks;

                    TimeSpan remainingTime = new TimeSpan(Convert.ToInt64(completeTimeTicks) - span.Ticks);

                    Decimal speed = bytes * 1000m / (Decimal)(span.TotalMilliseconds);

                    String speedText = " (" + FormatBytes(Convert.ToInt64(speed)) + "/s)";

                    if (remainingTime.Hours > 0)
                    {
                        Int32 minutes = remainingTime.Minutes;

                        if (remainingTime.Seconds > 30)
                        {
                            minutes++;
                        }

                        RemaingLabel.Text = "est. " + remainingTime.Hours + " hours, " + minutes + " minutes remaining" + speedText;
                    }
                    else if (remainingTime.Minutes > 0)
                    {
                        RemaingLabel.Text = "est. " + remainingTime.Minutes + " minutes, " + remainingTime.Seconds + " seconds remaining" + speedText;
                    }
                    else
                    {
                        RemaingLabel.Text = "est. " + remainingTime.Seconds + " seconds remaining" + speedText;
                    }
                }
            }

            ProgressBar.Update();
            ProgressBar.Refresh();
        }

        private void UpdateForm()
        {
            Refresh();
        }

        private void OnAbortButtonClick(Object sender
            , EventArgs e)
        {
            if (m_CopyThread != null)
            {
                AbortTimer = new System.Windows.Forms.Timer();

                AbortTimer.Interval = 100;
                AbortTimer.Tick += new EventHandler(AbortTimerTick);
                AbortTimer.Start();

                m_CopyThread.Abort();
            }
        }

        void AbortTimerTick(Object sender
            , EventArgs e)
        {
            if (m_CopyThread.IsAlive == false)
            {
                AbortTimer.Stop();

                m_CopyThread = null;

                UpdateProgressBar(-1, DateTime.Now);

                CopyFinished?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnAbortButtonMouseEnter(Object sender
            , EventArgs e)
        {
            if (AbortButton.Visible)
            {
                UseWaitCursor = false;
            }
        }

        private void OnAbortButtonMouseLeave(Object sender
            , EventArgs e)
        {
            if (AbortButton.Visible)
            {
                UseWaitCursor = true;
            }
        }

        private void FormatBytes()
        {
            SizeLabel.Text = (SourceListBox.Items.Count > 0) ? (FormatBytes(CalculateBytes())) : ("0 Byte");
        }

        private Int64 CalculateBytes()
        {
            Int64 bytes = 0;

            foreach (String item in SourceListBox.Items)
            {
                if (Directory.Exists(item))
                {
                    SearchOption option = (WithSubFoldersCheckBox.Checked) ? (SearchOption.AllDirectories) : (SearchOption.TopDirectoryOnly);

                    String[] files = Directory.GetFiles(item, "*.*", option);

                    foreach (String file in files)
                    {
                        FileInfo fi = new FileInfo(file);

                        bytes += fi.Length;
                    }
                }
                else if (File.Exists(item))
                {
                    FileInfo fi = new FileInfo(item);

                    bytes += fi.Length;
                }
            }

            return (bytes);
        }

        private static String FormatBytes(Int64 bytes)
        {
            Decimal roundBytes;
            String bytesPower;
            if (bytes / Math.Pow(2, 40) > 1)
            {
                roundBytes = Math.Round(bytes / (Decimal)(Math.Pow(2, 40)), 1, MidpointRounding.AwayFromZero);
                bytesPower = " TByte";
            }
            if (bytes / Math.Pow(2, 30) > 1)
            {
                roundBytes = Math.Round(bytes / (Decimal)(Math.Pow(2, 30)), 1, MidpointRounding.AwayFromZero);
                bytesPower = " GByte";
            }
            else if (bytes / Math.Pow(2, 20) > 1)
            {
                roundBytes = Math.Round(bytes / (Decimal)(Math.Pow(2, 20)), 1, MidpointRounding.AwayFromZero);
                bytesPower = " MByte";
            }
            else if (bytes / Math.Pow(2, 10) > 1)
            {
                roundBytes = Math.Round(bytes / (Decimal)(Math.Pow(2, 10)), 1, MidpointRounding.AwayFromZero);
                bytesPower = " KByte";
            }
            else
            {
                roundBytes = bytes;
                bytesPower = " Byte";
            }

            return (roundBytes + bytesPower);
        }

        private void OnWithSubFoldersCheckBoxCheckedChanged(Object sender
            , EventArgs e)
        {
            FormatBytes();
        }

        private void OnMainFormClosing(Object sender
            , FormClosingEventArgs e)
        {
            Settings.Default.LastTarget = TargetLocationTextBox.Text;
            Settings.Default.Save();
        }

        private void OnImportListButtonClick(Object sender
            , EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.Filter = "Text files|*.lst;*.txt";
                ofd.Multiselect = false;
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    using (StreamReader sr = new StreamReader(ofd.FileName, Encoding.GetEncoding(1252)))
                    {
                        while (sr.EndOfStream == false)
                        {
                            String file = sr.ReadLine();

                            if (File.Exists(file))
                            {
                                SourceListBox.Items.Add(file);

                                FormatBytes();
                            }
                        }
                    }
                }
            }
        }

        private void OnSourceListBoxKeyDown(Object sender
            , KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                OnRemoveEntryButtonClick(sender, e);
            }
        }
    }
}