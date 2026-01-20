using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp3
{
    public static class MenuBuilder
    {
        public static ContextMenuStrip CreateContextMenu(EventHandler downloadHandler, EventHandler deleteHandler, EventHandler renameHandler, EventHandler shareHandler)
        {
            ContextMenuStrip ctxMenu = new ContextMenuStrip();
            ctxMenu.RenderMode = ToolStripRenderMode.Professional;
            ctxMenu.BackColor = Color.FromArgb(40, 40, 40);
            ctxMenu.ForeColor = Color.WhiteSmoke;
            ctxMenu.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            ctxMenu.ImageScalingSize = new Size(18, 18);

            // Herunterladen
            ToolStripMenuItem itemDownload = new ToolStripMenuItem("Herunterladen") { Name = "ItemDownload", Image = ResizeIcon(Properties.Resources.icon_ctx_download, 18, 18) };
            itemDownload.Click += downloadHandler;

            // Freigeben (NEU)
            // HINWEIS: Stelle sicher, dass du 'icon_share' in den Ressourcen hast!
            // Falls nicht, nimm vorerst null oder ein anderes Icon.
            Image shareIcon = null;
            try { shareIcon = ResizeIcon(Properties.Resources.icon_share, 18, 18); } catch { }

            ToolStripMenuItem itemShare = new ToolStripMenuItem("Freigeben") { Name = "ItemShare", Image = shareIcon };
            itemShare.Click += shareHandler;

            // Umbenennen
            ToolStripMenuItem itemRename = new ToolStripMenuItem("Umbenennen") { Name = "ItemRename", Image = ResizeIcon(Properties.Resources.icon_rename, 18, 18) };
            itemRename.Click += renameHandler;

            // Löschen
            ToolStripMenuItem itemDelete = new ToolStripMenuItem("Löschen") { Name = "ItemDelete", Image = ResizeIcon(Properties.Resources.icon_ctx_löschen, 18, 18) };
            itemDelete.Click += deleteHandler;

            // Zusammenbauen
            ctxMenu.Items.Add(itemDownload);
            ctxMenu.Items.Add(itemShare); // <-- Hier eingefügt
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add(itemRename);
            ctxMenu.Items.Add(itemDelete);

            return ctxMenu;
        }

        public static Image ResizeIcon(Image image, int width, int height)
        {
            if (image == null) return null;
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
        public override Color MenuItemSelected => Color.FromArgb(60, 60, 65);
        public override Color MenuItemBorder => Color.FromArgb(80, 80, 80);
        public override Color MenuBorder => Color.FromArgb(30, 30, 30);
        public override Color ToolStripDropDownBackground => Color.FromArgb(40, 40, 40);
        public override Color ImageMarginGradientBegin => Color.FromArgb(40, 40, 40);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(40, 40, 40);
        public override Color ImageMarginGradientEnd => Color.FromArgb(40, 40, 40);
        public override Color SeparatorDark => Color.FromArgb(80, 80, 80);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 65);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 65);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(50, 50, 50);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(50, 50, 50);
    }
}