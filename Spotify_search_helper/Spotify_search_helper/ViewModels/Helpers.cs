using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;

namespace Spotify_search_helper.ViewModels
{
    public static class Helpers 
    {
        public readonly static ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        public static async void DisplayDialog(string title, string message)
        {
            try
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "Ok"
                };

                await dialog.ShowAsync();
            }
            catch (Exception)
            {

            }
        }

        public static string CleanString(string str)
        {
            string newStr = Regex.Replace(str, @"<[^>]+>| ", " ").Trim();
            newStr = Regex.Replace(newStr, "&(?!amp;)", "&");
            return newStr;
        }

        private static async Task<bool> LaunchUri(string url)
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

        public static async Task<StorageFile> ImageFileDialogPicker()
        {
            try
            {
                var picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.Thumbnail,
                    SuggestedStartLocation = PickerLocationId.PicturesLibrary
                };

                picker.FileTypeFilter.Add(".jpg");

                return await picker.PickSingleFileAsync();

            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<string> ImageToBase64(StorageFile file)
        {
            try
            {
                byte[] bytes;
                var buffer = await FileIO.ReadBufferAsync(file);
                using (MemoryStream mstream = new MemoryStream())
                {
                    await buffer.AsStream().CopyToAsync(mstream);
                    bytes = mstream.ToArray();
                }

                return Convert.ToBase64String(bytes);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<bool> OpenSpotifyAppAsync(string url)
        {
            try
            {
                ContentDialog dialog = new ContentDialog()
                {
                    Title = "Open Spotify app",
                    Content = "There are currently no active devices, open spotify app?",
                    CloseButtonText = "Cancel",
                    PrimaryButtonText = "Open Spotify"
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    return await LaunchUri(url);
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string MillisecondsToString(int milliSeconds)
        {
            string result = "0min";
            TimeSpan t = TimeSpan.FromMilliseconds(milliSeconds);
            if (t.Hours > 0)
            {
                if (t.Minutes > 0)
                    result = t.Hours + "h " + t.Minutes + "min";
                else
                    result = t.Hours + "h";
            }
            else if (t.Minutes > 0)
            {
                if (t.Seconds > 0)
                    result = t.Minutes + "min " + t.Seconds + "sec";
                else
                    result = t.Minutes + "min";
            }
            else if (t.Seconds > 0)
                result = t.Seconds + "sec";

            return result;
        }
    }
}
