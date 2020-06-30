using Microsoft.Toolkit.Collections;
using Spotify_search_helper.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace Spotify_search_helper.Data
{
    public class DataSource
    {
        public static DataSource Current = null;
        private readonly List<SimplePlaylist> _playlist = new List<SimplePlaylist>();

        private int startIndex = 0;
        private readonly int limit = 12;
        private Paging<SimplePlaylist> page;
        private Paging<PlaylistTrack<IPlayableItem>> _currentPlaylistPage;
        public ProfileUser Profile { get; set; }

        public DataSource()
        {
            Current = this;
        }

        /// <summary>
        /// opens a dialog to to authenticate user and give the app permissions.
        /// </summary>
        /// <returns></returns>
        public async Task Authenticate()
        {
            await Authentication.Authenticate();
        }

        /// <summary>
        /// Checks if the current user has been authenticated with a valid access token if any.
        /// </summary>
        /// <returns>
        /// True if authenticated, false othewise
        /// </returns>
        public async Task<bool> IsAuthenticated()
        {
            try
            {
                var currentUser = await Authentication.IsAuthenticated();
                Windows.UI.Xaml.Media.ImageSource image = null;
                if (currentUser.Images != null) image = new BitmapImage(new Uri(currentUser.Images.FirstOrDefault().Url));
                Profile = new ProfileUser(currentUser.Id, currentUser.DisplayName, image);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets current users profile.
        /// </summary>
        /// <returns>
        /// The current users profile.
        /// </returns>
        public async Task<ProfileUser> GetProfile()
        {
            try
            {
                if (Profile != null)
                    return Profile;
                else
                {
                    var spotify = await Authentication.GetSpotifyClientAsync();
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

            if (page != null && startIndex < page.Total)
            {
                while (startIndex < page.Total)
                {
                    var files = await GetPlaylists();
                    ViewModels.MainPageViewModel.Current.AddToCollection(files);

                    //delay to avoid api limit
                    //await Task.Delay(3000);
                }

                //update counts 
                ViewModels.MainPageViewModel.Current.UpdatePlaylistCategoryCount();
            }

            ViewModels.MainPageViewModel.Current.IsLoading = false;
        }

        /// <summary>
        /// Gets a list of the current users playlists, including following and own playlists.
        /// </summary>
        /// <returns>
        /// The list of playlists.
        /// </returns>
        public async Task<List<Playlist>> GetPlaylists()
        {
            var results = new List<Playlist>();

            PlaylistCurrentUsersRequest request = new PlaylistCurrentUsersRequest
            {
                Limit = limit,
                Offset = startIndex
            };
            var spotify = await Authentication.GetSpotifyClientAsync();

            if (spotify != null)
            {
                var result = await spotify.Playlists.CurrentUsers(request);
                if (startIndex == 0)
                {
                    page = result;
                }

                if (result != null && result.Items != null)
                {
                    BitmapImage image = null;
                    int itemsCount = 0;
                    PlaylistCategoryType categoryType = PlaylistCategoryType.All;
                    foreach (var item in result.Items)
                    {
                        if (_playlist.Find(c => c.Id == item.Id) == null)
                            _playlist.Add(item);

                        if (Profile != null && item.Owner != null)
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

                        if (item.Tracks != null) itemsCount = item.Tracks.Total;

                        results.Add(new Playlist(item.Id, item.Name, ViewModels.Helpers.CleanString(item.Description),
                            item.Owner.DisplayName, item.Owner.Id, image, categoryType, item.Uri, itemsCount));

                        image = null;
                        itemsCount = 0;
                        categoryType = PlaylistCategoryType.All;
                    }
                }

                startIndex += results.Count;
                return results;
            }
            else
            {
                ViewModels.Helpers.DisplayDialog("Unable to load playlists", "An error occured, could not load items. Please give it another shot and make sure your internet connection is working"); ;
                return null;
            }
        }

        /// <summary>
        /// Plays a list of tracks.
        /// </summary>
        /// <param name="tracks">
        /// The list of of tracks to play.
        /// </param>
        /// <param name="index">
        /// A position where playback is to be started (Optional). Default value is 0, starts at beginning of list.
        /// </param>
        /// <returns>
        /// True if playback was successful, false otherwise.
        /// </returns>
        public async Task<bool> PlaySpotifyTracks(List<Track> tracks, int index = 0)
        {
            try
            {
                return await PlaySpotifyMedia(tracks.Select(c => c.Uri).ToList(), index);
            }
            catch (Exception e)
            {
                ViewModels.Helpers.DisplayDialog("Cannot play items", e.Message);
                return false;
            }
        }

        /// <summary>
        /// Plays a list of Playlists with option to shuffle.
        /// </summary>
        /// <param name="playlists">
        /// The list of playlist to play.
        /// </param>
        /// <param name="shuffle">
        /// Turn on shuffle (Optional) default is false.
        /// </param>
        /// <returns>
        /// <see cref="bool"/> value, true if successful, false otherwise.
        /// </returns>
        public async Task<bool> PlaySpotifyItems(List<Playlist> playlists, bool shuffle = false)
        {
            try
            {
                var spotify = await Authentication.GetSpotifyClientAsync();

                List<Track> tracks = new List<Track>();
                List<string> uris = new List<string>();
                foreach (var item in playlists)
                {
                    var trackItems = await GetSpotifyTracks(item.Id);
                    if (trackItems != null)
                    {
                        var fullItems = await GetOtherSpotifyTracks();
                        if (fullItems != null) tracks.AddRange(fullItems);
                    }
                }
                uris.AddRange(tracks.Select(c => c.Uri));

                return await PlaySpotifyMedia(uris, 0, shuffle);
            }
            catch (Exception e)
            {
                ViewModels.Helpers.DisplayDialog("Cannot play items", e.Message);
                return false;
            }
        }

        private async Task<bool> PlaySpotifyMedia(List<string> uris, int index = 0, bool shuffle = false)
        {
            try
            {
                var spotify = await Authentication.GetSpotifyClientAsync();
                if (spotify == null) return false;
                if (uris.Count > 0)
                {
                    PlayerResumePlaybackRequest request = new PlayerResumePlaybackRequest
                    {
                        Uris = uris,
                        OffsetParam = new PlayerResumePlaybackRequest.Offset { Position = index },
                    };

                    if (await spotify.Player.ResumePlayback(request))
                    {
                        if (shuffle) await spotify.Player.SetShuffle(new PlayerShuffleRequest(true));
                        return true;
                    }
                    else
                    {
                        return await ViewModels.Helpers.LaunchUri(uris[index]);
                    }

                }
                else
                {
                    ViewModels.Helpers.DisplayDialog("Cannot play items", "An error occured, could not play items. Please give it another shot and make sure your internet connection is working");
                    return false;
                }
            }
            catch (Exception e)
            {
                if (!await ViewModels.Helpers.LaunchUri(uris[index]))
                {
                    ViewModels.Helpers.DisplayDialog("Cannot play items", e.Message);
                    return false;
                }
                return true;
            }
        }

        public List<Playlist> ConvertPlaylists(IEnumerable<FullPlaylist> playlists)
        {
            try
            {
                List<Playlist> results = new List<Playlist>();
                BitmapImage image = null;
                int itemsCount = 0;

                foreach (var item in playlists)
                {
                    if (item.Images != null && item.Images.Count > 0)
                        image = new BitmapImage(new Uri(item.Images.FirstOrDefault().Url));

                    if (item.Tracks != null) itemsCount = item.Tracks.Total;

                    results.Add(new Playlist
                    {
                        Id = item.Id,
                        Title = item.Name,
                        Owner = item.Owner.DisplayName,
                        OwnerId = item.Owner.Id,
                        Image = image,
                        CategoryType = PlaylistCategoryType.MyPlaylist,
                        Uri = item.Uri,
                        ItemsCount = itemsCount
                    });

                    image = null;
                    itemsCount = 0;
                }
                return results;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a Spotify track <see cref="PlaylistTrack{T}"/> into <see cref="Track"/>
        /// </summary>
        /// <param name="tracks">
        /// The list of tracks to convert.
        /// </param>
        /// <returns>
        /// The converted tracks as a list.
        /// </returns>
        private List<Track> ConvertTracks(IEnumerable<PlaylistTrack<IPlayableItem>> tracks)
        {
            try
            {
                List<Track> results = new List<Track>();
                Track item;
                BitmapImage image = null;
                foreach (var playlistTrack in tracks)
                {
                    if (playlistTrack.Track is FullTrack track)
                    {
                        item = new Track
                        {
                            Id = track.Id,
                            Title = track.Name,
                            Uri = track.Uri,
                            TrackNumber = track.TrackNumber,
                            DurationMs = track.DurationMs,
                            ArtistsStr = ""
                        };

                        if (track.Album != null)
                        {
                            item.Album = track.Album.Name;
                            //load track image after
                            if (track.Album.Images != null && track.Album.Images.Count > 0)
                            {
                                image = new BitmapImage(new Uri(track.Album.Images.FirstOrDefault().Url));
                                item.Image = image;
                            }
                        }
                        if (track.Artists != null && track.Artists.Count > 0)
                        {
                            if (track.Artists.Count == 1)
                                item.ArtistsStr = track.Artists.FirstOrDefault().Name;
                            else
                            {
                                for (int i = 0; i < track.Artists.Count; i++)
                                {
                                    if (i != (track.Artists.Count - 1))
                                        item.ArtistsStr += track.Artists[i].Name + ", ";
                                    else
                                        item.ArtistsStr += track.Artists[i].Name;
                                }
                            }
                        }
                        else
                            item.ArtistsStr = "Unknown";

                        results.Add(item);
                    }

                    image = null;
                }

                return results;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the spotify tracks for the playlist with the asscociated id.
        /// Works in conjuction with <see cref="GetOtherSpotifyTracks"/>. This loads the first batch of results.
        /// </summary>
        /// <param name="id">
        /// The id of the playlist the tracks belong to.
        /// </param>
        /// <returns>
        /// The list of tracks for the specified playlist.
        /// </returns>
        public async Task<List<Track>> GetSpotifyTracks(string id)
        {
            try
            {

                List<Track> results = new List<Track>();
                var spotify = await Authentication.GetSpotifyClientAsync();
                var fpl = await spotify.Playlists.Get(id);
                _currentPlaylistPage = fpl.Tracks;
                return ConvertTracks(fpl.Tracks.Items);
            }
            catch (Exception)
            {
                _currentPlaylistPage = null;
                ViewModels.Helpers.DisplayDialog("Error", "An error occured, please give it another shot and make sure your internet connection is working");
                return null;
            }
        }

        /// <summary>
        /// Get's the rest of the spotify tracks after loading the first limit.
        /// Allows loading the first batch for immediate access and then load the rest later.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Track>> GetOtherSpotifyTracks()
        {
            try
            {
                var spotify = await Authentication.GetSpotifyClientAsync();
                var items = ConvertTracks(await spotify.PaginateAll(_currentPlaylistPage));
                _currentPlaylistPage = null;
                return items;
            }
            catch (Exception e)
            {
                ViewModels.Helpers.DisplayDialog("Cannot load items", e.Message);
                return null;
            }
        }

        public async Task<bool> RemoveItemFromPlaylist(string playlistId, List<string> uris)
        {
            try
            {
                if (uris == null || uris.Count <= 0)
                    return false;

                var spotify = await Authentication.GetSpotifyClientAsync();
                if (spotify == null)
                    return false;

                List<PlaylistRemoveItemsRequest.Item> trackUris = new List<PlaylistRemoveItemsRequest.Item>();

                foreach (var uri in uris)
                {
                    trackUris.Add(new PlaylistRemoveItemsRequest.Item { Uri = uri });
                }

                //Remove multiple tracks
                await spotify.Playlists.RemoveItems(playlistId, new PlaylistRemoveItemsRequest(trackUris));
                return true;
            }
            catch (Exception)
            {

                throw;
            }
        }

        /// <summary>
        /// Creates a new playlist for current user.
        /// </summary>
        /// <param name="name">
        /// The name of the new playlist.
        /// </param>
        /// <param name="tracks">
        /// The tracks to add to the newly created playlist (Optional).
        /// </param>
        /// <returns></returns>
        public async Task<FullPlaylist> CreateSpotifyPlaylist(string name, IEnumerable<Track> tracks = null, string base64Jpg = null)
        {
            try
            {
                var spotify = await Authentication.GetSpotifyClientAsync();
                PlaylistCreateRequest request = new PlaylistCreateRequest(name);
                var playlist = await spotify.Playlists.Create(Profile.Id, request);

                if (tracks != null && tracks.Count() > 0)
                {
                    var plRequest = new PlaylistAddItemsRequest(tracks.Select(c => c.Uri).ToList());
                    await spotify.Playlists.AddItems(playlist.Id, plRequest);
                }

                try
                {
                    if (!string.IsNullOrEmpty(base64Jpg))
                        await spotify.Playlists.UploadCover(playlist.Id, base64Jpg); //how to handle image data thats > 256kb?
                }
                catch (Exception)
                {

                }
                return await spotify.Playlists.Get(playlist.Id);
            }
            catch (Exception)
            {
                ViewModels.Helpers.DisplayDialog("Error", "An error occured while creating your playlist");
                return null;
            }
        }

        /// <summary>
        /// Merges 2 or more playlists into 1 new playlist.
        /// </summary>
        /// <param name="name">
        /// The name of the new playlist.
        /// </param>
        /// <param name="playlists">
        /// The list of playlists to merge.
        /// </param>
        /// <param name="base64Jpg">
        /// The playlist cover in base64 string format (Optional). 
        /// </param>
        /// <param name="img">
        /// The cover image if using a custom cover image for the playlist.
        /// </param>
        /// <returns>
        /// The newly create playlist.
        /// </returns>
        public async Task<Playlist> MergeSpotifyPlaylists(string name, IEnumerable<Playlist> playlists, string base64Jpg = null, BitmapImage img = null)
        {
            try
            {
                if (playlists.Count() <= 1)
                    return null;

                var spotify = await Authentication.GetSpotifyClientAsync();
                List<Track> tracks = new List<Track>();

                foreach (var playlist in playlists)
                {
                    var items1 = await GetSpotifyTracks(playlist.Id);
                    if (items1 != null && items1.Count < playlist.ItemsCount)
                    {
                        //handle duplicates
                        var items2 = await GetOtherSpotifyTracks();
                        if (items2 != null)
                        {
                            foreach (var track in items2)
                            {
                                if (!tracks.Any(c => c.Id == track.Id))
                                    tracks.Add(track);
                            }
                        }
                    }
                    else if (items1 != null)
                    {
                        foreach (var track in items1)
                        {
                            if (!tracks.Any(c => c.Id == track.Id))
                                tracks.Add(track);
                        }
                    }
                }

                int itemsCount = 0;
                Windows.UI.Xaml.Media.ImageSource image = null;
                var item = await CreateSpotifyPlaylist(name, tracks, base64Jpg);
                if (item != null)
                {                 
                    if (item.Images != null && item.Images.Count > 0)
                        image = new BitmapImage(new Uri(item.Images.FirstOrDefault().Url));
                    else if (img != null)
                        image = img;

                    if (item.Tracks != null) itemsCount = item.Tracks.Total;

                    return new Playlist(item.Id, item.Name, "",
                        item.Owner.DisplayName, item.Owner.Id, image, PlaylistCategoryType.MyPlaylist, item.Uri, itemsCount);
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// //Unfollows (Delete if user is owner) a list of playlists that current user follows.
        /// </summary>
        /// <param name="ids">
        /// The list of playlist ids the user wants to unfollow.
        /// </param>
        /// <returns>
        /// A list of ids of the playlists that were succesfuly unfollowed.
        /// </returns>
        public async Task<List<string>> UnfollowSpotifyPlaylist(IEnumerable<string> ids)
        {
            try
            {
                List<string> success = new List<string>();
                var spotify = await Authentication.GetSpotifyClientAsync();
                foreach (var id in ids)
                {
                    try
                    {
                        if (await spotify.Follow.UnfollowPlaylist(id))
                            success.Add(id);
                    }
                    catch (Exception)
                    {

                    }
                }
                return success;
            }
            catch (Exception)
            {
                ViewModels.Helpers.DisplayDialog("Error", "An error occured while creating your playlist");
                return null;
            }
        }
    }

}
