﻿using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Input;
using System.Numerics;
using Windows.ApplicationModel.Core;

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
            Initialize();

            this.Loaded += MainPage_Loaded;
        }

        private void Initialize()
        {
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
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModels.MainPageViewModel.Current.LoadTheme();
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

        public void ScrollTracksViewToTop()
        {
            if(TracksListView.Items != null && TracksListView.Items.FirstOrDefault() != null)
            TracksListView.ScrollIntoView(TracksListView.Items.FirstOrDefault(), ScrollIntoViewAlignment.Leading);

        }
    }

}
