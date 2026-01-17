using ReaLTaiizor.Colors;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp3
{
    public static class UiHelper
    {
        // ========================================================================
        // LISTVIEW HELPERS
        // ========================================================================
        public static void SetupListView(MaterialListView listView)
        {
            listView.Font = new Font("Segoe UI", 12f, FontStyle.Regular);

            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(24, 24);
            imgList.ColorDepth = ColorDepth.Depth32Bit;
            imgList.Images.Add("dir", SystemIcons.Application);
            imgList.Images.Add("file", SystemIcons.WinLogo);
            listView.SmallImageList = imgList;

            listView.Columns.Clear();
            listView.Columns.Add("", 40);
            listView.Columns.Add("Name", 300);
            listView.Columns.Add("Größe", 100);
            listView.Columns.Add("Geändert", 150);
            listView.Columns.Add("Typ", 80);
        }

        public static void UpdateColumnWidths(MaterialListView listView)
        {
            // Sicherheitscheck: Gibt es überhaupt Spalten?
            if (listView.Columns.Count < 5 || listView.ClientSize.Width == 0) return;

            int fixedWidth = listView.Columns[0].Width + listView.Columns[2].Width + listView.Columns[3].Width + listView.Columns[4].Width;

            bool hasVerticalScroll = false;

            // FIX: Try-Catch Block, falls ListView.GetItemRect(0) fehlschlägt
            // Das passiert manchmal beim Neuladen oder wenn die Liste gerade geleert wurde.
            try
            {
                if (listView.Items.Count > 0)
                {
                    var rect = listView.GetItemRect(0); // Hier knallte es vorher
                    int itemHeight = rect.Height > 0 ? rect.Height : 25;

                    if ((listView.Items.Count * itemHeight) > listView.ClientSize.Height)
                    {
                        hasVerticalScroll = true;
                    }
                }
            }
            catch
            {
                // Falls ein Fehler passiert (z.B. IndexOutOfRange), ignorieren wir ihn einfach.
                // Wir nehmen an, es gibt keinen Scrollbalken, damit das Programm weiterläuft.
                hasVerticalScroll = false;
            }

            int buffer = hasVerticalScroll ? SystemInformation.VerticalScrollBarWidth + 4 : 0;
            int availableWidth = listView.ClientSize.Width - fixedWidth - buffer;

            if (availableWidth > 50) listView.Columns[1].Width = availableWidth;
        }

        // ========================================================================
        // INPUT DIALOG
        // ========================================================================
        public static string ShowInputDialog(string title, string hintText)
        {
            MaterialForm prompt = CreateBaseForm(title, 240);

            MaterialTextBoxEdit textBox = new MaterialTextBoxEdit();
            textBox.Hint = hintText;
            textBox.Location = new Point(20, 90);
            textBox.Width = 460;
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            MaterialButton btnOk = CreateButton("OK", DialogResult.OK, true, 390);
            MaterialButton btnCancel = CreateButton("Abbrechen", DialogResult.Cancel, false, 280);

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(btnOk);
            prompt.Controls.Add(btnCancel);
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        // ========================================================================
        // NACHRICHTEN & BESTÄTIGUNGEN
        // ========================================================================

        // Blau: Für reine Informationen
        public static void ShowInfoDialog(string title, string message)
        {
            var scheme = new MaterialColorScheme(MaterialPrimary.Blue500, MaterialPrimary.Blue700, MaterialPrimary.Blue200, MaterialAccent.LightBlue200, MaterialTextShade.WHITE);
            ShowGenericDialog(title, message, "OK", scheme);
        }

        // Grün: Für Erfolg (z.B. Download fertig)
        public static void ShowSuccessDialog(string title, string message)
        {
            var scheme = new MaterialColorScheme(MaterialPrimary.Green500, MaterialPrimary.Green700, MaterialPrimary.Green200, MaterialAccent.LightGreen200, MaterialTextShade.WHITE);
            ShowGenericDialog(title, message, "SUPER", scheme);
        }

        // Rot: Für Fehler
        public static void ShowErrorDialog(string title, string message)
        {
            var scheme = new MaterialColorScheme(MaterialPrimary.Red500, MaterialPrimary.Red700, MaterialPrimary.Red200, MaterialAccent.Red400, MaterialTextShade.WHITE);
            ShowGenericDialog(title, message, "VERSTANDEN", scheme);
        }

        public static void ShowScrollableErrorDialog(string title, string message)
        {
            var skinManager = MaterialSkinManager.Instance;
            var oldScheme = skinManager.ColorScheme;

            // Rotes Schema für Alarm
            skinManager.ColorScheme = new MaterialColorScheme(
                MaterialPrimary.Red500, MaterialPrimary.Red700,
                MaterialPrimary.Red200, MaterialAccent.Red400,
                MaterialTextShade.WHITE);

            MaterialForm prompt = new MaterialForm();
            prompt.Width = 600;  // Breiter
            prompt.Height = 500; // Höher
            prompt.Text = title;
            prompt.StartPosition = FormStartPosition.CenterParent;
            prompt.Sizable = false; // Feste Größe
            skinManager.AddFormToManage(prompt);

            // Scrollbare Textbox (RichTextBox ist besser für viel Text als Label)
            RichTextBox rtb = new RichTextBox();
            rtb.Text = message;
            rtb.Location = new Point(20, 80);
            rtb.Size = new Size(560, 320); // Viel Platz!
            rtb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            rtb.ReadOnly = true;
            rtb.BackColor = Color.FromArgb(50, 50, 50); // Dunkel passend zum Theme
            rtb.ForeColor = Color.White;
            rtb.BorderStyle = BorderStyle.None;
            rtb.ScrollBars = RichTextBoxScrollBars.Vertical; // WICHTIG: Scrollbalken
            rtb.Font = new Font("Consolas", 10f); // Monospace Font für Code/HTML gut lesbar

            MaterialButton btnOk = new MaterialButton();
            btnOk.Text = "Schließen";
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(450, 420);
            btnOk.Size = new Size(130, 36);
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            prompt.Controls.Add(rtb);
            prompt.Controls.Add(btnOk);
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnOk;

            prompt.ShowDialog();

            skinManager.ColorScheme = oldScheme;
        }

        // Rot: Gefährliche Frage (Löschen)
        public static bool ShowDangerConfirmation(string title, string message)
        {
            var redScheme = new MaterialColorScheme(MaterialPrimary.Red500, MaterialPrimary.Red700, MaterialPrimary.Red200, MaterialAccent.Red400, MaterialTextShade.WHITE);
            return ShowConfirmationBase(title, message, "LÖSCHEN", redScheme);
        }

        // Orange/Standard: Normale Frage (Logout) -> DAS HIER IST NEU FÜR DICH!
        public static bool ShowConfirmationDialog(string title, string message)
        {
            // Wir nehmen das aktuelle Schema (Orange) oder setzen es explizit
            //var orangeScheme = new MaterialColorScheme(MaterialPrimary.Orange500, MaterialPrimary.Orange700, MaterialPrimary.Orange200, MaterialAccent.DeepOrange400, MaterialTextShade.WHITE);
            // Einfach null übergeben, dann nutzt er das aktuelle Design
            return ShowConfirmationBase(title, message, "JA", null);
        }

        // ========================================================================
        // PRIVATE HELFER (DRY - Don't Repeat Yourself)
        // ========================================================================

        private static void ShowGenericDialog(string title, string message, string btnText, MaterialColorScheme scheme)
        {
            var oldScheme = MaterialSkinManager.Instance.ColorScheme;
            if (scheme != null) MaterialSkinManager.Instance.ColorScheme = scheme;

            MaterialForm prompt = CreateBaseForm(title, 240);
            AddLabel(prompt, message);

            MaterialButton btnOk = CreateButton(btnText, DialogResult.OK, true, 350, 130);
            prompt.Controls.Add(btnOk);
            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnOk;

            prompt.ShowDialog();
            MaterialSkinManager.Instance.ColorScheme = oldScheme;
        }

        private static bool ShowConfirmationBase(string title, string message, string okText, MaterialColorScheme scheme)
        {
            var oldScheme = MaterialSkinManager.Instance.ColorScheme;
            if (scheme != null) MaterialSkinManager.Instance.ColorScheme = scheme;

            MaterialForm prompt = CreateBaseForm(title, 250);
            AddLabel(prompt, message);

            MaterialButton btnYes = CreateButton(okText, DialogResult.OK, true, 370);
            MaterialButton btnNo = CreateButton("Abbrechen", DialogResult.Cancel, false, 250);

            prompt.Controls.Add(btnYes);
            prompt.Controls.Add(btnNo);
            prompt.AcceptButton = btnYes;
            prompt.CancelButton = btnNo;

            var result = prompt.ShowDialog();
            MaterialSkinManager.Instance.ColorScheme = oldScheme;

            return result == DialogResult.OK;
        }

        private static MaterialForm CreateBaseForm(string title, int height)
        {
            MaterialForm form = new MaterialForm();
            form.Width = 500;
            form.Height = height;
            form.Text = title;
            form.StartPosition = FormStartPosition.CenterParent;
            form.Sizable = false;
            MaterialSkinManager.Instance.AddFormToManage(form);
            return form;
        }

        private static void AddLabel(Form form, string text)
        {
            MaterialLabel lbl = new MaterialLabel();
            lbl.Text = text;
            lbl.Location = new Point(20, 80);
            lbl.Size = new Size(460, 100);
            lbl.FontType = MaterialSkinManager.FontType.Body1;
            lbl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            form.Controls.Add(lbl);
        }

        private static MaterialButton CreateButton(string text, DialogResult result, bool isPrimary, int x, int width = 110)
        {
            MaterialButton btn = new MaterialButton();
            btn.Text = text;
            btn.DialogResult = result;
            btn.Type = isPrimary ? MaterialButton.MaterialButtonType.Contained : MaterialButton.MaterialButtonType.Outlined;
            btn.UseAccentColor = !isPrimary; // Trick für Farben
            if (isPrimary) btn.UseAccentColor = false; // Primary nutzt Hauptfarbe

            btn.AutoSize = false;
            btn.Size = new Size(width, 36);
            btn.Location = new Point(x, 190);
            btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            return btn;
        }
    }
}