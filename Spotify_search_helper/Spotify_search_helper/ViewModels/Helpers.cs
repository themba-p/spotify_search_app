using System;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Spotify_search_helper.ViewModels
{
    public static class Helpers 
    {
        public readonly static ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public static async void DisplayDialog(string title, string message)
        {
            ContentDialog dialog = new ContentDialog()
            {
                Title = title,
                Content = message,
                CloseButtonText = "Ok"
            };

            await dialog.ShowAsync();
        }

        public static string CleanString(string str)
        {
            string newStr = Regex.Replace(str, @"<[^>]+>| ", " ").Trim();
            newStr = Regex.Replace(newStr, "&(?!amp;)", "&amp;");
            return newStr;
        }
    }
}
