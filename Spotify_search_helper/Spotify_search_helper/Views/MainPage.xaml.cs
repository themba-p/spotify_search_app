using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI;
using Windows.UI.Xaml.Media.Animation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Spotify_search_helper.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;

        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            Window.Current.SetTitleBar(DragGrid);
            Initialize();

            this.Loaded += MainPage_Loaded;
        }

        private void Initialize()
        {
            // Reset app back to normal.
            StatusBarExtensions.SetIsVisible(this, false);

            ApplicationViewExtensions.SetTitle(this, string.Empty);

            var lightGreyBrush = (Color)Application.Current.Resources["Status-bar-foreground"];
            var statusBarColor = (Color)Application.Current.Resources["Status-bar-color"];
            var brandColor = (Color)Application.Current.Resources["BrandColorThemeColor"];

            ApplicationViewExtensions.SetTitle(this, "Spotify Companion");
            StatusBarExtensions.SetBackgroundOpacity(this, 0.8);
            TitleBarExtensions.SetButtonBackgroundColor(this, statusBarColor);
            TitleBarExtensions.SetButtonForegroundColor(this, lightGreyBrush);
            TitleBarExtensions.SetBackgroundColor(this, statusBarColor);
            TitleBarExtensions.SetForegroundColor(this, lightGreyBrush);
            TitleBarExtensions.SetButtonBackgroundColor(this, Colors.Transparent);
            TitleBarExtensions.SetButtonHoverBackgroundColor(this, brandColor);
            TitleBarExtensions.SetButtonHoverForegroundColor(this, Colors.White);
        }

        public async void ShowCreatePlaylistDialogAsync()
        {
            MergePlaylistDialog dialog = new MergePlaylistDialog();
            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                //how to get text
            }
            else
            {
                dialog.Hide();
            }
        }

        public void HideCreatePlaylistDialog()
        {
            if (MergePlaylistDialog.Current != null)
                MergePlaylistDialog.Current.Hide();
        }

        public async void ShowDeleteConfirmDialog()
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Unfollow playlists?",
                Content = "Are you sure you want to unfollow the selected playlists?",
                CloseButtonText = "Cancel",
                PrimaryButtonText = "Unfollow",
                DefaultButton = ContentDialogButton.Primary,
                
            };

            var result = await dialog.ShowAsync();
            switch (result)
            {
                case ContentDialogResult.None:
                    break;
                case ContentDialogResult.Primary:
                    ViewModels.MainPageViewModel.Current.UnfollowSelectedPlaylists();
                    break;
                case ContentDialogResult.Secondary:
                    break;
            }
        }

        public void DoIt(object item)
        {
            try
            {
                ConnectedAnimation ConnectedAnimation = PlaylistContentView.PrepareConnectedAnimation("forwardAnimation", item, "connectedElement");
                ConnectedAnimation.Configuration = new BasicConnectedAnimationConfiguration();
                ConnectedAnimation.TryStart(destinationElement);
            }
            catch (Exception)
            {

            }
        }

        public async void UndoIt(object item)
        {
            PlaylistContentView.ScrollIntoView(item, ScrollIntoViewAlignment.Default);
            PlaylistContentView.UpdateLayout();

            ConnectedAnimation ConnectedAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("backwardsAnimation", destinationElement);
            ConnectedAnimation.Completed += ConnectedAnimation_Completed;
            ConnectedAnimation.Configuration = new BasicConnectedAnimationConfiguration();
            await PlaylistContentView.TryStartConnectedAnimationAsync(ConnectedAnimation, item, "connectedElement");
        }

        public void ScrollTracksViewToTop()
        {
            if (TracksListView.Items != null && TracksListView.Items.FirstOrDefault() != null)
                TracksListView.ScrollIntoView(TracksListView.Items.FirstOrDefault(), ScrollIntoViewAlignment.Leading);

        }

        public void ScrollSelectedPlaylistViewToTop()
        {
            try
            {
                if (SelectedPlaylistViewExpanded.Items != null)
                    SelectedPlaylistViewExpanded.ScrollIntoView(SelectedPlaylistViewExpanded.Items.FirstOrDefault());
            }
            catch (Exception)
            {
                //
            }
        }

        public void ScrollToPlaylistAlphabet(object item)
        {
            PlaylistContentView.ScrollIntoView(item, ScrollIntoViewAlignment.Leading);
        }

        public void ScrollPlaylistViewToTop()
        {
            if (PlaylistContentView.Items != null && PlaylistContentView.Items.FirstOrDefault() != null)
                PlaylistContentView.ScrollIntoView(PlaylistContentView.Items.FirstOrDefault());
        }

        public void ToggleDarkTheme(bool isEnabled)
        {
            if (isEnabled)
            {
                this.Frame.RequestedTheme = ElementTheme.Dark;
            }
            else
            {
                this.Frame.RequestedTheme = ElementTheme.Light;
            }
        }

        private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ViewModels.MainPageViewModel.Current.FilterAdvancedCollectionView();
        }

        private void SearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            //Display details of playlist, Tracks, playlist info etc.
            if (args.SelectedItem != null && args.SelectedItem is Models.Playlist item)
            {
                ViewModels.MainPageViewModel.Current.SelectedSearchItem(item);
            }
        }

        private void TrackSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            ViewModels.MainPageViewModel.Current.FilterTracksCollectionView(args.QueryText);
        }

        private void TrackSearchBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            ViewModels.MainPageViewModel.Current.TrackSuggestionChosen(args.SelectedItem as Models.Track);
        }

        private void ConnectedAnimation_Completed(ConnectedAnimation sender, object args)
        {
            ViewModels.MainPageViewModel.Current.IsPopupActive = false;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModels.MainPageViewModel.Current.LoadTheme();
        }
    }

}
