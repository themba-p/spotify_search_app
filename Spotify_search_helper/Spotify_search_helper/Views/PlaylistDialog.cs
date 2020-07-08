using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Spotify_search_helper.Views
{
    public sealed partial class PlaylistDialog : ContentDialog
    {
        public static PlaylistDialog Current;
        public PlaylistDialog()
        {
            this.InitializeComponent();
            Current = this;
        }
    }
}
