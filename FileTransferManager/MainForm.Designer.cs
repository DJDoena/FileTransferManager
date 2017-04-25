namespace DoenaSoft.FileTransferManager
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.TargetLocationTextBox = new System.Windows.Forms.TextBox();
            this.TargetLocationButton = new System.Windows.Forms.Button();
            this.TargetLabel = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SourceListBox = new System.Windows.Forms.ListBox();
            this.AddFileButton = new System.Windows.Forms.Button();
            this.AddFolderButton = new System.Windows.Forms.Button();
            this.RemoveEntryButton = new System.Windows.Forms.Button();
            this.WithSubFoldersCheckBox = new System.Windows.Forms.CheckBox();
            this.CopyButton = new System.Windows.Forms.Button();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.OverwriteComboBox = new System.Windows.Forms.ComboBox();
            this.ClearListButton = new System.Windows.Forms.Button();
            this.RemaingLabel = new System.Windows.Forms.Label();
            this.AbortButton = new System.Windows.Forms.Button();
            this.SizeLabel = new System.Windows.Forms.Label();
            this.ImportListButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // TargetLocationTextBox
            // 
            this.TargetLocationTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TargetLocationTextBox.Location = new System.Drawing.Point(84, 13);
            this.TargetLocationTextBox.Name = "TargetLocationTextBox";
            this.TargetLocationTextBox.ReadOnly = true;
            this.TargetLocationTextBox.Size = new System.Drawing.Size(525, 20);
            this.TargetLocationTextBox.TabIndex = 0;
            // 
            // TargetLocationButton
            // 
            this.TargetLocationButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.TargetLocationButton.Location = new System.Drawing.Point(615, 11);
            this.TargetLocationButton.Name = "TargetLocationButton";
            this.TargetLocationButton.Size = new System.Drawing.Size(85, 23);
            this.TargetLocationButton.TabIndex = 1;
            this.TargetLocationButton.Text = "Select Target";
            this.TargetLocationButton.UseVisualStyleBackColor = true;
            this.TargetLocationButton.Click += new System.EventHandler(this.OnTargetLocationButtonClick);
            // 
            // TargetLabel
            // 
            this.TargetLabel.AutoSize = true;
            this.TargetLabel.Location = new System.Drawing.Point(12, 16);
            this.TargetLabel.Name = "TargetLabel";
            this.TargetLabel.Size = new System.Drawing.Size(41, 13);
            this.TargetLabel.TabIndex = 2;
            this.TargetLabel.Text = "Target:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 40);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(44, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Source:";
            // 
            // SourceListBox
            // 
            this.SourceListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.SourceListBox.FormattingEnabled = true;
            this.SourceListBox.HorizontalScrollbar = true;
            this.SourceListBox.Location = new System.Drawing.Point(84, 40);
            this.SourceListBox.Name = "SourceListBox";
            this.SourceListBox.Size = new System.Drawing.Size(525, 342);
            this.SourceListBox.TabIndex = 4;
            this.SourceListBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnSourceListBoxKeyDown);
            // 
            // AddFileButton
            // 
            this.AddFileButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddFileButton.Location = new System.Drawing.Point(615, 40);
            this.AddFileButton.Name = "AddFileButton";
            this.AddFileButton.Size = new System.Drawing.Size(85, 23);
            this.AddFileButton.TabIndex = 5;
            this.AddFileButton.Text = "Add File(s)";
            this.AddFileButton.UseVisualStyleBackColor = true;
            this.AddFileButton.Click += new System.EventHandler(this.OnAddFileButtonClick);
            // 
            // AddFolderButton
            // 
            this.AddFolderButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddFolderButton.Location = new System.Drawing.Point(615, 69);
            this.AddFolderButton.Name = "AddFolderButton";
            this.AddFolderButton.Size = new System.Drawing.Size(85, 23);
            this.AddFolderButton.TabIndex = 6;
            this.AddFolderButton.Text = "Add Folder";
            this.AddFolderButton.UseVisualStyleBackColor = true;
            this.AddFolderButton.Click += new System.EventHandler(this.OnAddFolderButtonClick);
            // 
            // RemoveEntryButton
            // 
            this.RemoveEntryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RemoveEntryButton.Location = new System.Drawing.Point(615, 127);
            this.RemoveEntryButton.Name = "RemoveEntryButton";
            this.RemoveEntryButton.Size = new System.Drawing.Size(85, 23);
            this.RemoveEntryButton.TabIndex = 7;
            this.RemoveEntryButton.Text = "Remove Entry";
            this.RemoveEntryButton.UseVisualStyleBackColor = true;
            this.RemoveEntryButton.Click += new System.EventHandler(this.OnRemoveEntryButtonClick);
            // 
            // WithSubFoldersCheckBox
            // 
            this.WithSubFoldersCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.WithSubFoldersCheckBox.AutoSize = true;
            this.WithSubFoldersCheckBox.Checked = true;
            this.WithSubFoldersCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.WithSubFoldersCheckBox.Location = new System.Drawing.Point(84, 389);
            this.WithSubFoldersCheckBox.Name = "WithSubFoldersCheckBox";
            this.WithSubFoldersCheckBox.Size = new System.Drawing.Size(15, 14);
            this.WithSubFoldersCheckBox.TabIndex = 8;
            this.WithSubFoldersCheckBox.UseVisualStyleBackColor = true;
            this.WithSubFoldersCheckBox.CheckedChanged += new System.EventHandler(this.OnWithSubFoldersCheckBoxCheckedChanged);
            // 
            // CopyButton
            // 
            this.CopyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CopyButton.Location = new System.Drawing.Point(615, 436);
            this.CopyButton.Name = "CopyButton";
            this.CopyButton.Size = new System.Drawing.Size(85, 23);
            this.CopyButton.TabIndex = 9;
            this.CopyButton.Text = "Copy";
            this.CopyButton.UseVisualStyleBackColor = true;
            this.CopyButton.Click += new System.EventHandler(this.OnCopyButtonClick);
            // 
            // ProgressBar
            // 
            this.ProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgressBar.Location = new System.Drawing.Point(84, 436);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(525, 23);
            this.ProgressBar.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 411);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Overwrite:";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 388);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "Sub-Folders:";
            // 
            // OverwriteComboBox
            // 
            this.OverwriteComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.OverwriteComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.OverwriteComboBox.FormattingEnabled = true;
            this.OverwriteComboBox.Items.AddRange(new object[] {
            "ask",
            "always",
            "never"});
            this.OverwriteComboBox.Location = new System.Drawing.Point(84, 408);
            this.OverwriteComboBox.MaxDropDownItems = 3;
            this.OverwriteComboBox.Name = "OverwriteComboBox";
            this.OverwriteComboBox.Size = new System.Drawing.Size(121, 21);
            this.OverwriteComboBox.TabIndex = 13;
            // 
            // ClearListButton
            // 
            this.ClearListButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ClearListButton.Location = new System.Drawing.Point(615, 156);
            this.ClearListButton.Name = "ClearListButton";
            this.ClearListButton.Size = new System.Drawing.Size(85, 23);
            this.ClearListButton.TabIndex = 14;
            this.ClearListButton.Text = "Clear List";
            this.ClearListButton.UseVisualStyleBackColor = true;
            this.ClearListButton.Click += new System.EventHandler(this.OnClearListButtonClick);
            // 
            // RemaingLabel
            // 
            this.RemaingLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RemaingLabel.AutoSize = true;
            this.RemaingLabel.Location = new System.Drawing.Point(211, 411);
            this.RemaingLabel.Name = "RemaingLabel";
            this.RemaingLabel.Size = new System.Drawing.Size(22, 13);
            this.RemaingLabel.TabIndex = 15;
            this.RemaingLabel.Text = ".....";
            // 
            // AbortButton
            // 
            this.AbortButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.AbortButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.AbortButton.Location = new System.Drawing.Point(615, 436);
            this.AbortButton.Name = "AbortButton";
            this.AbortButton.Size = new System.Drawing.Size(85, 23);
            this.AbortButton.TabIndex = 16;
            this.AbortButton.Text = "Abort";
            this.AbortButton.UseVisualStyleBackColor = true;
            this.AbortButton.Visible = false;
            this.AbortButton.Click += new System.EventHandler(this.OnAbortButtonClick);
            this.AbortButton.MouseEnter += new System.EventHandler(this.OnAbortButtonMouseEnter);
            this.AbortButton.MouseLeave += new System.EventHandler(this.OnAbortButtonMouseLeave);
            // 
            // SizeLabel
            // 
            this.SizeLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SizeLabel.AutoSize = true;
            this.SizeLabel.Location = new System.Drawing.Point(615, 369);
            this.SizeLabel.Name = "SizeLabel";
            this.SizeLabel.Size = new System.Drawing.Size(37, 13);
            this.SizeLabel.TabIndex = 17;
            this.SizeLabel.Text = "0 Byte";
            // 
            // ImportListButton
            // 
            this.ImportListButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ImportListButton.Location = new System.Drawing.Point(615, 98);
            this.ImportListButton.Name = "ImportListButton";
            this.ImportListButton.Size = new System.Drawing.Size(85, 23);
            this.ImportListButton.TabIndex = 18;
            this.ImportListButton.Text = "Import List";
            this.ImportListButton.UseVisualStyleBackColor = true;
            this.ImportListButton.Click += new System.EventHandler(this.OnImportListButtonClick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(712, 478);
            this.Controls.Add(this.ImportListButton);
            this.Controls.Add(this.SizeLabel);
            this.Controls.Add(this.AbortButton);
            this.Controls.Add(this.RemaingLabel);
            this.Controls.Add(this.ClearListButton);
            this.Controls.Add(this.OverwriteComboBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ProgressBar);
            this.Controls.Add(this.CopyButton);
            this.Controls.Add(this.WithSubFoldersCheckBox);
            this.Controls.Add(this.RemoveEntryButton);
            this.Controls.Add(this.AddFolderButton);
            this.Controls.Add(this.AddFileButton);
            this.Controls.Add(this.SourceListBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TargetLabel);
            this.Controls.Add(this.TargetLocationButton);
            this.Controls.Add(this.TargetLocationTextBox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(720, 505);
            this.Name = "MainForm";
            this.Text = "File Transfer Manager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnMainFormClosing);
            this.Load += new System.EventHandler(this.OnMainFormLoad);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TargetLocationTextBox;
        private System.Windows.Forms.Button TargetLocationButton;
        private System.Windows.Forms.Label TargetLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox SourceListBox;
        private System.Windows.Forms.Button AddFileButton;
        private System.Windows.Forms.Button AddFolderButton;
        private System.Windows.Forms.Button RemoveEntryButton;
        private System.Windows.Forms.CheckBox WithSubFoldersCheckBox;
        private System.Windows.Forms.Button CopyButton;
        private System.Windows.Forms.ProgressBar ProgressBar;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox OverwriteComboBox;
        private System.Windows.Forms.Button ClearListButton;
        private System.Windows.Forms.Label RemaingLabel;
        private System.Windows.Forms.Button AbortButton;
        private System.Windows.Forms.Label SizeLabel;
        private System.Windows.Forms.Button ImportListButton;
    }
}

