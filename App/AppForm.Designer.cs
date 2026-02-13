namespace mw2_fps_unlocker
{
    partial class AppForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppForm));
            LabelFPS = new Label();
            LabelFOV = new Label();
            LabelFPSValue = new Label();
            LabelFOVValue = new Label();
            LabelStatusValue = new Label();
            SuspendLayout();
            // 
            // LabelFPS
            // 
            LabelFPS.AutoSize = true;
            LabelFPS.Location = new Point(14, 53);
            LabelFPS.Name = "LabelFPS";
            LabelFPS.Size = new Size(35, 20);
            LabelFPS.TabIndex = 0;
            LabelFPS.Text = "FPS:";
            // 
            // LabelFOV
            // 
            LabelFOV.AutoSize = true;
            LabelFOV.Location = new Point(14, 108);
            LabelFOV.Name = "LabelFOV";
            LabelFOV.Size = new Size(39, 20);
            LabelFOV.TabIndex = 1;
            LabelFOV.Text = "FOV:";
            // 
            // LabelFPSValue
            // 
            LabelFPSValue.AutoSize = true;
            LabelFPSValue.Location = new Point(237, 53);
            LabelFPSValue.Name = "LabelFPSValue";
            LabelFPSValue.Size = new Size(33, 20);
            LabelFPSValue.TabIndex = 4;
            LabelFPSValue.Text = "300";
            // 
            // LabelFOVValue
            // 
            LabelFOVValue.AutoSize = true;
            LabelFOVValue.Location = new Point(237, 108);
            LabelFOVValue.Name = "LabelFOVValue";
            LabelFOVValue.Size = new Size(25, 20);
            LabelFOVValue.TabIndex = 6;
            LabelFOVValue.Text = "80";
            // 
            // LabelStatusValue
            // 
            LabelStatusValue.AutoSize = true;
            LabelStatusValue.ForeColor = Color.RoyalBlue;
            LabelStatusValue.Location = new Point(14, 16);
            LabelStatusValue.Name = "LabelStatusValue";
            LabelStatusValue.Size = new Size(95, 20);
            LabelStatusValue.TabIndex = 8;
            LabelStatusValue.Text = "Not In-Game";
            // 
            // AppForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(282, 179);
            Controls.Add(LabelStatusValue);
            Controls.Add(LabelFOVValue);
            Controls.Add(LabelFPSValue);
            Controls.Add(LabelFOV);
            Controls.Add(LabelFPS);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 4, 3, 4);
            Name = "AppForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "FPS Unlocker";
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private Label LabelFPS;
        private Label LabelFOV;
        private Label LabelFPSValue;
        private Label LabelFOVValue;
        private Label LabelStatusValue;
    }
}