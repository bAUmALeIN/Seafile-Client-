using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp3
{
    public static class MenuBuilder
    {
        public static ContextMenuStrip CreateContextMenu(EventHandler downloadHandler, EventHandler deleteHandler)
        {
            // Context Menu (Standard WinForms, aber dunkel gestylt)
            ContextMenuStrip ctxMenu = new ContextMenuStrip();
            ctxMenu.RenderMode = ToolStripRenderMode.System;
            ctxMenu.BackColor = Color.FromArgb(50, 50, 50);
            ctxMenu.ForeColor = Color.White;

            // Renderer für dunkle Ränder
            ctxMenu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());

            ToolStripMenuItem itemDownload = new ToolStripMenuItem("Herunterladen");
            itemDownload.Name = "ItemDownload"; // WICHTIG für Zugriff
            itemDownload.Click += downloadHandler;

            ToolStripMenuItem itemDelete = new ToolStripMenuItem("Löschen");
            itemDelete.Name = "ItemDelete"; // WICHTIG für Zugriff
            itemDelete.Click += deleteHandler;

            ctxMenu.Items.Add(itemDownload);
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(itemDelete);

            return ctxMenu;
        }

        public static Image ResizeIcon(Image image, int width, int height)
        {
            Bitmap destImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(destImage))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, width, height);
            }
            return destImage;
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(80, 80, 80);
        public override Color MenuItemBorder => Color.FromArgb(80, 80, 80);
        public override Color MenuBorder => Color.FromArgb(40, 40, 40);
        public override Color ToolStripDropDownBackground => Color.FromArgb(50, 50, 50);
        public override Color ImageMarginGradientBegin => Color.FromArgb(50, 50, 50);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(50, 50, 50);
        public override Color ImageMarginGradientEnd => Color.FromArgb(50, 50, 50);
    }
}