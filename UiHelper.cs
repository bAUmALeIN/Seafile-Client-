using ReaLTaiizor.Colors;
using ReaLTaiizor.Controls;
using ReaLTaiizor.Forms;
using ReaLTaiizor.Manager;
using ReaLTaiizor.Util;
using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices; // Wichtig für WinAPI
using System.Windows.Forms;

namespace WinFormsApp3
{
    public static class UiHelper
    {
        // ========================================================================
        // WINAPI IMPORTS (Für den ultimativen Fix)
        // ========================================================================
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int LVM_SETEXTENDEDLISTVIEWSTYLE = 0x1036;
        private const int LVS_EX_DOUBLEBUFFER = 0x00010000; // Nativer Double Buffer

        // ========================================================================
        // LISTVIEW SETUP
        // ========================================================================
        public static void SetupListView(MaterialListView listView, ImageList customIcons = null)
        {
            // 1. Grundeinstellungen
            listView.View = View.Details;
            listView.OwnerDraw = true;
            listView.FullRowSelect = true;
            listView.GridLines = false;
            listView.Font = new Font("Segoe UI", 11f);

            Color darkBg = Color.FromArgb(50, 50, 50);
            listView.BackColor = darkBg;
            listView.ForeColor = Color.WhiteSmoke;

            // 2. STYLES: Opaque ist PFLICHT gegen weiße Blitzer!
            var method = typeof(Control).GetMethod("SetStyle", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(listView, new object[] {
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.Opaque, // VERBIETET Windows, den Hintergrund weiß zu löschen
                    true
                });
            }

            // 3. Nativer Double Buffer (Hilft oft besser als .NET Buffer bei ListViews)
            SendMessage(listView.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE, LVS_EX_DOUBLEBUFFER, LVS_EX_DOUBLEBUFFER);

            if (customIcons != null) listView.SmallImageList = customIcons;

            // 4. Hover-Effekt Logik
            int hoveredIndex = -1;
            listView.MouseMove += (s, e) => {
                var hit = listView.HitTest(e.Location);
                int newIndex = (hit.Item != null) ? hit.Item.Index : -1;
                if (newIndex != hoveredIndex)
                {
                    if (hoveredIndex != -1 && hoveredIndex < listView.Items.Count)
                        listView.Invalidate(listView.Items[hoveredIndex].Bounds);
                    hoveredIndex = newIndex;
                    if (hoveredIndex != -1)
                        listView.Invalidate(listView.Items[hoveredIndex].Bounds);
                }
            };
            listView.MouseLeave += (s, e) => {
                if (hoveredIndex != -1)
                {
                    int old = hoveredIndex;
                    hoveredIndex = -1;
                    if (old < listView.Items.Count) listView.Invalidate(listView.Items[old].Bounds);
                }
            };

            // 5. HEADER ZEICHNEN (Der "White Square" Killer)
            listView.DrawColumnHeader += (s, e) => {
                Color headerBg = Color.FromArgb(45, 45, 48);
                Color lineColor = Color.FromArgb(70, 70, 70);

                // Normalen Header füllen
                using (var b = new SolidBrush(headerBg))
                    e.Graphics.FillRectangle(b, e.Bounds);

                // Text & Linien
                using (var p = new Pen(lineColor))
                    e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

                using (var f = new Font("Segoe UI", 11f, FontStyle.Bold))
                {
                    Rectangle r = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height);
                    TextRenderer.DrawText(e.Graphics, e.Header.Text, f, r, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }

                // OVERDRAW: Den Bereich RECHTS neben der letzten Spalte füllen
                // Das ist der Bereich, der beim Vergrößern oft weiß aufblitzt ("eklig verschieben")
                if (e.ColumnIndex == listView.Columns.Count - 1)
                {
                    // Wir zeichnen einfach 4000 Pixel nach rechts weiter
                    Rectangle filler = new Rectangle(e.Bounds.Right, e.Bounds.Y, 4000, e.Bounds.Height);
                    using (var b = new SolidBrush(headerBg))
                    {
                        e.Graphics.FillRectangle(b, filler);
                    }
                    using (var p = new Pen(lineColor))
                        e.Graphics.DrawLine(p, filler.Left, filler.Bottom - 1, filler.Right, filler.Bottom - 1);
                }
            };

            // 6. ITEM DRAWING (Wegen Opaque MÜSSEN wir alles füllen)
            listView.DrawItem += (s, e) => { e.DrawDefault = false; };
            listView.DrawSubItem += (s, e) => {
                if (e.Item == null) return;
                int dropTargetIndex = (listView.Tag is int val) ? val : -1;
                Color bg = (e.ItemIndex % 2 == 0) ? Color.FromArgb(53, 53, 53) : Color.FromArgb(50, 50, 50);

                if (e.ItemIndex == dropTargetIndex) bg = Color.FromArgb(80, 80, 90);
                else if (e.Item.Selected) bg = Color.FromArgb(75, 75, 75);
                else if (e.ItemIndex == hoveredIndex) bg = Color.FromArgb(62, 62, 62);

                using (var b = new SolidBrush(bg))
                {
                    e.Graphics.FillRectangle(b, e.Bounds);

                    // Auch hier: Nach rechts überzeichnen beim letzten Item
                    if (e.ColumnIndex == listView.Columns.Count - 1)
                    {
                        Rectangle filler = new Rectangle(e.Bounds.Right, e.Bounds.Y, 4000, e.Bounds.Height);
                        e.Graphics.FillRectangle(b, filler);
                        using (var p = new Pen(Color.FromArgb(60, 60, 60)))
                            e.Graphics.DrawLine(p, filler.Left, filler.Bottom - 1, filler.Right, filler.Bottom - 1);
                    }
                }

                using (var p = new Pen(Color.FromArgb(60, 60, 60)))
                    e.Graphics.DrawLine(p, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

                if (e.ColumnIndex == 0)
                {
                    int x = e.Bounds.X + 10;
                    if (listView.SmallImageList != null && !string.IsNullOrEmpty(e.Item.ImageKey) && listView.SmallImageList.Images.ContainsKey(e.Item.ImageKey))
                    {
                        int yIcon = e.Bounds.Y + ((e.Bounds.Height - 20) / 2);
                        e.Graphics.DrawImage(listView.SmallImageList.Images[e.Item.ImageKey], x, yIcon, 20, 20);
                        x += 32;
                    }
                    Rectangle r = new Rectangle(x, e.Bounds.Y, e.Bounds.Width - (x - e.Bounds.X), e.Bounds.Height);
                    TextRenderer.DrawText(e.Graphics, e.Item.Text, listView.Font, r, Color.White, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                }
                else
                {
                    TextRenderer.DrawText(e.Graphics, e.SubItem.Text, listView.Font, e.Bounds, Color.FromArgb(180, 180, 180), TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                }
            };
        }

        public static void UpdateColumnWidths(MaterialListView listView)
        {
            if (listView.Columns.Count < 2 || listView.ClientSize.Width <= 0) return;
            int fixedWidths = 0;
            // Summiere alle Spalten außer der ersten
            for (int i = 1; i < listView.Columns.Count; i++) fixedWidths += listView.Columns[i].Width;

            // Berechne verfügbaren Platz für Spalte 0
            int avail = listView.ClientSize.Width - fixedWidths;
            // WICHTIG: Kein Puffer (-4) mehr, da wir Opaque nutzen. 
            // Aber um sicherzugehen, dass kein "Loch" entsteht:
            if (avail > 50) listView.Columns[0].Width = avail - 2;
        }

        public static void UpdateTransferColumnWidths(MaterialListView listView)
        {
            if (listView.Columns.Count < 5 || listView.ClientSize.Width == 0) return;
            int fixedWidth = listView.Columns[0].Width + listView.Columns[2].Width + listView.Columns[3].Width + listView.Columns[4].Width;
            int availableWidth = listView.ClientSize.Width - fixedWidth - 4;
            if (availableWidth > 50) listView.Columns[1].Width = availableWidth;
        }

        // --- Dialog Helpers (unverändert) ---
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

        public static void ShowInfoDialog(string title, string message)
        {
            var scheme = new MaterialColorScheme(MaterialPrimary.Blue500, MaterialPrimary.Blue700, MaterialPrimary.Blue200, MaterialAccent.LightBlue200, MaterialTextShade.WHITE);
            ShowGenericDialog(title, message, "OK", scheme);
        }

        public static void ShowSuccessDialog(string title, string message)
        {
            var scheme = new MaterialColorScheme(MaterialPrimary.Green500, MaterialPrimary.Green700, MaterialPrimary.Green200, MaterialAccent.LightGreen200, MaterialTextShade.WHITE);
            ShowGenericDialog(title, message, "SUPER", scheme);
        }

        public static void ShowErrorDialog(string title, string message)
        {
            ShowScrollableErrorDialog(title, message);
        }

        public static void ShowScrollableErrorDialog(string title, string message)
        {
            var skinManager = MaterialSkinManager.Instance;
            var oldScheme = skinManager.ColorScheme;
            skinManager.ColorScheme = new MaterialColorScheme(MaterialPrimary.Red500, MaterialPrimary.Red700, MaterialPrimary.Red200, MaterialAccent.Red400, MaterialTextShade.WHITE);
            MaterialForm prompt = new MaterialForm();
            prompt.Width = 600;
            prompt.Height = 500;
            prompt.Text = title;
            prompt.StartPosition = FormStartPosition.CenterParent;
            prompt.Sizable = false;
            skinManager.AddFormToManage(prompt);

            RichTextBox rtb = new RichTextBox();
            rtb.Text = message;
            rtb.Location = new Point(20, 80);
            rtb.Size = new Size(560, 320);
            rtb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            rtb.ReadOnly = true;
            rtb.BackColor = Color.FromArgb(50, 50, 50);
            rtb.ForeColor = Color.White;
            rtb.BorderStyle = BorderStyle.None;
            rtb.ScrollBars = RichTextBoxScrollBars.Vertical;
            rtb.Font = new Font("Consolas", 10f);

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

        public static bool ShowDangerConfirmation(string title, string message)
        {
            var redScheme = new MaterialColorScheme(MaterialPrimary.Red500, MaterialPrimary.Red700, MaterialPrimary.Red200, MaterialAccent.Red400, MaterialTextShade.WHITE);
            return ShowConfirmationBase(title, message, "LÖSCHEN", redScheme);
        }

        public static bool ShowConfirmationDialog(string title, string message)
        {
            return ShowConfirmationBase(title, message, "JA", null);
        }

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
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Location = new Point(20, 80);
            lbl.Size = new Size(460, 100);
            lbl.Font = new Font("Segoe UI", 11f, FontStyle.Regular);
            lbl.ForeColor = Color.WhiteSmoke;
            lbl.BackColor = Color.Transparent;
            lbl.TextAlign = ContentAlignment.MiddleLeft;
            lbl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            form.Controls.Add(lbl);
        }

        private static MaterialButton CreateButton(string text, DialogResult result, bool isPrimary, int x, int width = 110)
        {
            MaterialButton btn = new MaterialButton();
            btn.Text = text;
            btn.DialogResult = result;
            btn.Type = isPrimary ? MaterialButton.MaterialButtonType.Contained : MaterialButton.MaterialButtonType.Outlined;
            btn.UseAccentColor = !isPrimary;
            if (isPrimary) btn.UseAccentColor = false;
            btn.AutoSize = false;
            btn.Size = new Size(width, 36);
            btn.Location = new Point(x, 190);
            btn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            return btn;
        }

        public static string FormatByteSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }
    }
}