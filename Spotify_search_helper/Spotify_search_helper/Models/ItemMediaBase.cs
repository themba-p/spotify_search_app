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

        public ItemMediaBase(string id, string title, ImageSource image, string uri)
            : base(id, title, image, uri)
        {

        }

        public ItemMediaBase(string id, string title, ImageSource image, string uri, User owner, string mediaPreviewUrl)
            : base(id, title, image, uri)
        {
            Owner = owner;
            MediaPreviewUrl = mediaPreviewUrl;
        }

        public string MediaPreviewUrl { get; set; }
        public User Owner { get; set; }
        public ItemMediaBaseType MediaBaseType { get; set; }
        public bool CanModify { get; set; }

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
