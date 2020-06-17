using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_search_helper.Models
{
    public class Category
    {
        public Category() { }

        public Category(CategoryType type, string title, string description, string iconPath)
        {
            this.Type = type;
            this.Title = title;
            this.Description = description;
            this.IconPath = iconPath;
        }

        private CategoryType _type;
        public CategoryType Type
        {
            get => _type;
            set => _type = value;
        }

        private string _title;
        public string Title
        {
            get => _title;
            set => _title = value;
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => _description = value;
        }

        private string _iconPath;
        public string IconPath
        {
            get => _iconPath;
            set => _iconPath = value;
        }

        public static List<Category> GetCategories()
        {
            return new List<Category>
            {
                new Category(CategoryType.Playlist,
                "Manage playlists",
                "Merge, Clone or bulk delete.",
                "/Assets/playlist.png"),
                new Category(CategoryType.Liked,
                "Liked songs",
                "Clone to new playlist or clear.",
                "/Assets/liked.png"),
                new Category(CategoryType.MadeForYou,
                "Made for you",
                "Save Daily, Weekly mixes to library.",
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
