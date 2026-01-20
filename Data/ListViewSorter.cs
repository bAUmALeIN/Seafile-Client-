using System;
using System.Collections;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3.Data
{
    public class ListViewSorter : IComparer
    {
        private int _colIndex;
        private bool _ascending;

        public ListViewSorter(int columnIndex, bool ascending)
        {
            _colIndex = columnIndex;
            _ascending = ascending;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = x as ListViewItem;
            ListViewItem itemY = y as ListViewItem;

            if (itemX == null || itemY == null) return 0;

            // "Zurück" (..) immer ganz oben halten
            if (itemX.Tag is SeafileEntry eX && eX.type == "back") return -1;
            if (itemY.Tag is SeafileEntry eY && eY.type == "back") return 1;

            int result = 0;

            // Daten aus dem Tag holen (Sauberer als Text-Parsing!)
            var tagX = itemX.Tag;
            var tagY = itemY.Tag;

            // Fall 1: Wir sind in einer Bibliothek (SeafileEntry Objekte)
            if (tagX is SeafileEntry entryX && tagY is SeafileEntry entryY)
            {
                switch (_colIndex)
                {
                    case 0: // Name
                        // Ordner immer vor Dateien
                        if (entryX.type == "dir" && entryY.type != "dir") return -1;
                        if (entryX.type != "dir" && entryY.type == "dir") return 1;
                        result = string.Compare(entryX.name, entryY.name, StringComparison.OrdinalIgnoreCase);
                        break;
                    case 1: // Größe
                        result = entryX.size.CompareTo(entryY.size);
                        break;
                    case 2: // Geändert (Datum)
                        result = entryX.mtime.CompareTo(entryY.mtime);
                        break;
                    case 3: // Typ
                        result = string.Compare(entryX.type, entryY.type, StringComparison.OrdinalIgnoreCase);
                        break;
                    default:
                        result = string.Compare(itemX.SubItems[_colIndex].Text, itemY.SubItems[_colIndex].Text);
                        break;
                }
            }
            // Fall 2: Wir sind im Root (SeafileRepo Objekte)
            else if (tagX is SeafileRepo repoX && tagY is SeafileRepo repoY)
            {
                switch (_colIndex)
                {
                    case 0: // Name
                        result = string.Compare(repoX.name, repoY.name, StringComparison.OrdinalIgnoreCase);
                        break;
                    case 1: // Größe
                        result = repoX.size.CompareTo(repoY.size);
                        break;
                    case 2: // Geändert
                        result = repoX.mtime.CompareTo(repoY.mtime);
                        break;
                    case 3: // Owner
                        result = string.Compare(repoX.owner, repoY.owner, StringComparison.OrdinalIgnoreCase);
                        break;
                }
            }
            // Fallback: Textvergleich
            else
            {
                string textX = itemX.SubItems.Count > _colIndex ? itemX.SubItems[_colIndex].Text : "";
                string textY = itemY.SubItems.Count > _colIndex ? itemY.SubItems[_colIndex].Text : "";
                result = string.Compare(textX, textY);
            }

            // Richtung umkehren wenn absteigend
            return _ascending ? result : -result;
        }
    }
}