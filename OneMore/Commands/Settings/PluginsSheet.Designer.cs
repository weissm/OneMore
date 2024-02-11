﻿namespace River.OneMoreAddIn.Settings
{
	partial class PluginsSheet
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
			if (disposing && (components != null))
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this.gridView = new System.Windows.Forms.DataGridView();
			this.introBox = new River.OneMoreAddIn.UI.MoreMultilineLabel();
			this.toolStrip = new River.OneMoreAddIn.UI.ScaledToolStrip();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.runLabel = new System.Windows.Forms.ToolStripLabel();
			this.runButton = new System.Windows.Forms.ToolStripButton();
			this.deleteLabel = new System.Windows.Forms.ToolStripLabel();
			this.deleteButton = new System.Windows.Forms.ToolStripButton();
			this.nameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.cmdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.introBox = new River.OneMoreAddIn.UI.MoreMultilineLabel();
			this.toolStrip = new River.OneMoreAddIn.UI.MoreToolStrip();
			this.renameButton = new River.OneMoreAddIn.UI.MoreMenuItem();
			this.deleteButton = new River.OneMoreAddIn.UI.MoreMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.gridView)).BeginInit();
			this.SuspendLayout();
			// 
			// gridView
			// 
			this.gridView.AllowUserToAddRows = false;
			this.gridView.AllowUserToResizeRows = false;
			this.gridView.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
			this.gridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.gridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.nameColumn,
			this.cmdColumn});
			this.gridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.gridView.Location = new System.Drawing.Point(10, 93);
			this.gridView.MultiSelect = false;
			this.gridView.Name = "gridView";
			this.gridView.RowHeadersWidth = 30;
			this.gridView.RowTemplate.Height = 28;
			this.gridView.Size = new System.Drawing.Size(780, 398);
			this.gridView.TabIndex = 2;
this.gridView.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.gridView_CellContentClick);
			this.gridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.EditOnDoubleClick);
			this.gridView.RowHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.EditOnDoubleClickRow);
			// 
			// nameColumn
			// 
			this.nameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.nameColumn.DataPropertyName = "Name";
			this.nameColumn.HeaderText = "Name";
			this.nameColumn.MinimumWidth = 100;
			this.nameColumn.Name = "nameColumn";
			this.nameColumn.ReadOnly = true;
			// 
			// cmdColumn
			// 
			this.cmdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.cmdColumn.DataPropertyName = "FullCommand";
			this.cmdColumn.HeaderText = "Command";
			this.cmdColumn.MinimumWidth = 8;
			this.cmdColumn.Name = "cmdColumn";
			this.cmdColumn.ReadOnly = true;
			// 
			// introPanel
			// 
			this.introPanel.Controls.Add(this.introLabel);
			this.introPanel.Dock = System.Windows.Forms.DockStyle.Top;
			this.introPanel.Location = new System.Drawing.Point(7, 6);
			this.introPanel.Margin = new System.Windows.Forms.Padding(2);
			this.introPanel.Name = "introPanel";
			this.introPanel.Size = new System.Drawing.Size(519, 36);
			this.introPanel.TabIndex = 3;
			// 
			// introLabel
			// 
			this.introLabel.AutoSize = true;
			this.introLabel.Location = new System.Drawing.Point(2, 3);
			this.introLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.introLabel.Name = "introLabel";
			this.introLabel.Size = new System.Drawing.Size(513, 13);
			this.introLabel.TabIndex = 0;
			this.introLabel.Text = "Click the edit button to modify a plugin or double-click its name to rename it. C" +
	"hanges are saved immediately";
			// 
			// toolStrip
			// 
			this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripButton1,
			this.toolStripSeparator1,
			this.runLabel,
			this.runButton,
			this.deleteLabel,
			this.deleteButton});
			this.toolStrip.Location = new System.Drawing.Point(7, 42);
			this.toolStrip.Name = "toolStrip";
			this.toolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.toolStrip.Size = new System.Drawing.Size(780, 33);
			this.toolStrip.Stretch = true;
			this.toolStrip.TabIndex = 4;
			this.toolStrip.Text = "Tool Strip";
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(34, 28);
			this.toolStripButton1.Text = "editButton";
			this.toolStripButton1.Click += new System.EventHandler(this.EditSelection);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 31);
// 
			// runLabel
			// 
			this.runLabel.Name = "runLabel";
			this.runLabel.Size = new System.Drawing.Size(37, 28);
			this.runLabel.Text = "Run:";
			this.runLabel.Click += new System.EventHandler(this.runLabel_Click);
			// 
			// runButton
			// 
			this.runButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.runButton.Image = ((System.Drawing.Image)(resources.GetObject("runButton.Image")));
			this.runButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.runButton.Name = "runButton";
			this.runButton.Size = new System.Drawing.Size(29, 28);
			this.runButton.Text = "Run";
			this.runButton.Click += new System.EventHandler(this.RunItem);
			// 
			// deleteLabel
			// 
			this.deleteLabel.ForeColor = System.Drawing.Color.Black;
			this.deleteLabel.Name = "deleteLabel";
			this.deleteLabel.Size = new System.Drawing.Size(66, 28);
			this.deleteLabel.Text = "Delete:";
			this.deleteLabel.Click += new System.EventHandler(this.deleteLabel_Click);
			// 
			// deleteButton
			// 
			this.deleteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.deleteButton.Image = global::River.OneMoreAddIn.Properties.Resources.Delete;
			this.deleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(34, 28);
			this.deleteButton.Text = "Delete";
			this.deleteButton.Click += new System.EventHandler(this.DeleteItem);
			// 
			// nameColumn
			// 
			this.nameColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.nameColumn.DataPropertyName = "Name";
			dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Black;
			this.nameColumn.DefaultCellStyle = dataGridViewCellStyle1;
			this.nameColumn.HeaderText = "Name";
			this.nameColumn.MinimumWidth = 100;
			this.nameColumn.Name = "nameColumn";
			this.nameColumn.ReadOnly = true;
			// 
			// cmdColumn
			// 
			this.cmdColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			this.cmdColumn.DataPropertyName = "FullCommand";
			dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Black;
			this.cmdColumn.DefaultCellStyle = dataGridViewCellStyle2;
			this.cmdColumn.HeaderText = "Command";
			this.cmdColumn.MinimumWidth = 8;
			this.cmdColumn.Name = "cmdColumn";
			this.cmdColumn.ReadOnly = true;
			// 
			// introBox
			// 
			this.introBox.AutoSize = true;
			this.introBox.Dock = System.Windows.Forms.DockStyle.Top;
			this.introBox.Location = new System.Drawing.Point(10, 9);
			this.introBox.Name = "introBox";
			this.introBox.Padding = new System.Windows.Forms.Padding(0, 0, 0, 36);
			this.introBox.Size = new System.Drawing.Size(780, 56);
			this.introBox.TabIndex = 0;
			this.introBox.Text = "Click the edit button to modify a plugin or double-click its name to rename it. C" +
    "hanges are saved immediately";
			this.introBox.ThemedBack = "ControlLightLight";
			this.introBox.ThemedFore = null;
			// 
			// toolStrip
			// 
			this.toolStrip.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
			this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.toolStrip.Location = new System.Drawing.Point(10, 65);
			this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.renameButton,
			this.deleteButton});
			this.toolStrip.Name = "toolStrip";
			this.toolStrip.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
			this.toolStrip.Size = new System.Drawing.Size(780, 28);
			this.toolStrip.Stretch = true;
			this.toolStrip.TabIndex = 4;
			this.toolStrip.Text = "Tool Strip";
			// 
			// renameButton
			// 
			this.renameButton.Image = global::River.OneMoreAddIn.Properties.Resources.m_Rename;
			this.renameButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.renameButton.Name = "renameButton";
			this.renameButton.Size = new System.Drawing.Size(115, 38);
			this.renameButton.Text = "Rename";
			this.renameButton.Click += new System.EventHandler(this.EditSelection);
			// 
			// deleteButton
			// 
			this.deleteButton.Image = global::River.OneMoreAddIn.Properties.Resources.m_Delete;
			this.deleteButton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.deleteButton.Name = "deleteButton";
			this.deleteButton.Size = new System.Drawing.Size(102, 38);
			this.deleteButton.Text = "Delete";
			this.deleteButton.Click += new System.EventHandler(this.DeleteItem);
			// 
			// PluginsSheet
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.Controls.Add(this.gridView);
			this.Controls.Add(this.toolStrip);
			this.Controls.Add(this.introBox);
			this.ForeColor = System.Drawing.SystemColors.ControlText;
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.MinimumSize = new System.Drawing.Size(750, 400);
			this.Name = "PluginsSheet";
			this.Padding = new System.Windows.Forms.Padding(10, 9, 10, 9);
			this.Size = new System.Drawing.Size(800, 500);
			((System.ComponentModel.ISupportInitialize)(this.gridView)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DataGridView gridView;
		private UI.ScaledToolStrip toolStrip;
		private System.Windows.Forms.ToolStripButton runButton;
		private System.Windows.Forms.ToolStripLabel runLabel;
		private System.Windows.Forms.ToolStripButton deleteButton;
		private System.Windows.Forms.ToolStripLabel deleteLabel;
		private UI.MoreMultilineLabel introBox;
		private UI.MoreMenuItem renameButton;
		private System.Windows.Forms.DataGridViewTextBoxColumn nameColumn;
		private System.Windows.Forms.DataGridViewTextBoxColumn cmdColumn;
	}
}