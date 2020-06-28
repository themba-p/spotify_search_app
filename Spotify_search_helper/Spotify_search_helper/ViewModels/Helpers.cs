using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

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
            newStr = Regex.Replace(newStr, "&(?!amp;)", "&");
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

        private static async Task<BitmapImage> GetImageFromFile(StorageFile file)
        {
            try
            {
                BitmapImage bitmapImage = new BitmapImage();
                //IRandomAccessStream stream = await file.OpenReadAsync();
                //image.SetSource(stream);
                //return image;

                // Ensure the stream is disposed once the image is loaded
                using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    // Set the image source to the selected bitmap
                    // Decode pixel sizes are optional
                    bitmapImage.DecodePixelHeight = 640;
                    bitmapImage.DecodePixelWidth = 640;

                    await bitmapImage.SetSourceAsync(fileStream);
                }
                return bitmapImage;
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
    }
}
