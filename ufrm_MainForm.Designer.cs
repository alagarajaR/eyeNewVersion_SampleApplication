
namespace SampleApplication
{
    partial class ufrm_MainForm
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
            this.btn_OpenCamera = new System.Windows.Forms.Button();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            this.counterLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_OpenCamera
            // 
            this.btn_OpenCamera.Location = new System.Drawing.Point(12, 12);
            this.btn_OpenCamera.Name = "btn_OpenCamera";
            this.btn_OpenCamera.Size = new System.Drawing.Size(117, 32);
            this.btn_OpenCamera.TabIndex = 1;
            this.btn_OpenCamera.Text = "Play Video";
            this.btn_OpenCamera.UseVisualStyleBackColor = true;
            this.btn_OpenCamera.Click += new System.EventHandler(this.btn_OpenCamera_Click);
            // 
            // pictureBox
            // 
            this.pictureBox.Location = new System.Drawing.Point(7, 50);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(1044, 585);
            this.pictureBox.TabIndex = 2;
            this.pictureBox.TabStop = false;
            // 
            // counterLabel
            // 
            this.counterLabel.AutoSize = true;
            this.counterLabel.Location = new System.Drawing.Point(30, 658);
            this.counterLabel.Name = "counterLabel";
            this.counterLabel.Size = new System.Drawing.Size(89, 17);
            this.counterLabel.TabIndex = 3;
            this.counterLabel.Text = "Frame Count";
            // 
            // ufrm_MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1057, 698);
            this.Controls.Add(this.counterLabel);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.btn_OpenCamera);
            this.MaximizeBox = false;
            this.Name = "ufrm_MainForm";
            this.Text = "Sample IDS uEye Capture Application";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ufrm_MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.ufrm_MainForm_FormClosed);
            this.Load += new System.EventHandler(this.ufrm_MainForm_Load);
            this.Shown += new System.EventHandler(this.ufrm_MainForm_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button btn_OpenCamera;
        private System.Windows.Forms.PictureBox pictureBox;
        private System.Windows.Forms.Label counterLabel;
    }
}

