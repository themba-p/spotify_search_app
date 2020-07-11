namespace Spotify_search_helper.Models
{
    public class MessengerHelper
    {
        public object Item { get; set; }
        public MessengerAction Action { get; set; }
        public TargetView Target { get; set; }
    }

    public enum MessengerAction
    {
        ScrollToItem, 
        RightTapped
    }

    public enum TargetView
    {
        Playlist,
        Tracks, 
        SelectedPlaylist,
        Alphabet
    }

    public enum MediaControlType
    {
        Play,
        Pause,
        Stop
    }
}
