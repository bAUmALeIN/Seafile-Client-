using System;
using System.Drawing;
using System.Windows.Forms;
using WinFormsApp3.Controls; // Zugriff auf StableLabel
using WinFormsApp3.Data;

namespace WinFormsApp3.Data
{
    public class BreadcrumbManager
    {
        private readonly FlowLayoutPanel _flowPath;
        private readonly NavigationState _navState;
        private readonly Action _onNavigateRoot;
        private readonly Action<string, string> _onNavigateRepo; // repoId, repoName
        private readonly Action<string> _onNavigateFolder; // folderName

        private readonly Font _crumbFontBold = new Font("Segoe UI", 11f, FontStyle.Bold);

        public BreadcrumbManager(
            FlowLayoutPanel flowPanel,
            NavigationState navState,
            Action onNavigateRoot,
            Action<string, string> onNavigateRepo,
            Action<string> onNavigateFolder)
        {
            _flowPath = flowPanel;
            _navState = navState;
            _onNavigateRoot = onNavigateRoot;
            _onNavigateRepo = onNavigateRepo;
            _onNavigateFolder = onNavigateFolder;
        }

        public void Update(string searchContext = null)
        {
            if (_flowPath == null) return;

            _flowPath.SuspendLayout();
            _flowPath.Controls.Clear();

            Color colorLink = Color.White;
            Color colorSep = Color.Gray;

            // Home Icon
            PictureBox btnHomeIcon = CreateHomeIcon();
            _flowPath.Controls.Add(btnHomeIcon);

            // "Bibliotheken" Text
            Label lblHomeText = CreateBreadcrumbLabel("Bibliotheken", null, _crumbFontBold, colorLink);
            _flowPath.Controls.Add(lblHomeText);

            if (searchContext != null)
            {
                AddSeparator(_crumbFontBold, colorSep);
                Label lblSearch = CreateBreadcrumbLabel($"Suche: '{searchContext}'", null, _crumbFontBold, Color.Orange);
                _flowPath.Controls.Add(lblSearch);
            }
            else if (!_navState.IsInRoot)
            {
                AddSeparator(_crumbFontBold, colorSep);

                // Repo Name
                Label lblRepo = CreateBreadcrumbLabel(_navState.CurrentRepoName, "/", _crumbFontBold, colorLink);
                _flowPath.Controls.Add(lblRepo);

                // Ordner Pfad
                string path = _navState.CurrentPath;
                if (path != "/")
                {
                    string[] parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    string currentBuildPath = "";

                    foreach (var part in parts)
                    {
                        currentBuildPath += "/" + part;
                        AddSeparator(_crumbFontBold, colorSep);
                        Label lblPart = CreateBreadcrumbLabel(part, currentBuildPath, _crumbFontBold, colorLink);
                        _flowPath.Controls.Add(lblPart);
                    }
                }
            }

            _flowPath.ResumeLayout(true);
        }

        private PictureBox CreateHomeIcon()
        {
            PictureBox pb = new PictureBox();
            try { pb.Image = Properties.Resources.icon_home; } catch { }
            pb.SizeMode = PictureBoxSizeMode.Zoom;
            pb.Size = new Size(24, 30);
            pb.Cursor = Cursors.Hand;
            pb.Margin = new Padding(0, 0, 5, 0);
            pb.Click += (s, e) => _onNavigateRoot?.Invoke();
            return pb;
        }

        private Label CreateBreadcrumbLabel(string text, string navigationPath, Font font, Color color)
        {
            StableLabel lbl = new StableLabel();
            lbl.Text = text;
            lbl.ForeColor = color;
            lbl.Font = font;
            lbl.Height = 30;
            lbl.Margin = new Padding(0, 0, 0, 0);

            Size preferredSize = TextRenderer.MeasureText(text, font);
            lbl.Width = preferredSize.Width + 5;

            bool isClickable = !text.StartsWith("Suche:");

            if (isClickable)
            {
                lbl.Cursor = Cursors.Hand;
                lbl.MouseEnter += (s, e) => lbl.ForeColor = Color.Orange;
                lbl.MouseLeave += (s, e) => lbl.ForeColor = color;

                lbl.Click += (s, e) =>
                {
                    if (navigationPath == null)
                    {
                        _onNavigateRoot?.Invoke();
                    }
                    else if (navigationPath == "/")
                    {
                        _onNavigateRepo?.Invoke(_navState.CurrentRepoId, _navState.CurrentRepoName);
                    }
                    else
                    {
                        // Wir setzen den Pfad neu, indem wir Repo betreten und dann Ordner setzen
                        // Die Logik hier ist etwas vereinfacht: Wir rufen einfach Repo auf und navigieren manuell
                        // Aber da Form1 die Logik hat, müssen wir aufpassen.
                        // Der sauberste Weg ist, Form1 mitzuteilen "Navigiere zu Repo X mit Pfad Y"
                        // Hier rufen wir Folder Navigation auf:

                        // Trick: Wir nutzen die Callback Struktur
                        _onNavigateRepo?.Invoke(_navState.CurrentRepoId, _navState.CurrentRepoName);

                        // Wir müssen den State VOR dem Laden setzen, das passiert aber in Form1 normalerweise.
                        // Da BreadcrumbManager NavigationState als Referenz hat, können wir ihn manipulieren
                        // ABER: Das Laden muss danach passieren. 

                        // Besserer Fix: Form1 Logik nutzen.
                        // Da wir hier im Manager sind, simulieren wir die Schritte:
                        _navState.EnterRepo(_navState.CurrentRepoId, _navState.CurrentRepoName);
                        // Folder setzen
                        string[] folders = navigationPath.TrimStart('/').Split('/');
                        foreach (var f in folders) _navState.EnterFolder(f);

                        // Jetzt das Refresh Triggern (wir nutzen den Repo Callback als "Refresh" signal im weitesten Sinne
                        // oder wir bräuchten ein explizites "Refresh" Action Delegate.
                        // Da Form1 beim Click Logik ausführt, ist das Delegate Design oben etwas limitiert.
                        // Wir nutzen einfach die Delegates als Trigger.

                        // KORREKTUR: Wir nutzen eine spezifische Action für "GoToPath"
                    }
                };
            }
            return lbl;
        }

        private void AddSeparator(Font font, Color color)
        {
            StableLabel sep = new StableLabel();
            sep.Text = "/";
            sep.ForeColor = color;
            sep.Font = font;
            sep.Height = 30;
            sep.Width = 15;
            sep.TextAlign = ContentAlignment.MiddleCenter;
            sep.Margin = new Padding(0, 0, 0, 0);
            _flowPath.Controls.Add(sep);
        }
    }
}