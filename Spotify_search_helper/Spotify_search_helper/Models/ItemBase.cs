using Spotify_search_helper.ViewModels;
using System.Collections.Generic;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class ItemBase : NotificationBase
    {
        public ItemBase() { }

        public ItemBase(string id, string title, string uri, Dictionary<string, string> externalUrls)
        {
            Id = id;
            Title = title;
            Uri = uri;
            ExternalUrls = externalUrls;
        }

        public ItemBase(string id, string title, ImageSource image, string uri, Dictionary<string, string> externalUrls)
        {
            Id = id;
            Title = title;
            Image = image;
            Uri = uri;
            ExternalUrls = externalUrls;
        }

        public string Id { get; set; }
        public string Title { get; set; }
        public ImageSource Image { get; set; }
        public string Uri { get; set; }
        public Dictionary<string, string> ExternalUrls { get; set; }

        private bool _isFocused = true;
        public bool IsFocused
        {
            get { return _isFocused; }
            set { SetProperty(this._isFocused, value, () => this._isFocused = value); }
        }
    }

    
}
