using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI;
using Windows.UI.Xaml.Media.Animation;
using Spotify_search_helper.Models;
using Spotify_search_helper.Data;
using GalaSoft.MvvmLight.Messaging;
using Spotify_search_helper.ViewModels;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Composition;
using Windows.UI.Xaml.Input;
using System.Numerics;
using muxc = Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Spotify_search_helper.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        private readonly PlaylistDialog _createPlaylistDialog = null;
        private readonly AddToPlaylistDialog _addToPlaylistDialog = null;
        private ContentDialog _dialog = null;

        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            Window.Current.SetTitleBar(DragGrid);
            Initialize();
            _createPlaylistDialog = new PlaylistDialog();
            _addToPlaylistDialog = new AddToPlaylistDialog();
            RegisterMessenger();

            this.SizeChanged += MainPage_SizeChanged;
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Messenger.Default.Send(e);           
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

        private void RegisterMessenger()
        {
            Messenger.Default.Register<ElementTheme>(this, ToggleTheme);
            Messenger.Default.Register<MediaControlType>(this, MediaControl);
            Messenger.Default.Register<DialogManager>(this, DialogManage);
            Messenger.Default.Register<MessengerHelper>(this, HandleMessengerHelper);
        }

        private void HandleMessengerHelper(MessengerHelper messenger)
        {
            try
            {
                switch (messenger.Action)
                {
                    case MessengerAction.ScrollToItem:
                        switch (messenger.Target)
                        {
                            case TargetView.Playlist:
                                if (messenger.Item == null && PlaylistContentView.Items != null) messenger.Item = PlaylistContentView.Items.FirstOrDefault();
                                PlaylistContentView.ScrollIntoView(messenger.Item, ScrollIntoViewAlignment.Leading);
                                break;
                            case TargetView.Tracks:
                                if (messenger.Item == null && TracksListView.Items != null) messenger.Item = TracksListView.Items.FirstOrDefault();
                                TracksListView.ScrollIntoView(messenger.Item, ScrollIntoViewAlignment.Leading);
                                break;
                            case TargetView.SelectedPlaylist:
                                if (messenger.Item == null && SelectedPlaylistViewExpanded.Items != null) messenger.Item = SelectedPlaylistViewExpanded.Items.FirstOrDefault();
                                SelectedPlaylistViewExpanded.ScrollIntoView(messenger.Item, ScrollIntoViewAlignment.Leading);
                                break;
                            case TargetView.Alphabet:
                                PlaylistContentView.ScrollIntoView(messenger.Item, ScrollIntoViewAlignment.Leading);
                                break;
                        }
                        break;                        
                }
            }
            catch (Exception)
            {

            }
        }

        private async void DialogManage(DialogManager manager)
        {
            switch (manager.Type)
            {
                case DialogType.AddToPlaylist:
                    if (manager.Action == DialogAction.Show)
                        await _addToPlaylistDialog.ShowAsync();
                    else
                        _addToPlaylistDialog.Hide();
                    break;
                case DialogType.Merge:
                case DialogType.CreatePlaylist:
                case DialogType.EditPlaylist:
                    if (manager.Action == DialogAction.Show)
                        await _createPlaylistDialog.ShowAsync();
                    else
                        _createPlaylistDialog.Hide();
                    break;
                case DialogType.Default:
                case DialogType.Unfollow:
                    _dialog = new ContentDialog
                    {
                        Title = manager.Title,
                        Content = manager.Message,
                        PrimaryButtonText = manager.PrimaryButtonText,
                        SecondaryButtonText = manager.SecondaryButtonText,
                        DefaultButton = ContentDialogButton.Primary
                    };
                    
                    Messenger.Default.Send(new DialogResult(DialogType.Unfollow, await _dialog.ShowAsync(), manager.Item));
                    break;
            }
        }

        public void MediaControl(MediaControlType controlType)
        {   
            switch (controlType)
            {
                case MediaControlType.Play:
                    mediaElement.Play();
                    break;
                case MediaControlType.Pause:
                    mediaElement.Pause();
                    break;
                case MediaControlType.Stop:
                    mediaElement.Stop();
                    break;
            }
            
        }

        public void ToggleTheme(ElementTheme theme)
        {
            this.Frame.RequestedTheme = theme;
        }

        public void DoIt(object item)
        {
            try
            {
                ConnectedAnimation ConnectedAnimation = PlaylistContentView.PrepareConnectedAnimation("forwardAnimation", item, "connectedElement");
                ConnectedAnimation.Configuration = new DirectConnectedAnimationConfiguration();
                ConnectedAnimation.TryStart(destinationElement);
            }
            catch (Exception)
            {

            }
        }

        public async void UndoIt(object item)
        {
            try
            {
                PlaylistContentView.ScrollIntoView(item, ScrollIntoViewAlignment.Default);
                PlaylistContentView.UpdateLayout();
                MainPageViewModel.Current.IsPopupActive = false;

                ConnectedAnimation ConnectedAnimation = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("backwardsAnimation", destinationElement);
                //ConnectedAnimation.Completed += ConnectedAnimation_Completed;
                ConnectedAnimation.Configuration = new DirectConnectedAnimationConfiguration();
                await PlaylistContentView.TryStartConnectedAnimationAsync(ConnectedAnimation, item, "connectedElement");
            }
            catch (Exception)
            {

            }
        }

        private void PlaylistItem_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            FlyoutShowOptions options = new FlyoutShowOptions
            {
                Placement = FlyoutPlacementMode.RightEdgeAlignedTop,
                Position = e.GetPosition(senderElement),
                ShowMode = FlyoutShowMode.Standard
            };
            flyoutBase.ShowAt(senderElement, options);
            //update focus state
            Messenger.Default.Send(new MessengerHelper
            {
                Action = MessengerAction.RightTapped,
                Target = TargetView.Playlist,
                Item = senderElement.DataContext as ItemBase
            });
            //MainPageViewModel.Current.UpdatePlaylistFocusState(senderElement.DataContext as ItemBase);
            e.Handled = true;

            flyoutBase.Closed += FlyoutBase_Closed;
        }

        private void FlyoutBase_Closed(object sender, object e)
        {
            MainPageViewModel.Current.ResetPlaylistFocusState();
        }

        private void TrackItem_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            FlyoutShowOptions options = new FlyoutShowOptions
            {
                Placement = FlyoutPlacementMode.RightEdgeAlignedTop,
                Position = e.GetPosition(senderElement),
                ShowMode = FlyoutShowMode.Standard
            };
            flyoutBase.ShowAt(senderElement, options);
            //update focus state
            Messenger.Default.Send(new MessengerHelper
            {
                Action = MessengerAction.RightTapped,
                Target = TargetView.Tracks,
                Item = senderElement.DataContext as ItemBase
            });
            
            e.Handled = true;

            flyoutBase.Closed += TracksFlyoutBase_Closed;
        }

        private void TracksFlyoutBase_Closed(object sender, object e)
        {
            MainPageViewModel.Current.ResetTracksFocusState();
        }
    }

}
