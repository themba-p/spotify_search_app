using Spotify_search_helper.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class Track : ItemMediaBase
    {
        public Track() { }

        public Track(string id, string title, ImageSource image, string uri, User owner, string mediaPreviewUrl, string artist, string album, int trackNumber, int durationMs)
            : base(id, title, image, uri, owner, mediaPreviewUrl)
        {
            Artist = artist;
            Album = album;
            TrackNumber = trackNumber;
            DurationMs = durationMs;
            MediaBaseType = ItemMediaBaseType.Playlist;
        }

        public string Artist { get; set; }
        public string Album { get; set; }
        public int TrackNumber { get; set; }
        public int DurationMs { get; set; }
    }
}
