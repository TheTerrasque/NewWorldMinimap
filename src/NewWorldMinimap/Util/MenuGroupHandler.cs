using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static System.Windows.Forms.Menu;

namespace NewWorldMinimap.Util
{
    public class MenuGroupHandler
    {
        List<MenuItem> Items = new List<MenuItem>();

        public void AddItem(string text, EventHandler onClick, Shortcut shortcut = Shortcut.None, bool selected = false)
        {
            var index = Items.Count;
            var mi = new MenuItem(text, (s, e) =>
            {
                Items.ForEach(x => x.Checked = false);
                Items[index].Checked = true;
                onClick(s, e);
            },
            shortcut);
            mi.Checked = selected;
            Items.Add(mi);
        }

        public void CreateMenu(MenuItemCollection collection)
        {
            if (collection.Count > 0)
            {
                collection.Add("-");
            }
            collection.AddRange(GetItems());
        }

        public MenuItem[] GetItems()
        {
            return Items.ToArray();
        }
    }
}
