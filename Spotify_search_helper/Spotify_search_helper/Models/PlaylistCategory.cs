using Spotify_search_helper.ViewModels;
using System.Collections.Generic;

namespace Spotify_search_helper.Models
{
    public class PlaylistCategory : NotificationBase
    {

        public PlaylistCategory(string title, PlaylistCategoryType categoryType)
        {
            this.Title = title;
            this.CategoryType = categoryType;
        }

        public PlaylistCategoryType CategoryType { get; set; }


        private string _title;
        public string Title
        {
            get { return this._title; }
            set { SetProperty(this._title, value, () => this._title = value); }
        }

        private int _count;
        public int Count
        {
            get { return _count; }
            set { SetProperty(this._count, value, () => this._count = value); }
        }

        public static List<PlaylistCategory> GetCategoryItems()
        {
            return new List<PlaylistCategory>
            {
                new PlaylistCategory("All playlists", PlaylistCategoryType.All),
                new PlaylistCategory("My playlists", PlaylistCategoryType.MyPlaylist),
                new PlaylistCategory("Made For You", PlaylistCategoryType.MadeForYou),
                new PlaylistCategory("Following", PlaylistCategoryType.Following)
            };
        }
    }

    public enum PlaylistCategoryType
    {
        MadeForYou,
        MyPlaylist,
        Following,
        All
    };
}
