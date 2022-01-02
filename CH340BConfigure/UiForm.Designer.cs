
namespace CH340BConfigure
{
    partial class UiForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UiForm));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonUpdateDevList = new System.Windows.Forms.Button();
            this.comboBoxDeviceList = new System.Windows.Forms.ComboBox();
            this.groupBoxConfigControls = new System.Windows.Forms.GroupBox();
            this.labelStatus = new System.Windows.Forms.Label();
            this.buttonWrite = new System.Windows.Forms.Button();
            this.buttonRead = new System.Windows.Forms.Button();
            this.textBoxSerialNumber = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxProductString = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxPid = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxVid = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2.SuspendLayout();
            this.groupBoxConfigControls.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.buttonUpdateDevList);
            this.groupBox2.Controls.Add(this.comboBoxDeviceList);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(367, 55);
            this.groupBox2.TabIndex = 22;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "CH340B Devices";
            // 
            // buttonUpdateDevList
            // 
            this.buttonUpdateDevList.Location = new System.Drawing.Point(270, 19);
            this.buttonUpdateDevList.Name = "buttonUpdateDevList";
            this.buttonUpdateDevList.Size = new System.Drawing.Size(85, 23);
            this.buttonUpdateDevList.TabIndex = 1;
            this.buttonUpdateDevList.Text = "Update";
            this.buttonUpdateDevList.UseVisualStyleBackColor = true;
            this.buttonUpdateDevList.Click += new System.EventHandler(this.buttonUpdateDevList_Click);
            // 
            // comboBoxDeviceList
            // 
            this.comboBoxDeviceList.AllowDrop = true;
            this.comboBoxDeviceList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxDeviceList.FormattingEnabled = true;
            this.comboBoxDeviceList.Location = new System.Drawing.Point(12, 20);
            this.comboBoxDeviceList.Name = "comboBoxDeviceList";
            this.comboBoxDeviceList.Size = new System.Drawing.Size(249, 21);
            this.comboBoxDeviceList.TabIndex = 0;
            this.comboBoxDeviceList.SelectedIndexChanged += new System.EventHandler(this.comboBoxDeviceList_SelectedIndexChanged);
            // 
            // groupBoxConfigControls
            // 
            this.groupBoxConfigControls.Controls.Add(this.labelStatus);
            this.groupBoxConfigControls.Controls.Add(this.buttonWrite);
            this.groupBoxConfigControls.Controls.Add(this.buttonRead);
            this.groupBoxConfigControls.Controls.Add(this.textBoxSerialNumber);
            this.groupBoxConfigControls.Controls.Add(this.label3);
            this.groupBoxConfigControls.Controls.Add(this.textBoxProductString);
            this.groupBoxConfigControls.Controls.Add(this.label4);
            this.groupBoxConfigControls.Controls.Add(this.textBoxPid);
            this.groupBoxConfigControls.Controls.Add(this.label2);
            this.groupBoxConfigControls.Controls.Add(this.textBoxVid);
            this.groupBoxConfigControls.Controls.Add(this.label1);
            this.groupBoxConfigControls.Location = new System.Drawing.Point(12, 76);
            this.groupBoxConfigControls.Name = "groupBoxConfigControls";
            this.groupBoxConfigControls.Size = new System.Drawing.Size(367, 203);
            this.groupBoxConfigControls.TabIndex = 23;
            this.groupBoxConfigControls.TabStop = false;
            this.groupBoxConfigControls.Text = "Configure CH340B";
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(9, 170);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(40, 13);
            this.labelStatus.TabIndex = 10;
            this.labelStatus.Text = "Status:";
            // 
            // buttonWrite
            // 
            this.buttonWrite.Location = new System.Drawing.Point(270, 165);
            this.buttonWrite.Name = "buttonWrite";
            this.buttonWrite.Size = new System.Drawing.Size(85, 23);
            this.buttonWrite.TabIndex = 9;
            this.buttonWrite.Text = "Write";
            this.buttonWrite.UseVisualStyleBackColor = true;
            this.buttonWrite.Click += new System.EventHandler(this.buttonWrite_Click);
            // 
            // buttonRead
            // 
            this.buttonRead.Location = new System.Drawing.Point(176, 165);
            this.buttonRead.Name = "buttonRead";
            this.buttonRead.Size = new System.Drawing.Size(85, 23);
            this.buttonRead.TabIndex = 8;
            this.buttonRead.Text = "Read";
            this.buttonRead.UseVisualStyleBackColor = true;
            this.buttonRead.Click += new System.EventHandler(this.buttonRead_Click);
            // 
            // textBoxSerialNumber
            // 
            this.textBoxSerialNumber.Location = new System.Drawing.Point(139, 130);
            this.textBoxSerialNumber.MaxLength = 8;
            this.textBoxSerialNumber.Name = "textBoxSerialNumber";
            this.textBoxSerialNumber.Size = new System.Drawing.Size(216, 20);
            this.textBoxSerialNumber.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 133);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(117, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Serial Number (8 chars)";
            // 
            // textBoxProductString
            // 
            this.textBoxProductString.Location = new System.Drawing.Point(139, 95);
            this.textBoxProductString.MaxLength = 18;
            this.textBoxProductString.Name = "textBoxProductString";
            this.textBoxProductString.Size = new System.Drawing.Size(216, 20);
            this.textBoxProductString.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 98);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(124, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Product String (18 chars)";
            // 
            // textBoxPid
            // 
            this.textBoxPid.Location = new System.Drawing.Point(139, 60);
            this.textBoxPid.Name = "textBoxPid";
            this.textBoxPid.Size = new System.Drawing.Size(216, 20);
            this.textBoxPid.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(99, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Product ID (PID) 0x";
            // 
            // textBoxVid
            // 
            this.textBoxVid.Location = new System.Drawing.Point(139, 25);
            this.textBoxVid.Name = "textBoxVid";
            this.textBoxVid.Size = new System.Drawing.Size(216, 20);
            this.textBoxVid.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(96, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Vendor ID (VID) 0x";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(392, 291);
            this.Controls.Add(this.groupBoxConfigControls);
            this.Controls.Add(this.groupBox2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CH340B Configuration Utility";
            this.groupBox2.ResumeLayout(false);
            this.groupBoxConfigControls.ResumeLayout(false);
            this.groupBoxConfigControls.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonUpdateDevList;
        private System.Windows.Forms.ComboBox comboBoxDeviceList;
        private System.Windows.Forms.GroupBox groupBoxConfigControls;
        private System.Windows.Forms.TextBox textBoxVid;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonWrite;
        private System.Windows.Forms.Button buttonRead;
        private System.Windows.Forms.TextBox textBoxSerialNumber;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxProductString;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxPid;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label labelStatus;
    }
}

