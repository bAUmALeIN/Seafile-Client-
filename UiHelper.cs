using ReaLTaiizor.Colors;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System.Drawing;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public static class UiHelper
    {
        public static void SetupListView(MaterialListView listView)
        {
            // Icons
            ImageList imgList = new ImageList();
            imgList.ImageSize = new Size(24, 24);
            imgList.ColorDepth = ColorDepth.Depth32Bit;
            imgList.Images.Add("dir", SystemIcons.Application);
            imgList.Images.Add("file", SystemIcons.WinLogo);
            listView.SmallImageList = imgList;

            // Spalten definieren (Jetzt mit Datum!)
            listView.Columns.Clear();
            listView.Columns.Add("", 40);          // Icon
            listView.Columns.Add("Name", 300);     // Name
            listView.Columns.Add("Größe", 100);    // Größe
            listView.Columns.Add("Geändert", 150); // NEU: Datum
            listView.Columns.Add("Typ", 80);       // Typ
        }

        public static void UpdateColumnWidths(MaterialListView listView)
        {
            // Wir prüfen auf < 5 Spalten (weil wir jetzt eine mehr haben)
            if (listView.Columns.Count < 5 || listView.ClientSize.Width == 0) return;

            // Feste Breite: Icon + Größe + Datum + Typ
            int fixedWidth = listView.Columns[0].Width + listView.Columns[2].Width + listView.Columns[3].Width + listView.Columns[4].Width;

            bool hasVerticalScroll = false;
            if (listView.Items.Count > 0)
            {
                int itemHeight = listView.GetItemRect(0).Height;
                if (itemHeight == 0) itemHeight = 25;
                if (listView.Items.Count * itemHeight > listView.ClientSize.Height)
                    hasVerticalScroll = true;
            }

            int buffer = hasVerticalScroll ? SystemInformation.VerticalScrollBarWidth + 4 : 0;
            int availableWidth = listView.ClientSize.Width - fixedWidth - buffer;

            if (availableWidth > 50)
                listView.Columns[1].Width = availableWidth;
        }


        public static string ShowInputDialog(string title, string hintText)
        {
            MaterialForm prompt = new MaterialForm();
            prompt.Width = 500;
            prompt.Height = 240;
            prompt.Text = title;
            prompt.StartPosition = FormStartPosition.CenterParent;
            prompt.Sizable = false;

            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(prompt);

            MaterialTextBoxEdit textBox = new MaterialTextBoxEdit();
            textBox.Hint = hintText;
            textBox.Location = new Point(20, 90);
            textBox.Width = 460;
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            MaterialButton btnOk = new MaterialButton();
            btnOk.Text = "OK";
            btnOk.Type = MaterialButton.MaterialButtonType.Contained;
            btnOk.UseAccentColor = true;
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(390, 180);
            btnOk.AutoSize = false;
            btnOk.Size = new Size(90, 36);
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            MaterialButton btnCancel = new MaterialButton();
            btnCancel.Text = "Abbrechen";
            btnCancel.Type = MaterialButton.MaterialButtonType.Outlined;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(280, 180);
            btnCancel.AutoSize = false;
            btnCancel.Size = new Size(100, 36);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(btnOk);
            prompt.Controls.Add(btnCancel);

            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }

        // --- NEU: Roter Warn-Dialog für Löschen ---
        public static bool ShowDangerConfirmation(string title, string message)
        {
            var skinManager = MaterialSkinManager.Instance;

            // 1. Altes Farbschema speichern
            var oldScheme = skinManager.ColorScheme;

            // 2. Auf ROTES Warn-Schema umschalten
            skinManager.ColorScheme = new MaterialColorScheme(
                MaterialPrimary.Red500,
                MaterialPrimary.Red700,
                MaterialPrimary.Red200,
                MaterialAccent.Red400,
                MaterialTextShade.WHITE
            );

            // 3. Dialog bauen
            MaterialForm prompt = new MaterialForm();
            prompt.Width = 500;
            prompt.Height = 250;
            prompt.Text = title;
            prompt.StartPosition = FormStartPosition.CenterParent;
            prompt.Sizable = false;

            skinManager.AddFormToManage(prompt);

            // Warntext (Label)
            MaterialLabel lblMsg = new MaterialLabel();
            lblMsg.Text = message;
            lblMsg.Location = new Point(20, 80);
            lblMsg.Size = new Size(460, 100);
            lblMsg.FontType = MaterialSkinManager.FontType.Body1;
            lblMsg.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // LÖSCHEN Button (Rot, ausgefüllt)
            MaterialButton btnDelete = new MaterialButton();
            btnDelete.Text = "LÖSCHEN";
            btnDelete.Type = MaterialButton.MaterialButtonType.Contained;
            btnDelete.UseAccentColor = false; // Nutzt Primary Color (jetzt Rot)
            btnDelete.DialogResult = DialogResult.OK;
            btnDelete.Location = new Point(370, 190);
            btnDelete.AutoSize = false;
            btnDelete.Size = new Size(110, 36);
            btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            // Abbrechen Button
            MaterialButton btnCancel = new MaterialButton();
            btnCancel.Text = "Abbrechen";
            btnCancel.Type = MaterialButton.MaterialButtonType.Outlined;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(250, 190);
            btnCancel.AutoSize = false;
            btnCancel.Size = new Size(110, 36);
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            prompt.Controls.Add(lblMsg);
            prompt.Controls.Add(btnDelete);
            prompt.Controls.Add(btnCancel);

            prompt.AcceptButton = btnDelete;
            prompt.CancelButton = btnCancel;

            // 4. Anzeigen
            var result = prompt.ShowDialog();

            // 5. WICHTIG: Altes Farbschema (Orange) wiederherstellen
            skinManager.ColorScheme = oldScheme;

            return result == DialogResult.OK;
        }

    }
}