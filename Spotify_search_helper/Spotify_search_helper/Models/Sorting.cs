using Microsoft.Toolkit.Uwp.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_search_helper.Models
{
    public class Sorting
    {
        public Sorting(string title, string property, SortType type, SortDirection sortDirection)
        {
            this.Title = title;
            this.Property = property;
            this.Type = type;
            this.SortDirection = sortDirection;
        }

        public static List<Sorting> _playlistSortList = new List<Sorting>
        {
            new Sorting("Default", "", SortType.Default, SortDirection.Ascending),
            new Sorting("File name (A to Z)", "Title", SortType.Name, SortDirection.Ascending),
            new Sorting("File name (Z to A)", "Title", SortType.Name, SortDirection.Descending),
            new Sorting("Number of tracks (smallest first)", "ItemsCount", SortType.Size, SortDirection.Ascending),
            new Sorting("Number of tracks (largest first)", "ItemsCount", SortType.Size, SortDirection.Descending),
        };

        public static List<Sorting> _tracksSortList = new List<Sorting>
        {
            new Sorting("Default", "", SortType.Default, SortDirection.Ascending),
            new Sorting("Track name (A to Z)", "Title", SortType.Name, SortDirection.Ascending),
            new Sorting("Track name (Z to A)", "Title", SortType.Name, SortDirection.Descending),
            new Sorting("Album name (A to Z)", "Album", SortType.Album, SortDirection.Ascending),
            new Sorting("Album name (Z to A)", "Album", SortType.Album, SortDirection.Descending),
            new Sorting("Artist name (A to Z)", "ArtistStr", SortType.Artist, SortDirection.Ascending),
            new Sorting("Artist name (Z to A)", "ArtistStr", SortType.Artist, SortDirection.Descending),
        };

        public string Title { get; set; }
        public string Property { get; set; }
        public SortType Type { get; set; }
        public SortDirection SortDirection { get; set; }

        public enum SortType
        {
            Default,
            Artist,
            Album,
            Name,
            Date,
            Size
        }
    }
}
