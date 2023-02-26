namespace SinkingFeeling
{
    partial class FormMain
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
            pictureBoxDroneCam = new PictureBox();
            label1 = new Label();
            panelMap = new Panel();
            pictureBoxMap = new PictureBox();
            panelDroneCam = new Panel();
            buttonLaunchDrone = new Button();
            buttonRemoveWaypoint = new Button();
            buttonAddWayPoint = new Button();
            panelTelemetry = new Panel();
            listBoxDroneCam = new ListBox();
            ((System.ComponentModel.ISupportInitialize)pictureBoxDroneCam).BeginInit();
            panelMap.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxMap).BeginInit();
            panelDroneCam.SuspendLayout();
            panelTelemetry.SuspendLayout();
            SuspendLayout();
            // 
            // pictureBoxDroneCam
            // 
            pictureBoxDroneCam.Dock = DockStyle.Bottom;
            pictureBoxDroneCam.Location = new Point(10, 22);
            pictureBoxDroneCam.Name = "pictureBoxDroneCam";
            pictureBoxDroneCam.Size = new Size(400, 240);
            pictureBoxDroneCam.TabIndex = 0;
            pictureBoxDroneCam.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point);
            label1.ForeColor = Color.White;
            label1.Location = new Point(5, -1);
            label1.Name = "label1";
            label1.Size = new Size(91, 21);
            label1.TabIndex = 4;
            label1.Text = "Drone Cam";
            // 
            // panelMap
            // 
            panelMap.Controls.Add(pictureBoxMap);
            panelMap.Dock = DockStyle.Left;
            panelMap.Location = new Point(5, 5);
            panelMap.Margin = new Padding(5);
            panelMap.Name = "panelMap";
            panelMap.Size = new Size(721, 615);
            panelMap.TabIndex = 7;
            // 
            // pictureBoxMap
            // 
            pictureBoxMap.Dock = DockStyle.Fill;
            pictureBoxMap.Location = new Point(0, 0);
            pictureBoxMap.Name = "pictureBoxMap";
            pictureBoxMap.Size = new Size(721, 615);
            pictureBoxMap.TabIndex = 0;
            pictureBoxMap.TabStop = false;
            // 
            // panelDroneCam
            // 
            panelDroneCam.Controls.Add(buttonLaunchDrone);
            panelDroneCam.Controls.Add(buttonRemoveWaypoint);
            panelDroneCam.Controls.Add(buttonAddWayPoint);
            panelDroneCam.Controls.Add(pictureBoxDroneCam);
            panelDroneCam.Controls.Add(label1);
            panelDroneCam.Dock = DockStyle.Top;
            panelDroneCam.Location = new Point(726, 5);
            panelDroneCam.Name = "panelDroneCam";
            panelDroneCam.Padding = new Padding(10, 0, 10, 5);
            panelDroneCam.Size = new Size(420, 267);
            panelDroneCam.TabIndex = 8;
            // 
            // buttonLaunchDrone
            // 
            buttonLaunchDrone.Cursor = Cursors.Hand;
            buttonLaunchDrone.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            buttonLaunchDrone.Location = new Point(330, 213);
            buttonLaunchDrone.Name = "buttonLaunchDrone";
            buttonLaunchDrone.Size = new Size(70, 39);
            buttonLaunchDrone.TabIndex = 7;
            buttonLaunchDrone.Text = "Launch";
            buttonLaunchDrone.UseVisualStyleBackColor = true;
            buttonLaunchDrone.Click += ButtonLaunchDroneClick;
            // 
            // buttonRemoveWaypoint
            // 
            buttonRemoveWaypoint.Cursor = Cursors.Hand;
            buttonRemoveWaypoint.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            buttonRemoveWaypoint.Location = new Point(95, 29);
            buttonRemoveWaypoint.Name = "buttonRemoveWaypoint";
            buttonRemoveWaypoint.Size = new Size(70, 39);
            buttonRemoveWaypoint.TabIndex = 6;
            buttonRemoveWaypoint.Text = "Remove Waypoint";
            buttonRemoveWaypoint.UseVisualStyleBackColor = true;
            buttonRemoveWaypoint.Click += ButtonRemoveWaypoint_Click;
            // 
            // buttonAddWayPoint
            // 
            buttonAddWayPoint.Cursor = Cursors.Hand;
            buttonAddWayPoint.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            buttonAddWayPoint.Location = new Point(19, 29);
            buttonAddWayPoint.Name = "buttonAddWayPoint";
            buttonAddWayPoint.Size = new Size(70, 39);
            buttonAddWayPoint.TabIndex = 5;
            buttonAddWayPoint.Text = "Add Waypoint";
            buttonAddWayPoint.UseVisualStyleBackColor = true;
            buttonAddWayPoint.Click += ButtonAddWayPoint_Click;
            // 
            // panelTelemetry
            // 
            panelTelemetry.Controls.Add(listBoxDroneCam);
            panelTelemetry.Dock = DockStyle.Fill;
            panelTelemetry.Location = new Point(726, 272);
            panelTelemetry.Name = "panelTelemetry";
            panelTelemetry.Padding = new Padding(10, 5, 10, 0);
            panelTelemetry.Size = new Size(420, 348);
            panelTelemetry.TabIndex = 0;
            // 
            // listBoxDroneCam
            // 
            listBoxDroneCam.BackColor = Color.Black;
            listBoxDroneCam.BorderStyle = BorderStyle.None;
            listBoxDroneCam.CausesValidation = false;
            listBoxDroneCam.Dock = DockStyle.Fill;
            listBoxDroneCam.Font = new Font("Lucida Console", 12F, FontStyle.Regular, GraphicsUnit.Point);
            listBoxDroneCam.ForeColor = Color.White;
            listBoxDroneCam.FormattingEnabled = true;
            listBoxDroneCam.ItemHeight = 16;
            listBoxDroneCam.Items.AddRange(new object[] { "Drone Targeting System v1.0" });
            listBoxDroneCam.Location = new Point(10, 5);
            listBoxDroneCam.Margin = new Padding(5);
            listBoxDroneCam.Name = "listBoxDroneCam";
            listBoxDroneCam.SelectionMode = SelectionMode.None;
            listBoxDroneCam.Size = new Size(400, 343);
            listBoxDroneCam.TabIndex = 0;
            listBoxDroneCam.TabStop = false;
            listBoxDroneCam.UseTabStops = false;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(1151, 625);
            Controls.Add(panelTelemetry);
            Controls.Add(panelDroneCam);
            Controls.Add(panelMap);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FormMain";
            Padding = new Padding(5);
            ShowIcon = false;
            Text = "Sinking Feeling";
            Load += FormMain_Load;
            KeyDown += FormMain_KeyDown;
            ((System.ComponentModel.ISupportInitialize)pictureBoxDroneCam).EndInit();
            panelMap.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxMap).EndInit();
            panelDroneCam.ResumeLayout(false);
            panelDroneCam.PerformLayout();
            panelTelemetry.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBoxDroneCam;
        private Label label1;
        private Panel panelMap;
        private PictureBox pictureBoxMap;
        private Panel panelDroneCam;
        private Panel panelTelemetry;
        private ListBox listBoxDroneCam;
        private Button buttonAddWayPoint;
        private Button buttonLaunchDrone;
        private Button buttonRemoveWaypoint;
    }
}