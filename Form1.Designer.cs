namespace WinFormsApp3
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            lblStatus = new Label();
            lstRepos = new ReaLTaiizor.Controls.MaterialListView();
            btnLogout = new ReaLTaiizor.Controls.MaterialButton();
            panelActionbar = new Panel();
            materialButton1 = new ReaLTaiizor.Controls.MaterialButton();
            materialButton3 = new ReaLTaiizor.Controls.MaterialButton();
            materialButton2 = new ReaLTaiizor.Controls.MaterialButton();
            panelActionbar.SuspendLayout();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(19, 3);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(22, 15);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "-/-";
            // 
            // lstRepos
            // 
            lstRepos.AutoSizeTable = false;
            lstRepos.BackColor = Color.FromArgb(255, 255, 255);
            lstRepos.BorderStyle = BorderStyle.None;
            lstRepos.Depth = 0;
            lstRepos.Dock = DockStyle.Fill;
            lstRepos.FullRowSelect = true;
            lstRepos.Location = new Point(3, 127);
            lstRepos.MinimumSize = new Size(200, 100);
            lstRepos.MouseLocation = new Point(-1, -1);
            lstRepos.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.OUT;
            lstRepos.Name = "lstRepos";
            lstRepos.OwnerDraw = true;
            lstRepos.Size = new Size(1665, 892);
            lstRepos.TabIndex = 6;
            lstRepos.UseCompatibleStateImageBehavior = false;
            lstRepos.View = View.Details;
            // 
            // btnLogout
            // 
            btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnLogout.AutoSize = false;
            btnLogout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnLogout.BackColor = Color.Transparent;
            btnLogout.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnLogout.Depth = 0;
            btnLogout.FlatStyle = FlatStyle.Flat;
            btnLogout.HighEmphasis = true;
            btnLogout.Icon = null;
            btnLogout.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            btnLogout.Location = new Point(1514, 11);
            btnLogout.Margin = new Padding(4, 6, 4, 6);
            btnLogout.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            btnLogout.Name = "btnLogout";
            btnLogout.NoAccentTextColor = Color.Empty;
            btnLogout.Size = new Size(137, 36);
            btnLogout.TabIndex = 0;
            btnLogout.Text = "Ausloggen";
            btnLogout.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Text;
            btnLogout.UseAccentColor = false;
            btnLogout.UseVisualStyleBackColor = false;
            btnLogout.Click += btnLogout_Click;
            // 
            // panelActionbar
            // 
            panelActionbar.BackColor = Color.Transparent;
            panelActionbar.Controls.Add(materialButton1);
            panelActionbar.Controls.Add(btnLogout);
            panelActionbar.Controls.Add(materialButton3);
            panelActionbar.Controls.Add(materialButton2);
            panelActionbar.Dock = DockStyle.Top;
            panelActionbar.Location = new Point(3, 64);
            panelActionbar.Name = "panelActionbar";
            panelActionbar.Size = new Size(1665, 63);
            panelActionbar.TabIndex = 8;
            // 
            // materialButton1
            // 
            materialButton1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            materialButton1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            materialButton1.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            materialButton1.Depth = 0;
            materialButton1.HighEmphasis = true;
            materialButton1.Icon = null;
            materialButton1.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            materialButton1.Location = new Point(1406, 11);
            materialButton1.Margin = new Padding(4, 6, 4, 6);
            materialButton1.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            materialButton1.Name = "materialButton1";
            materialButton1.NoAccentTextColor = Color.Empty;
            materialButton1.Size = new Size(79, 36);
            materialButton1.TabIndex = 11;
            materialButton1.Text = "Suchen";
            materialButton1.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Text;
            materialButton1.UseAccentColor = false;
            materialButton1.UseVisualStyleBackColor = true;
            materialButton1.Click += btnSearch_Click;
            // 
            // materialButton3
            // 
            materialButton3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            materialButton3.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            materialButton3.Depth = 0;
            materialButton3.HighEmphasis = true;
            materialButton3.Icon = null;
            materialButton3.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            materialButton3.Location = new Point(117, 11);
            materialButton3.Margin = new Padding(4, 6, 4, 6);
            materialButton3.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            materialButton3.Name = "materialButton3";
            materialButton3.NoAccentTextColor = Color.Empty;
            materialButton3.Size = new Size(88, 36);
            materialButton3.TabIndex = 10;
            materialButton3.Text = "Löschen";
            materialButton3.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Text;
            materialButton3.UseAccentColor = false;
            materialButton3.UseVisualStyleBackColor = true;
            materialButton3.Click += BtnDelete_Click;
            // 
            // materialButton2
            // 
            materialButton2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            materialButton2.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            materialButton2.Depth = 0;
            materialButton2.HighEmphasis = true;
            materialButton2.Icon = null;
            materialButton2.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            materialButton2.Location = new Point(16, 11);
            materialButton2.Margin = new Padding(4, 6, 4, 6);
            materialButton2.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            materialButton2.Name = "materialButton2";
            materialButton2.NoAccentTextColor = Color.Empty;
            materialButton2.Size = new Size(64, 36);
            materialButton2.TabIndex = 10;
            materialButton2.Text = "Neu";
            materialButton2.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Text;
            materialButton2.UseAccentColor = false;
            materialButton2.UseVisualStyleBackColor = true;
            materialButton2.Click += BtnNew_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1671, 1022);
            Controls.Add(lstRepos);
            Controls.Add(panelActionbar);
            Controls.Add(lblStatus);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "BBS-ME File Explorer";
            Load += Form1_Load;
            panelActionbar.ResumeLayout(false);
            panelActionbar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label lblStatus;
        private ReaLTaiizor.Controls.MaterialListView lstRepos;
        private Panel panelActionbar;
        private ReaLTaiizor.Controls.MaterialButton btnLogout;
        private ReaLTaiizor.Controls.MaterialButton materialButton3;
        private ReaLTaiizor.Controls.MaterialButton materialButton2;
        private ReaLTaiizor.Controls.MaterialButton materialButton1;
    }
}
