using System.Drawing;
using System.Windows.Forms;
using ReaLTaiizor.Controls;
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

            // Spalten 
            listView.Columns.Clear();
            listView.Columns.Add("", 40);         // Icon
            listView.Columns.Add("Name", 300);    // Name
            listView.Columns.Add("Größe", 100);   // Größe
            listView.Columns.Add("Typ", 80);      // Typ
        }

        public static void UpdateColumnWidths(MaterialListView listView)
        {
            if (listView.Columns.Count < 4 || listView.ClientSize.Width == 0) return;

            int fixedWidth = listView.Columns[0].Width + listView.Columns[2].Width + listView.Columns[3].Width;

            // Scrollbar-Check
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

        public static string ShowInputDialog(string title, string promptText)
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true,
                MinimizeBox = false,
                MaximizeBox = false
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Text = promptText, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 340 };
            System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button() { Text = "Ok", Left = 240, Width = 100, Top = 90, DialogResult = DialogResult.OK };

            confirmation.BackColor = Color.Orange;
            confirmation.ForeColor = Color.Black;
            confirmation.FlatStyle = FlatStyle.Flat;

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }
}