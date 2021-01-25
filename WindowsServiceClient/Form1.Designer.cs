namespace WindowsServiceClient
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.button_install = new System.Windows.Forms.Button();
            this.button_uninstall = new System.Windows.Forms.Button();
            this.button_start = new System.Windows.Forms.Button();
            this.button_stop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button_install
            // 
            this.button_install.Location = new System.Drawing.Point(33, 64);
            this.button_install.Name = "button_install";
            this.button_install.Size = new System.Drawing.Size(75, 23);
            this.button_install.TabIndex = 0;
            this.button_install.Text = "安装服务";
            this.button_install.UseVisualStyleBackColor = true;
            // 
            // button_uninstall
            // 
            this.button_uninstall.Location = new System.Drawing.Point(167, 64);
            this.button_uninstall.Name = "button_uninstall";
            this.button_uninstall.Size = new System.Drawing.Size(75, 23);
            this.button_uninstall.TabIndex = 1;
            this.button_uninstall.Text = "卸载服务";
            this.button_uninstall.UseVisualStyleBackColor = true;
            // 
            // button_start
            // 
            this.button_start.Location = new System.Drawing.Point(33, 126);
            this.button_start.Name = "button_start";
            this.button_start.Size = new System.Drawing.Size(75, 23);
            this.button_start.TabIndex = 0;
            this.button_start.Text = "启动服务";
            this.button_start.UseVisualStyleBackColor = true;
            // 
            // button_stop
            // 
            this.button_stop.Location = new System.Drawing.Point(167, 126);
            this.button_stop.Name = "button_stop";
            this.button_stop.Size = new System.Drawing.Size(75, 23);
            this.button_stop.TabIndex = 0;
            this.button_stop.Text = "停止服务";
            this.button_stop.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.button_uninstall);
            this.Controls.Add(this.button_stop);
            this.Controls.Add(this.button_start);
            this.Controls.Add(this.button_install);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button_install;
        private System.Windows.Forms.Button button_uninstall;
        private System.Windows.Forms.Button button_start;
        private System.Windows.Forms.Button button_stop;
    }
}

