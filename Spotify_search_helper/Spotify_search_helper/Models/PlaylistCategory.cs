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

        private int _tracksCount;
        public int TracksCount
        {
            get { return _tracksCount; }
            set { SetProperty(this._tracksCount, value, () => this._tracksCount = value); }
        }

        private bool _hasResults;
        public bool HasResults
        {
            get { return _hasResults; }
            set { SetProperty(this._hasResults, value, () => this._hasResults = value); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(this._isSelected, value, () => this._isSelected = value); }
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
