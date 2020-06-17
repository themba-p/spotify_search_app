using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spotify_search_helper.Models;
using SpotifyAPI.Web;

namespace Spotify_search_helper.Data
{
    public class DataSource
    {
        public static DataSource Current = null;
        public DataSource() 
        {
            Current = this;
        }

        public async Task Initialize()
        {
            if(await Authentication.GetClient() != null)
                ViewModels.MainPageViewModel.Current.IsLoading = false;
        }

        private ProfileUser _profile;
        public ProfileUser Profile
        {
            get => _profile;
            set => _profile = value;
        }

        public static async void AuthComplete()
        {
            await DataSource.Current.GetProfile();
            ViewModels.MainPageViewModel.Current.IsLoading = false;
        }

        public async Task<ProfileUser> GetProfile()
        {
            try
            {
                if (Profile != null)
                    return Profile;
                else
                {
                    var spotify = await Authentication.GetClient();
                    if (spotify != null)
                    {
                        var user = await spotify.UserProfile.Current();
                        Windows.UI.Xaml.Media.ImageSource image = null;
                        if (user.Images != null) image = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(user.Images.FirstOrDefault().Url));
                        return new ProfileUser(user.Id, user.DisplayName, image);
                    }
                    return null;
                }
            }
            catch (Exception e)
            {
                ViewModels.Helpers.DisplayDialog("Error", e.Message);
                return null;
            }
        }

        private async Task<IList<SimplePlaylist>> GetUserPlaylist()
        {
            var spotify = await Authentication.GetClient();
            try
            {
                var playlists = await spotify.PaginateAll(spotify.Playlists.CurrentUsers());
                return playlists;
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
