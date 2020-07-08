using Spotify_search_helper.ViewModels;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class Playlist : ItemMediaBase
    {
        public Playlist() { }

        public Playlist(string id, string title, ImageSource image, string uri, User owner, string mediaPreviewUrl, string description, PlaylistCategoryType categoryType, int itemsCount, bool isQuickaccessItem = false)
            :base(id, title, image, uri, owner, mediaPreviewUrl)
        {
            Description = Helpers.CleanString(description);
            CategoryType = categoryType;
            ItemsCount = itemsCount;
            MediaBaseType = ItemMediaBaseType.Playlist;
            this.IsQuickAccessItem = isQuickaccessItem;
        }

        public string Description { get; set; }
        public int ItemsCount { get; set; }
        public PlaylistCategoryType CategoryType { get; set; }

        private string _durationStr = "0";
        public string DurationStr
        {
            get { return _durationStr; }
            set { SetProperty(_durationStr, value, () => _durationStr = value); }
        }

        private bool _isQuickAccessItem;
        public bool IsQuickAccessItem
        {
            get { return _isQuickAccessItem; }
            set { SetProperty(_isQuickAccessItem, value, () => _isQuickAccessItem = value); }
        }
    }
}
