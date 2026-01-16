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
            panelSIdebar = new Panel();
            parrotPictureBoxLogo = new ReaLTaiizor.Controls.ParrotPictureBox();
            btnLogout = new ReaLTaiizor.Controls.MaterialButton();
            panelActionbar = new Panel();
            materialButton3 = new ReaLTaiizor.Controls.MaterialButton();
            materialButton2 = new ReaLTaiizor.Controls.MaterialButton();
            materialButton1 = new ReaLTaiizor.Controls.MaterialButton();
            panelSIdebar.SuspendLayout();
            panelActionbar.SuspendLayout();
            SuspendLayout();
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(566, 3);
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
            lstRepos.Location = new Point(150, 87);
            lstRepos.MinimumSize = new Size(200, 100);
            lstRepos.MouseLocation = new Point(-1, -1);
            lstRepos.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.OUT;
            lstRepos.Name = "lstRepos";
            lstRepos.OwnerDraw = true;
            lstRepos.Size = new Size(1472, 790);
            lstRepos.TabIndex = 6;
            lstRepos.UseCompatibleStateImageBehavior = false;
            lstRepos.View = View.Details;
            // 
            // panelSIdebar
            // 
            panelSIdebar.BackColor = SystemColors.ControlDarkDark;
            panelSIdebar.Controls.Add(parrotPictureBoxLogo);
            panelSIdebar.Controls.Add(btnLogout);
            panelSIdebar.Dock = DockStyle.Left;
            panelSIdebar.Location = new Point(3, 24);
            panelSIdebar.Name = "panelSIdebar";
            panelSIdebar.Size = new Size(147, 853);
            panelSIdebar.TabIndex = 7;
            // 
            // parrotPictureBoxLogo
            // 
            parrotPictureBoxLogo.BackColor = Color.Transparent;
            parrotPictureBoxLogo.BackgroundImage = Properties.Resources.cover_bbs_removebg_preview;
            parrotPictureBoxLogo.BackgroundImageLayout = ImageLayout.Zoom;
            parrotPictureBoxLogo.ColorLeft = Color.DodgerBlue;
            parrotPictureBoxLogo.ColorRight = Color.DodgerBlue;
            parrotPictureBoxLogo.CompositingQualityType = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            parrotPictureBoxLogo.FilterAlpha = 200;
            parrotPictureBoxLogo.FilterEnabled = true;
            parrotPictureBoxLogo.Image = null;
            parrotPictureBoxLogo.InterpolationType = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            parrotPictureBoxLogo.IsElipse = false;
            parrotPictureBoxLogo.IsParallax = false;
            parrotPictureBoxLogo.Location = new Point(22, 15);
            parrotPictureBoxLogo.Name = "parrotPictureBoxLogo";
            parrotPictureBoxLogo.PixelOffsetType = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            parrotPictureBoxLogo.Size = new Size(93, 69);
            parrotPictureBoxLogo.SmoothingType = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            parrotPictureBoxLogo.TabIndex = 9;
            parrotPictureBoxLogo.Text = "parrotPictureBox1";
            parrotPictureBoxLogo.TextRenderingType = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            // 
            // btnLogout
            // 
            btnLogout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnLogout.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            btnLogout.Depth = 0;
            btnLogout.HighEmphasis = true;
            btnLogout.Icon = null;
            btnLogout.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            btnLogout.Location = new Point(17, 802);
            btnLogout.Margin = new Padding(4, 6, 4, 6);
            btnLogout.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            btnLogout.Name = "btnLogout";
            btnLogout.NoAccentTextColor = Color.Empty;
            btnLogout.Size = new Size(107, 36);
            btnLogout.TabIndex = 0;
            btnLogout.Text = "Ausloggen";
            btnLogout.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Contained;
            btnLogout.UseAccentColor = false;
            btnLogout.UseVisualStyleBackColor = true;
            btnLogout.Click += btnLogout_Click;
            // 
            // panelActionbar
            // 
            panelActionbar.BackColor = SystemColors.ActiveBorder;
            panelActionbar.Controls.Add(materialButton1);
            panelActionbar.Controls.Add(materialButton3);
            panelActionbar.Controls.Add(materialButton2);
            panelActionbar.Dock = DockStyle.Top;
            panelActionbar.Location = new Point(150, 24);
            panelActionbar.Name = "panelActionbar";
            panelActionbar.Size = new Size(1472, 63);
            panelActionbar.TabIndex = 8;
            // 
            // materialButton3
            // 
            materialButton3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            materialButton3.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            materialButton3.Depth = 0;
            materialButton3.HighEmphasis = true;
            materialButton3.Icon = Properties.Resources.icons8_datei_löschen_40;
            materialButton3.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            materialButton3.Location = new Point(118, 15);
            materialButton3.Margin = new Padding(4, 6, 4, 6);
            materialButton3.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            materialButton3.Name = "materialButton3";
            materialButton3.NoAccentTextColor = Color.Empty;
            materialButton3.Size = new Size(116, 36);
            materialButton3.TabIndex = 10;
            materialButton3.Text = "Löschen";
            materialButton3.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Outlined;
            materialButton3.UseAccentColor = false;
            materialButton3.UseVisualStyleBackColor = true;
            // 
            // materialButton2
            // 
            materialButton2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            materialButton2.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            materialButton2.Depth = 0;
            materialButton2.HighEmphasis = true;
            materialButton2.Icon = Properties.Resources.icons8_datei_hinzufügen_40;
            materialButton2.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            materialButton2.Location = new Point(17, 15);
            materialButton2.Margin = new Padding(4, 6, 4, 6);
            materialButton2.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            materialButton2.Name = "materialButton2";
            materialButton2.NoAccentTextColor = Color.Empty;
            materialButton2.Size = new Size(78, 36);
            materialButton2.TabIndex = 10;
            materialButton2.Text = "Neu";
            materialButton2.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Outlined;
            materialButton2.UseAccentColor = false;
            materialButton2.UseVisualStyleBackColor = true;
            // 
            // materialButton1
            // 
            materialButton1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            materialButton1.Density = ReaLTaiizor.Controls.MaterialButton.MaterialButtonDensity.Default;
            materialButton1.Depth = 0;
            materialButton1.HighEmphasis = true;
            materialButton1.Icon = (Image)resources.GetObject("materialButton1.Icon");
            materialButton1.IconType = ReaLTaiizor.Controls.MaterialButton.MaterialIconType.Rebase;
            materialButton1.Location = new Point(1352, 15);
            materialButton1.Margin = new Padding(4, 6, 4, 6);
            materialButton1.MouseState = ReaLTaiizor.Helper.MaterialDrawHelper.MaterialMouseState.HOVER;
            materialButton1.Name = "materialButton1";
            materialButton1.NoAccentTextColor = Color.Empty;
            materialButton1.Size = new Size(107, 36);
            materialButton1.TabIndex = 11;
            materialButton1.Text = "Suchen";
            materialButton1.Type = ReaLTaiizor.Controls.MaterialButton.MaterialButtonType.Outlined;
            materialButton1.UseAccentColor = false;
            materialButton1.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1625, 880);
            Controls.Add(lstRepos);
            Controls.Add(panelActionbar);
            Controls.Add(panelSIdebar);
            Controls.Add(lblStatus);
            FormStyle = ReaLTaiizor.Enum.Material.FormStyles.ActionBar_None;
            Name = "Form1";
            Padding = new Padding(3, 24, 3, 3);
            Text = "Seafile Explorer";
            Load += Form1_Load;
            panelSIdebar.ResumeLayout(false);
            panelSIdebar.PerformLayout();
            panelActionbar.ResumeLayout(false);
            panelActionbar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label lblStatus;
        private ReaLTaiizor.Controls.MaterialListView lstRepos;
        private Panel panelSIdebar;
        private Panel panelActionbar;
        private ReaLTaiizor.Controls.MaterialButton btnLogout;
        private ReaLTaiizor.Controls.ParrotPictureBox parrotPictureBoxLogo;
        private ReaLTaiizor.Controls.MaterialButton materialButton3;
        private ReaLTaiizor.Controls.MaterialButton materialButton2;
        private ReaLTaiizor.Controls.MaterialButton materialButton1;
    }
}
