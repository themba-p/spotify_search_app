using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Spotify_search_helper.ViewModels
{
    public static class Helpers
    {
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
    }
}
