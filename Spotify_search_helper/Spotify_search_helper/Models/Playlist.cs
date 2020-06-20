using Spotify_search_helper.ViewModels;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class Playlist : NotificationBase
    {
        public Playlist() { }

        public Playlist(string id, string title, string description, string owner,string ownerId, ImageSource image, PlaylistCategoryType categoryType)
        {
            this.Id = id;
            this.Title = title;
            this.Description = description;
            this.Owner = owner;
            this.OwnerId = ownerId;
            this.Image = image;
            this.CategoryType = categoryType;
        }

        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Owner { get; set; }
        public ImageSource Image { get; set; }
        public string OwnerId { get; set; }
        public PlaylistCategoryType CategoryType { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(this._isSelected, value, () => this._isSelected = value); }
        }

        
    }
}
