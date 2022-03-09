
namespace River.OneMoreAddIn.Commands
{
	partial class ImportWebDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportWebDialog));
            this.addressLabel = new System.Windows.Forms.Label();
            this.addressBox = new System.Windows.Forms.TextBox();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.appendButton = new System.Windows.Forms.RadioButton();
            this.newPageButton = new System.Windows.Forms.RadioButton();
            this.newChildButton = new System.Windows.Forms.RadioButton();
            this.imagesBox = new System.Windows.Forms.CheckBox();
            this.checkBox_EnableDebug = new System.Windows.Forms.CheckBox();
            this.ImportMD = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // addressLabel
            // 
            this.addressLabel.AutoSize = true;
            this.addressLabel.Location = new System.Drawing.Point(16, 24);
            this.addressLabel.Name = "addressLabel";
            this.addressLabel.Size = new System.Drawing.Size(60, 17);
            this.addressLabel.TabIndex = 0;
            this.addressLabel.Text = "Address";
            // 
            // addressBox
            // 
            this.addressBox.Location = new System.Drawing.Point(82, 22);
            this.addressBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 12);
            this.addressBox.Name = "addressBox";
            this.addressBox.Size = new System.Drawing.Size(488, 22);
            this.addressBox.TabIndex = 1;
            this.addressBox.TextChanged += new System.EventHandler(this.addressBox_TextChanged);
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Enabled = false;
            this.okButton.Location = new System.Drawing.Point(386, 168);
            this.okButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(89, 30);
            this.okButton.TabIndex = 9;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(480, 168);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(89, 30);
            this.cancelButton.TabIndex = 8;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // appendButton
            // 
            this.appendButton.AutoSize = true;
            this.appendButton.Location = new System.Drawing.Point(82, 137);
            this.appendButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.appendButton.Name = "appendButton";
            this.appendButton.Size = new System.Drawing.Size(179, 21);
            this.appendButton.TabIndex = 10;
            this.appendButton.Text = "Append to current page";
            this.appendButton.UseVisualStyleBackColor = true;
            // 
            // newPageButton
            // 
            this.newPageButton.AutoSize = true;
            this.newPageButton.Checked = true;
            this.newPageButton.Location = new System.Drawing.Point(82, 89);
            this.newPageButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.newPageButton.Name = "newPageButton";
            this.newPageButton.Size = new System.Drawing.Size(155, 21);
            this.newPageButton.TabIndex = 11;
            this.newPageButton.TabStop = true;
            this.newPageButton.Text = "Create as new page";
            this.newPageButton.UseVisualStyleBackColor = true;
            // 
            // newChildButton
            // 
            this.newChildButton.AutoSize = true;
            this.newChildButton.Location = new System.Drawing.Point(82, 113);
            this.newChildButton.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.newChildButton.Name = "newChildButton";
            this.newChildButton.Size = new System.Drawing.Size(253, 21);
            this.newChildButton.TabIndex = 12;
            this.newChildButton.Text = "Create as new child of current page";
            this.newChildButton.UseVisualStyleBackColor = true;
            // 
            // imagesBox
            // 
            this.imagesBox.AutoSize = true;
            this.imagesBox.Location = new System.Drawing.Point(82, 57);
            this.imagesBox.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.imagesBox.Name = "imagesBox";
            this.imagesBox.Size = new System.Drawing.Size(174, 21);
            this.imagesBox.TabIndex = 13;
            this.imagesBox.Text = "Import as static images";
            this.imagesBox.UseVisualStyleBackColor = true;
            // 
            // checkBox_EnableDebug
            // 
            this.checkBox_EnableDebug.AutoSize = true;
            this.checkBox_EnableDebug.Location = new System.Drawing.Point(386, 58);
            this.checkBox_EnableDebug.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBox_EnableDebug.Name = "checkBox_EnableDebug";
            this.checkBox_EnableDebug.Size = new System.Drawing.Size(120, 21);
            this.checkBox_EnableDebug.TabIndex = 14;
            this.checkBox_EnableDebug.Text = "Enable Debug";
            this.checkBox_EnableDebug.UseVisualStyleBackColor = true;
            this.checkBox_EnableDebug.CheckedChanged += new System.EventHandler(this.checkBox_EnableDebug_CheckedChanged);
            // 
            // ImportMD
            // 
            this.ImportMD.AutoSize = true;
            this.ImportMD.Location = new System.Drawing.Point(386, 90);
            this.ImportMD.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ImportMD.Name = "ImportMD";
            this.ImportMD.Size = new System.Drawing.Size(94, 21);
            this.ImportMD.TabIndex = 15;
            this.ImportMD.Text = "Import MD";
            this.ImportMD.UseVisualStyleBackColor = true;
            this.ImportMD.CheckedChanged += new System.EventHandler(this.ImportMD_CheckedChanged);
            // 
            // ImportWebDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(585, 213);
            this.Controls.Add(this.ImportMD);
            this.Controls.Add(this.checkBox_EnableDebug);
            this.Controls.Add(this.imagesBox);
            this.Controls.Add(this.newChildButton);
            this.Controls.Add(this.newPageButton);
            this.Controls.Add(this.appendButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.addressBox);
            this.Controls.Add(this.addressLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ImportWebDialog";
            this.Padding = new System.Windows.Forms.Padding(13, 12, 13, 12);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Import Web Page";
            this.Load += new System.EventHandler(this.ImportWebDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label addressLabel;
		private System.Windows.Forms.TextBox addressBox;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.RadioButton appendButton;
		private System.Windows.Forms.RadioButton newPageButton;
		private System.Windows.Forms.RadioButton newChildButton;
		private System.Windows.Forms.CheckBox imagesBox;
        private System.Windows.Forms.CheckBox checkBox_EnableDebug;
        private System.Windows.Forms.CheckBox ImportMD;
    }
}