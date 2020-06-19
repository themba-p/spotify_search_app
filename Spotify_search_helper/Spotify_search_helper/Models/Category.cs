using Spotify_search_helper.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_search_helper.Models
{
    public class Category : NotificationBase
    {
        public Category() { }

        public static readonly List<string> _personalizedPlaylistNames = new List<string>
        {
            "discover weekly",
            "release radar",
            "daily mix 1",
            "daily mix 2",
            "daily mix 3",
            "daily mix 4",
            "daily mix 5",
            "daily mix 6",
            "on repeat",
            "repeat rewind",
        };

        public Category(CategoryType type, string title, string description, string iconPath)
        {
            this.Type = type;
            this.Title = title;
            this.Description = description;
            this.IconPath = iconPath;
        }

        public CategoryType Type { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }

        public static List<Category> GetCategoryItems()
        {
            return new List<Category>
            {
                new Category(CategoryType.Playlist,
                "Manage playlists",
                "Merge, Clone or bulk delete.",
                "/Assets/playlist.png"),
                new Category(CategoryType.Liked,
                "Liked songs",
                "Create a new playlist from your liked songs, and clear.",
                "/Assets/liked.png"),
                new Category(CategoryType.MadeForYou,
                "YouTube playlist from a Spotify playlist",
                "Create a YouTube.",
                "/Assets/made_for_you.png"),
                new Category(CategoryType.Convert,
                "Convert files",
                "Find your local files on Spotify.",
                "/Assets/convert.png")
            };
        }
    }
    
    public enum CategoryType
    {
        Playlist,
        Liked,
        MadeForYou,
        Convert
    };
}
