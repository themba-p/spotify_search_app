using Spotify_search_helper.ViewModels;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class ItemBase : NotificationBase
    {
        public ItemBase() { }

        public ItemBase(string id, string title, string uri)
        {
            Id = id;
            Title = title;
            Uri = uri;
        }

        public ItemBase(string id, string title, ImageSource image, string uri)
        {
            Id = id;
            Title = title;
            Image = image;
            Uri = uri;
        }

        public string Id { get; set; }
        public string Title { get; set; }
        public ImageSource Image { get; set; }
        public string Uri { get; set; }
    }

    
}
