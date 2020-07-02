using Spotify_search_helper.ViewModels;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class Playlist : ItemMediaBase
    {
        public Playlist() { }

        public Playlist(string id, string title, ImageSource image, string uri, User owner, string mediaPreviewUrl, string description, PlaylistCategoryType categoryType, int itemsCount)
            :base(id, title, image, uri, owner, mediaPreviewUrl)
        {
            Description = Helpers.CleanString(description);
            CategoryType = categoryType;
            ItemsCount = itemsCount;
            MediaBaseType = ItemMediaBaseType.Playlist;
        }

        public string Description { get; set; }
        public int ItemsCount { get; set; }
        public PlaylistCategoryType CategoryType { get; set; }
    }
}
