using Spotify_search_helper.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class Track : NotificationBase
    {
        public Track() { }

        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Uri { get; set; }
        public int TrackNumber { get; set; }
        public int DurationMs { get; set; }
        public string ArtistsStr { get; set; }
        public ImageSource Image { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(this._isSelected, value, () => this._isSelected = value); }
        }
    }
}
