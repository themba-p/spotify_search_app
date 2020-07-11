using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class ItemMediaBase : ItemBase
    {
        public ItemMediaBase() { }

        public ItemMediaBase(string id, string title, ImageSource image, string uri, Dictionary<string, string> externalUrls)
            : base(id, title, image, uri, externalUrls)
        {

        }

        public ItemMediaBase(string id, string title, ImageSource image, string uri, Dictionary<string, string> externalUrls, User owner, string mediaPreviewUrl)
            : base(id, title, image, uri, externalUrls)
        {
            Owner = owner;
            MediaPreviewUrl = mediaPreviewUrl;
        }

        public string MediaPreviewUrl { get; set; }
        public User Owner { get; set; }
        public ItemMediaBaseType MediaBaseType { get; set; }

        private bool _canModify;
        public bool CanModify
        {
            get { return _canModify; }
            set { SetProperty(this._canModify, value, () => this._canModify = value); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(this._isSelected, value, () => this._isSelected = value); }
        }

        private bool _isLoadingPreview;
        public bool IsLoadingPreview
        {
            get { return _isLoadingPreview; }
            set { SetProperty(_isLoadingPreview, value, () => _isLoadingPreview = value); }
        }
    }

    public enum ItemMediaBaseType
    {
        Playlist,
        Track
    }
}
