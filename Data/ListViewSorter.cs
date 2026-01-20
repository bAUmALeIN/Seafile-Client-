using System;
using System.Collections;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3.Data
{
    public class ListViewSorter : IComparer
    {
        private readonly int _colIndex;
        private readonly bool _ascending;

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

            // REGEL 1: Der "Zurück" (..) Button bleibt IMMER ganz oben
            if (itemX.Tag is SeafileEntry eX_Back && eX_Back.type == "back") return -1;
            if (itemY.Tag is SeafileEntry eY_Back && eY_Back.type == "back") return 1;

            int result = 0;
            object tagX = itemX.Tag;
            object tagY = itemY.Tag;

            // Fall A: Wir sortieren Dateien/Ordner (SeafileEntry)
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

                    case 2: // Geändert
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
            // Fall B: Wir sortieren Bibliotheken (SeafileRepo)
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
                    default:
                        // Fallback Text
                        string tX = itemX.SubItems.Count > _colIndex ? itemX.SubItems[_colIndex].Text : "";
                        string tY = itemY.SubItems.Count > _colIndex ? itemY.SubItems[_colIndex].Text : "";
                        result = string.Compare(tX, tY);
                        break;
                }
            }
            else
            {
                // Fallback: Einfacher Textvergleich
                string txtX = itemX.SubItems.Count > _colIndex ? itemX.SubItems[_colIndex].Text : "";
                string txtY = itemY.SubItems.Count > _colIndex ? itemY.SubItems[_colIndex].Text : "";
                result = string.Compare(txtX, txtY);
            }

            // Bei Absteigend das Ergebnis umkehren
            return _ascending ? result : -result;
        }
    }
}