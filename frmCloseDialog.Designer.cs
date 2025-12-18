namespace TVCast
{
    partial class frmCloseDialog
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
            this.panel1 = new AntdUI.Panel();
            this.lblTip = new AntdUI.Label();
            this.btnExitApp = new AntdUI.Button();
            this.btnMinimize = new AntdUI.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.panel1.BorderWidth = 1F;
            this.panel1.Controls.Add(this.btnMinimize);
            this.panel1.Controls.Add(this.btnExitApp);
            this.panel1.Controls.Add(this.lblTip);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Radius = 4;
            this.panel1.Size = new System.Drawing.Size(294, 125);
            this.panel1.TabIndex = 0;
            this.panel1.Text = "panel1";
            // 
            // lblTip
            // 
            this.lblTip.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTip.Location = new System.Drawing.Point(32, 21);
            this.lblTip.Name = "lblTip";
            this.lblTip.Size = new System.Drawing.Size(238, 29);
            this.lblTip.TabIndex = 0;
            this.lblTip.Text = "您要退出程序还是最小化到托盘？";
            // 
            // btnExitApp
            // 
            this.btnExitApp.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnExitApp.IconSvg = "RollbackOutlined";
            this.btnExitApp.Location = new System.Drawing.Point(32, 85);
            this.btnExitApp.Name = "btnExitApp";
            this.btnExitApp.Radius = 4;
            this.btnExitApp.Size = new System.Drawing.Size(90, 28);
            this.btnExitApp.TabIndex = 1;
            this.btnExitApp.Text = "退出程序";
            this.btnExitApp.Type = AntdUI.TTypeMini.Primary;
            this.btnExitApp.WaveSize = 0;
            this.btnExitApp.Click += new System.EventHandler(this.btnExitApp_Click);
            // 
            // btnMinimize
            // 
            this.btnMinimize.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnMinimize.IconSvg = "MinusSquareOutlined";
            this.btnMinimize.Location = new System.Drawing.Point(166, 85);
            this.btnMinimize.Name = "btnMinimize";
            this.btnMinimize.Radius = 4;
            this.btnMinimize.Size = new System.Drawing.Size(90, 28);
            this.btnMinimize.TabIndex = 1;
            this.btnMinimize.Text = "最小化";
            this.btnMinimize.Type = AntdUI.TTypeMini.Primary;
            this.btnMinimize.WaveSize = 0;
            this.btnMinimize.Click += new System.EventHandler(this.btnMinimize_Click);
            // 
            // frmCloseDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(294, 125);
            this.Controls.Add(this.panel1);
            this.Name = "frmCloseDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmCloseDialog";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private AntdUI.Panel panel1;
        private AntdUI.Label lblTip;
        private AntdUI.Button btnMinimize;
        private AntdUI.Button btnExitApp;
    }
}