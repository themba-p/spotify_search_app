using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        public static async Task<bool> LaunchUri(string url)
        {
            //"packageFamilyName": "SpotifyAB.SpotifyMusic_zpdnekdrzrea0",
            //"packageIdentityName": "SpotifyAB.SpotifyMusic",
            //"windowsPhoneLegacyId": "caac1b9d-621b-4f96-b143-e10e1397740a",
            //"publisherCertificateName": "CN=453637B3-4E12-4CDF-B0D3-2A3C863BF6EF"
            if (!string.IsNullOrEmpty(url))
            {
                var uri = new Uri(url);
                // Set the recommended app
                var options = new Windows.System.LauncherOptions
                {
                    PreferredApplicationPackageFamilyName = "SpotifyAB.SpotifyMusic_zpdnekdrzrea0",
                    PreferredApplicationDisplayName = "Spotify Music"
                };

                // Launch the URI and pass in the recommended app
                // in case the user has no apps installed to handle the URI
                return await Windows.System.Launcher.LaunchUriAsync(uri, options);
            }
            return false;
        }
    }
}
