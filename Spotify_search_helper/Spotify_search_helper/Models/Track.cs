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

        public Track(string id, string title, ImageSource image, string uri, Dictionary<string, string> externalUrls, User owner, string mediaPreviewUrl, string artist, string album, int trackNumber, int duration)
            : base(id, title, image, uri, externalUrls, owner, mediaPreviewUrl)
        {
            Artist = artist;
            Album = album;
            TrackNumber = trackNumber;
            Duration = duration;
            MediaBaseType = ItemMediaBaseType.Playlist;
        }

        public string Artist { get; set; }
        public string Album { get; set; }
        public int TrackNumber { get; set; }
        public int Duration { get; set; }
        public string DurationFormated
        {
            get
            {
                return Helpers.MillisecondsToStringAlt(Duration);
            }
        }
    }
}
