using System;
using System.Linq;
using System.Windows;

namespace TaskLens.Helpers
{
    public static class LocalizationManager
    {
        public static void ApplyLanguage (string languageCode)
        {
            var resourceDictionary = new ResourceDictionary();
            switch (languageCode)
            {
                case "en":
                    resourceDictionary.Source = new Uri("Resources/StringResources.en.xaml", UriKind.Relative);
                    break;
                case "ko":
                default:
                    resourceDictionary.Source = new Uri("Resources/StringResources.ko.xaml", UriKind.Relative);
                    break;
            }

            var mergedDictionary = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString.Contains("StringResources") == true);

            if (mergedDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(mergedDictionary);
            }

            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
        }
    }
}
