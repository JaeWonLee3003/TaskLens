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
            //var dictionaries = Application.Current.Resources.MergedDictionaries;
            //dictionaries.Clear();

            //var newDict = new ResourceDictionary
            //{
            //    Source = new Uri($"Theme/{themeName}.xaml", UriKind.Relative)
            //};
            //dictionaries.Add(newDict);

            var dictionaries = Application.Current.Resources.MergedDictionaries;

            // 언어 리소스를 찾고, 제거하지 않도록 해야 합니다.
            var languageDict = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.Contains("StringResources") == true);

            // 기존 리소스에 영향을 주지 않도록 언어 리소스를 그대로 두고, 테마만 변경
            var newDict = new ResourceDictionary
            {
                Source = new Uri($"Theme/{themeName}.xaml", UriKind.Relative)
            };

            // 테마 리소스만 추가
            dictionaries.Add(newDict);

            // 언어 리소스가 이미 있으면 다시 추가할 필요 없이 그대로 둠
            if (languageDict != null)
            {
                dictionaries.Add(languageDict);
            }

        }
    }

}
