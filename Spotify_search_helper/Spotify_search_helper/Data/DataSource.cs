using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Toolkit.Collections;
using Spotify_search_helper.Models;
using SpotifyAPI.Web;
using Windows.UI.Xaml.Media.Imaging;

namespace Spotify_search_helper.Data
{
    public class DataSource
    {
        public static DataSource Current = null;

        private int startIndex = 0;
        private readonly int limit = 12;
        private Paging<SimplePlaylist> page;

        public DataSource() 
        {
            Current = this;
        }

        public async Task Initialize()
        {
            if(await Authentication.GetClient() != null)
                ViewModels.MainPageViewModel.Current.IsLoading = false;
        }

        public ProfileUser Profile { get; set; }

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
                        Profile = new ProfileUser(user.Id, user.DisplayName, image);
                        return Profile;
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

        public async void GetOther()
        {
            ViewModels.MainPageViewModel.Current.IsLoading = true;

            if(page != null && startIndex < page.Total)
            {
                while (startIndex < page.Total)
                {
                    var files = await GetPlaylists();
                    ViewModels.MainPageViewModel.Current.AddToCollection(files);

                    //delay to avoid api limit
                    //await Task.Delay(3000);
                }

                //update counts 
                ViewModels.MainPageViewModel.Current.UpdatePlaylistCategoryCounts();
            }

            ViewModels.MainPageViewModel.Current.IsLoading = false;
        }

        public async Task<List<Playlist>> GetPlaylists()
        {
            var results = new List<Playlist>();

            PlaylistCurrentUsersRequest request = new PlaylistCurrentUsersRequest
            {
                Limit = limit,
                Offset = startIndex
            };
            var spotify = await Authentication.GetClient();
            var result = await spotify.Playlists.CurrentUsers(request);
            if (startIndex == 0)
            {
                page = result;
                ViewModels.MainPageViewModel.Current.UpdateSelectedPlaylistCategory(page.Total);
            }

            if(result != null && result.Items != null)
            {
                BitmapImage image = null;
                PlaylistCategoryType categoryType = PlaylistCategoryType.All; 
                foreach (var item in result.Items)
                {
                    if(Profile != null && item.Owner != null)
                    {
                        if (item.Owner.Id == Profile.Id)
                            categoryType = PlaylistCategoryType.MyPlaylist;
                        else
                        {
                            if (Models.Category._personalizedPlaylistNames.Find(c => c.ToLower().Equals(item.Name.ToLower())) != null)
                                categoryType = PlaylistCategoryType.MadeForYou;
                            else
                                categoryType = PlaylistCategoryType.Following;
                        }
                    }

                    if (item.Images != null && item.Images.Count > 0)
                        image = new BitmapImage(new Uri(item.Images.FirstOrDefault().Url));
                    results.Add(new Playlist(item.Id, item.Name, ViewModels.Helpers.CleanString(item.Description), item.Owner.DisplayName, item.Owner.Id, image, categoryType));
                }
            }

            startIndex += results.Count;
            return results;
        }
    }


    /// <summary>
    /// A sample implementation of the <see cref="Collections.IIncrementalSource{TSource}"/> interface.
    /// </summary>
    /// <seealso cref="Collections.IIncrementalSource{TSource}"/>
    public class PlaylistSource : IIncrementalSource<Playlist>
    {
        private int startIndex = 0;
        private readonly int limit = 6;
        private readonly List<SimplePlaylist> _playlist;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaylistSource"/> class.
        /// </summary>
        public PlaylistSource()
        {
            _playlist = new List<SimplePlaylist>();
            Load();
        }

        private Paging<SimplePlaylist> page;

        private async void Load()
        {
            // we need the first page
            var spotify = await Authentication.GetClient();
            PlaylistCurrentUsersRequest request = new PlaylistCurrentUsersRequest
            {
                Limit = limit,
                Offset = startIndex
            };

            page = await spotify.Playlists.CurrentUsers(request);

            if (page != null) ViewModels.MainPageViewModel.Current.UpdateSelectedPlaylistCategory(page.Total);
        }

        /// <summary>
        /// Retrieves items based on <paramref name="pageIndex"/> and <paramref name="pageSize"/> arguments.
        /// </summary>
        /// <param name="pageIndex">
        /// The zero-based index of the page that corresponds to the items to retrieve.
        /// </param>
        /// <param name="pageSize">
        /// The number of <see cref="Person"/> items to retrieve for the specified <paramref name="pageIndex"/>.
        /// </param>
        /// <param name="cancellationToken">
        /// Used to propagate notification that operation should be canceled.
        /// </param>
        /// <returns>
        /// Returns a collection of <see cref="Person"/>.
        /// </returns>
        public async Task<IEnumerable<Playlist>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
        {
            //update loading 
            ViewModels.MainPageViewModel.Current.IsLoading = true;

            //get current filter type
            var category = ViewModels.MainPageViewModel.Current.GetPlaylistCategory();
            var profile = await DataSource.Current.GetProfile();

            var results = new List<Playlist>();
            var items = new List<SimplePlaylist>();
            PlaylistCurrentUsersRequest request = new PlaylistCurrentUsersRequest
            {
                Limit = limit,
                Offset = startIndex
            };
            
            if (_playlist.Count > startIndex)
            {
                if ((startIndex + limit) <= _playlist.Count)
                {
                    items.AddRange(_playlist.GetRange(startIndex, limit));
                    startIndex += limit;
                }
                else
                {
                    int extraItems = (startIndex + limit) - _playlist.Count;
                    if (extraItems > 0)
                    {
                        items.AddRange(_playlist.GetRange(startIndex, _playlist.Count));
                        startIndex += _playlist.Count - startIndex;

                        //get the rest of the items from spotify
                        var spotify = await Authentication.GetClient();
                        var result = await spotify.Playlists.CurrentUsers(request);
                        if (result != null && result.Items != null)
                        {
                            items.AddRange(result.Items);
                            startIndex += result.Items.Count;
                        }
                    }
                }
            }
            else
            {
                var spotify = await Authentication.GetClient();
                var result = await spotify.Playlists.CurrentUsers(request);
                if (result != null && result.Items != null)
                {
                    items.AddRange(result.Items);
                    startIndex += items.Count;
                }
            }

            if (items.Count > 0)
            {
                Windows.UI.Xaml.Media.ImageSource image;

                foreach (var item in items)
                {
                    if (_playlist.Find(c => c.Id == item.Id) == null)
                        _playlist.Add(item);

                    image = null;

                    if (item.Owner != null && profile != null)
                    {
                        switch (category)
                        {
                            case PlaylistCategoryType.MadeForYou:
                                break;
                            case PlaylistCategoryType.MyPlaylist:
                                if (item.Owner.Id == profile.Id)
                                {
                                    if (item.Images != null && item.Images.Count > 0)
                                        image = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(item.Images.FirstOrDefault().Url));
                                    results.Add(new Playlist(item.Id, item.Name, ViewModels.Helpers.CleanString(item.Description), item.Owner.DisplayName, item.Owner.Id, image, PlaylistCategoryType.MyPlaylist));
                                }
                                break;
                            case PlaylistCategoryType.Following:
                                if (item.Owner.Id != profile.Id)
                                {
                                    if (item.Images != null && item.Images.Count > 0)
                                        image = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(item.Images.FirstOrDefault().Url));
                                    results.Add(new Playlist(item.Id, item.Name, ViewModels.Helpers.CleanString(item.Description), item.Owner.DisplayName, item.Owner.Id, image, PlaylistCategoryType.Following));
                                }
                                break;
                            case PlaylistCategoryType.All:
                            default:
                                if (item.Images != null && item.Images.Count > 0)
                                    image = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(item.Images.FirstOrDefault().Url));
                                results.Add(new Playlist(item.Id, item.Name, ViewModels.Helpers.CleanString(item.Description), item.Owner.DisplayName, item.Owner.Id, image, PlaylistCategoryType.All));
                                break;
                        }
                    }       
                }
            }

            if (pageIndex == 0)
            {
                await Task.Delay(2000);
            }
            else
            {
                await Task.Delay(1000);
            }

            ViewModels.MainPageViewModel.Current.IsLoading = false;

            return results;
        }
    }
}
