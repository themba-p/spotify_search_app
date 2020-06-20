using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Input;
using System.Numerics;

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

            // Reset app back to normal.
            StatusBarExtensions.SetIsVisible(this, false);

            ApplicationViewExtensions.SetTitle(this, string.Empty);

            var lightGreyBrush = (Color)Application.Current.Resources["Status-bar-foreground"];
            var brandColor = (Color)Application.Current.Resources["Status-bar-color"];
            
            ApplicationViewExtensions.SetTitle(this, "Spotify Companion");
            StatusBarExtensions.SetBackgroundOpacity(this, 0.8);
            TitleBarExtensions.SetButtonBackgroundColor(this, brandColor);
            TitleBarExtensions.SetButtonForegroundColor(this, lightGreyBrush);
            TitleBarExtensions.SetBackgroundColor(this, brandColor);
            TitleBarExtensions.SetForegroundColor(this, lightGreyBrush);

            this.Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModels.MainPageViewModel.Current.LoadTheme();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ViewModels.MainPageViewModel.Current.FilterAdvancedCollectionView();
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
    }

}
