using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TaskLens.Theme
{
    public static class ThemeManager
    {
        public static void ApplyTheme (string themeName)
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            dictionaries.Clear();

            var newDict = new ResourceDictionary
            {
                Source = new Uri($"Theme/{themeName}.xaml", UriKind.Relative)
            };
            dictionaries.Add(newDict);
        }
    }

}
