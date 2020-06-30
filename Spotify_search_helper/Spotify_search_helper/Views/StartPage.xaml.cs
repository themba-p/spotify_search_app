using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Spotify_search_helper.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        public StartPage()
        {
            this.InitializeComponent();
            Initialize();
            this.Loaded += Page_Loaded;

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

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //ViewModels.MainPageViewModel.Current.LoadTheme();
        }
    }
}
