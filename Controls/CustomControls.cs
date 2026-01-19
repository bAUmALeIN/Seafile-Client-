using System;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp3.Controls
{
    // Stabilisiertes Label (verhindert Font-Reset durch MaterialSkin)
    public class StableLabel : Label
    {
        public StableLabel()
        {
            this.AutoSize = false;
            this.UseMnemonic = false;
            this.TextAlign = ContentAlignment.MiddleLeft;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            // Erzwinge Bold-Schriftart, wenn sie verloren geht (außer bei "/")
            if (this.Font != null && !this.Font.Bold && this.Text != "/")
            {
                this.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            }
        }
    }

    // Stabilisierter Button (Verhindert Font-Reset beim Klick)
    public class StableButton : Button
    {
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            // Wenn das Theme versucht, die Schrift zu ändern (z.B. bold weg), erzwingen wir es zurück
            if (this.Font != null && !this.Font.Bold)
            {
                this.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            }
        }
    }
}