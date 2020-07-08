using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Toolkit.Uwp.UI;
using Spotify_search_helper.Data;
using Spotify_search_helper.Models;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Spotify_search_helper.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public static MainPageViewModel Current = null;
        private bool _useConnectedAnimation = true;

        public MainPageViewModel()
        {
            Current = this;
            Initialize();
            RegisterMessenger();
            LoadTheme();
        }

        private async void Initialize()
        {
            IsLoading = true;
            TracksMaxWidth = 700;

            var categories = Models.Category.GetCategoryItems();
            foreach (var item in categories)
                CategoryList.Add(item);

            LoadQuickAccess();

            var playlistCategories = PlaylistCategory.GetCategoryItems();
            foreach (var item in playlistCategories)
                PlaylistCategoryList.Add(item);

            SelectedPlaylistCategory = PlaylistCategoryList.FirstOrDefault();

            this.Profile = await DataSource.Current.GetProfile();

            // Set up the AdvancedCollectionView with live shaping enabled to filter and sort the original list
            var sourceItems = await DataSource.Current.GetPlaylists();
            if (sourceItems != null) _playlistCollectionCopy.AddRange(sourceItems);
            AdvancedCollectionView = new AdvancedCollectionView(sourceItems, true);

            CurrentSorting = PlaylistSortList.FirstOrDefault();

            SelectedPlaylistCollection.CollectionChanged += SelectedPlaylistCollection_CollectionChanged;

            //getting the rest of the playlists in the background 
            DataSource.Current.GetOther();
            UpdatePlaylistCategoryCount();

            //
        }

        private async void Refresh()
        {
            IsLoading = true;

            try
            {
                ResetTracksView();
                _playlistCollectionCopy.Clear();
                _filteredPlaylistCollection.Clear();
                SelectedPlaylistCollection.Clear();
                IsPopupActive = false;

                AdvancedCollectionView.Clear();
                SearchText = "";
                CurrentSorting = PlaylistSortList.FirstOrDefault();
                SelectedPlaylistCategory = PlaylistCategoryList.FirstOrDefault();
            }
            catch (Exception)
            {

            }

            var sourceItems = await DataSource.Current.GetPlaylists();
            if (sourceItems != null) _playlistCollectionCopy.AddRange(sourceItems);
            AdvancedCollectionView = new AdvancedCollectionView(sourceItems, true);

            //getting the rest of the playlists in the background 
            DataSource.Current.GetOther();
            UpdatePlaylistCategoryCount();

            IsLoading = false;
        }

        private void RegisterMessenger()
        {
            Messenger.Default.Register<DialogResult>(this, ManageDialogResult);
            Messenger.Default.Register<SizeChangedEventArgs>(this, HandleSizeChanged);
        }

        private void ManageDialogResult(DialogResult result)
        {
            if (result.ResultType == Windows.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                switch (result.Type)
                {
                    case DialogType.Merge:
                        MergeSelectedPlaylist();
                        break;
                    case DialogType.CreatePlaylist:
                        CreatePlaylistSelectedTracks();
                        break;
                    case DialogType.Unfollow:

                        UnfollowSelectedPlaylists();
                        break;
                }
            }
        }

        private void HandleSizeChanged(SizeChangedEventArgs e)
        {
            try
            {
                //padding for TracksListView 24 0 24 12
                if (e.NewSize.Width < TracksMaxWidth)
                    TracksViewWidth = e.NewSize.Width;
                else
                    TracksViewWidth = TracksMaxWidth;
            }
            catch (Exception)
            {

            }
        }

        #region Quick Access

        private async void LoadQuickAccess()
        {
            var items = await QuickAccessItem.Get();
            if (items != null)
            {
                var playlists = await DataSource.Current.GetPlaylists(items.Select(c => c.Id));
                if (playlists != null)
                    QuickAccessCollectionView = new AdvancedCollectionView(playlists, true);
                else
                    QuickAccessCollectionView = new AdvancedCollectionView();
            }
            else
                QuickAccessCollectionView = new AdvancedCollectionView();
        }

        private RelayCommand<Playlist> _quickAccessItemClickCommand;
        public RelayCommand<Playlist> QuickAccessItemClickCommand
        {
            get
            {
                if (_quickAccessItemClickCommand == null)
                {
                    _quickAccessItemClickCommand = new RelayCommand<Playlist>((item) =>
                    {
                        ShowQuickAccessView = false;
                        LoadPlaylistTracks(item);
                    });
                }
                return _quickAccessItemClickCommand;
            }
        }

        private RelayCommand _clearQuickAccessCommand;
        public RelayCommand ClearQuickAccessCommand
        {
            get
            {
                if (_clearQuickAccessCommand == null)
                {
                    _clearQuickAccessCommand = new RelayCommand(async() =>
                    {
                        await QuickAccessItem.Clear();
                    });
                }
                return _clearQuickAccessCommand;
            }
        }

        private RelayCommand<Playlist> _removeQuickAccessItemCommand;
        public RelayCommand<Playlist> RemoveQuickAccessItemCommand
        {
            get
            {
                if (_removeQuickAccessItemCommand == null)
                {
                    _removeQuickAccessItemCommand = new RelayCommand<Playlist>(async(item) =>
                    {
                        if (item != null && await QuickAccessItem.Remove(item.Id))
                        {
                            item.IsQuickAccessItem = false;
                            using (QuickAccessCollectionView.DeferRefresh())
                            {
                                QuickAccessCollectionView.Remove(item);
                            }
                        }
                    });
                }
                return _removeQuickAccessItemCommand;
            }
        }

        private RelayCommand<Playlist> _addQuickAccessItemCommand;
        public RelayCommand<Playlist> AddQuickAccessItemCommand
        {
            get
            {
                if (_addQuickAccessItemCommand == null)
                {
                    _addQuickAccessItemCommand = new RelayCommand<Playlist>(async (item) =>
                    {
                        if (item != null && await QuickAccessItem.Add(new QuickAccessItem(item.Id, item.Uri)))
                        {
                            item.IsQuickAccessItem = true;
                            using (QuickAccessCollectionView.DeferRefresh())
                            {
                                QuickAccessCollectionView.Add(item);
                            }
                        }
                    });
                }
                return _addQuickAccessItemCommand;
            }
        }

        private AdvancedCollectionView _quickAccessCollectionView;
        public AdvancedCollectionView QuickAccessCollectionView
        {
            get => _quickAccessCollectionView;
            set
            {
                _quickAccessCollectionView = value;
                RaisePropertyChanged("QuickAccessCollectionView");
            }
        }

        #endregion

        #region PlaylistView

        public async void UnfollowSelectedPlaylists()
        {
            IsLoading = true;

            bool showDialog = false;
            int failedCount = 0;
            var success = await DataSource.Current.UnfollowSpotifyPlaylist(SelectedPlaylistCollection.Select(c => c.Id));

            if (success == null || success.Count < SelectedPlaylistCollection.Count)
                showDialog = true;

            if (success == null)
                failedCount = SelectedPlaylistCollection.Count;
            else
                failedCount = SelectedPlaylistCollection.Count - success.Count;

            if (success != null)
            {
                foreach (var id in success)
                {
                    var pl1 = SelectedPlaylistCollection.Where(c => c.Id == id).FirstOrDefault();
                    if (pl1 != null) SelectedPlaylistCollection.Remove(pl1);
                    var pl2 = _playlistCollectionCopy.Where(c => c.Id == id).FirstOrDefault();
                    if(pl2 != null) _playlistCollectionCopy.Remove(pl2);
                    using (AdvancedCollectionView.DeferRefresh())
                    {
                        var pl3 = AdvancedCollectionView.Where(c => ((Playlist)c).Id == id).FirstOrDefault();
                        if (pl3 != null) AdvancedCollectionView.Remove(pl3);
                    }
                }
                UpdateAlphabet();
                UpdatePlaylistCategoryCount();
            }

            if (showDialog)
            {
                Helpers.DisplayDialog("Some playlist could not be followed", "Failed to unfollow " + failedCount + "playlists, please try again");
            }

            IsLoading = false;
        }

        public async void MergeSelectedPlaylist()
        {
            IsLoading = true;

            //display a dialog for the time being to get name
            if (!string.IsNullOrEmpty(NewPlaylistName) && SelectedPlaylistCollection.Count > 0)
            {
                var playlist = await DataSource.Current.MergeSpotifyPlaylists(NewPlaylistName, SelectedPlaylistCollection, _base64JpegData);
                if (playlist != null)
                {
                    Messenger.Default.Send(new DialogManager
                    {
                        Type = DialogType.Merge,
                        Action = DialogAction.Hide
                    });
                    ResetCreatePlaylistDialog();
                    var selected = SelectedPlaylistCollection.ToList();
                    foreach (var item in selected)
                    {
                        item.IsSelected = false;
                        SelectedPlaylistCollection.Remove(item);
                    }
                    AddToCollection(new List<Playlist> { playlist });
                    //show newly created playlist
                    LoadPlaylistTracks(playlist);
                }
            }

            IsLoading = false;
        }

        private async void PickImageFromFilePicker()
        {
            IsLoading = true;

            var file = await Helpers.ImageFileDialogPicker();
            if (file != null)
            {
                //load Base64JpegData
                MergeImageFilePath = file.Path;
                _base64JpegData = await Helpers.ImageToBase64(file);
            }

            IsLoading = false;
        }

        private void SortPlaylistCollection(Sorting sorting)
        {
            if (sorting.Type != Sorting.SortType.Name)
                IsAlphabetEnabled = false;
            else
                IsAlphabetEnabled = true;

            using (AdvancedCollectionView.DeferRefresh())
            {
                AdvancedCollectionView.SortDescriptions.Clear();
                switch (sorting.Type)
                {
                    case Sorting.SortType.Name:
                    case Sorting.SortType.Size:
                        AdvancedCollectionView.SortDescriptions.Add(new SortDescription(sorting.Property, sorting.SortDirection));
                        break;
                    case Sorting.SortType.Default:
                        break;
                    default:
                        AdvancedCollectionView.SortDescriptions.Add(new SortDescription("Title", SortDirection.Ascending));
                        break;
                }
            }

            Messenger.Default.Send(new MessengerHelper
            {
                Item = AdvancedCollectionView.FirstOrDefault(),
                Action = MessengerAction.ScrollToItem,
                Target = TargetView.Playlist
            });
        }

        public void AddToCollection(IEnumerable<Playlist> items)
        {
            _playlistCollectionCopy.AddRange(items);
            using (AdvancedCollectionView.DeferRefresh())
            {
                foreach (var item in items)
                {
                    AdvancedCollectionView.Add(item);
                }
            }
            UpdateAlphabet(items);
        }

        private void UpdateAlphabet(IEnumerable<Playlist> items = null, bool reset = false)
        {
            List<string> alphabet = new List<string>();
            if (Alphabet.Count > 0 && !reset)
                alphabet = Alphabet.ToList();
            else if (reset)
            {
                Alphabet.Clear();
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    if (!string.IsNullOrEmpty(item.Title) && alphabet.Where(c => c.ToUpper() == item.Title[0].ToString().ToUpper()).FirstOrDefault() == null)
                    {
                        //check if is number
                        if (int.TryParse(item.Title[0].ToString(), out int num))
                        {
                            if (alphabet.Where(c => c == "#").FirstOrDefault() == null)
                                alphabet.Add("#");
                        }
                        else
                            alphabet.Add(item.Title[0].ToString());
                    }
                }
            }

            var list = alphabet.OrderBy(c => c).ToList();
            Alphabet = new ObservableCollection<string>(list);
        }

        private bool _isPlaylistSearchActive;
        public bool IsPlaylistSearchActive
        {
            get => _isPlaylistSearchActive;
            set
            {
                _isPlaylistSearchActive = value;
                RaisePropertyChanged("IsPlaylistSearchActive");
            }
        }

        private string _playlistViewSubTitle;
        public string PlaylistViewSubTitle
        {
            get => _playlistViewSubTitle;
            set
            {
                _playlistViewSubTitle = value;
                RaisePropertyChanged("PlaylistViewSubTitle");
            }
        }

        private void UpdatePlaylistViewSubTitle()
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                IsPlaylistSearchActive = true;
                PlaylistViewSubTitle = "Filter results for `" + SearchText + "` (" + _filteredPlaylistCollection.Count + ")";
            }
            else
            {
                IsPlaylistSearchActive = false;
                PlaylistViewSubTitle = "";
            }
        }

        public void FilterPlaylistCollectionView(bool isSwitchingCategory = false)
        {
            // isSwitchingCategory allows us to use AdvancedCollectionView.DeferRefresh() 
            //  if only switching category because using it when searching makes the autocomplete lose focus everytime
            // the text changes

            if (AdvancedCollectionView == null)
                return;

            //make sure the filtered items are clear
            _filteredPlaylistCollection.Clear();


            if (!isSwitchingCategory)
            {
                if (!string.IsNullOrEmpty(SearchText))
                {
                    if (SelectedPlaylistCategory.CategoryType == PlaylistCategoryType.All)
                    {
                        AdvancedCollectionView.Filter = c => (((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Description).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Owner.Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);

                        _filteredPlaylistCollection.AddRange(_playlistCollectionCopy.Where(c => c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        c.Description.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        c.Owner.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)));
                    }
                    else
                    {
                        AdvancedCollectionView.Filter = c => ((((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Description).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Playlist)c).Owner.Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)) &&
                        ((Playlist)c).CategoryType == SelectedPlaylistCategory.CategoryType;

                        _filteredPlaylistCollection.AddRange(_playlistCollectionCopy.Where(c => (c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        c.Description.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        c.Owner.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))));
                    }
                }
                else
                {
                    if (SelectedPlaylistCategory.CategoryType == PlaylistCategoryType.All)
                    {
                        AdvancedCollectionView.Filter = c => c != null;
                        _filteredPlaylistCollection.AddRange(_playlistCollectionCopy);
                    }
                    else
                    {
                        AdvancedCollectionView.Filter = c => ((Playlist)c).CategoryType == SelectedPlaylistCategory.CategoryType;
                        _filteredPlaylistCollection.AddRange(_playlistCollectionCopy);
                    }
                }

                AdvancedCollectionView.RefreshFilter();
            }
            else
            {
                using(AdvancedCollectionView.DeferRefresh())
                {
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        if (SelectedPlaylistCategory.CategoryType == PlaylistCategoryType.All)
                        {
                            AdvancedCollectionView.Filter = c => (((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Description).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Owner.Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);

                            _filteredPlaylistCollection.AddRange(_playlistCollectionCopy.Where(c => c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            c.Description.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            c.Owner.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)));
                        }
                        else
                        {
                            AdvancedCollectionView.Filter = c => ((((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Description).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Owner.Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)) &&
                            ((Playlist)c).CategoryType == SelectedPlaylistCategory.CategoryType;

                            _filteredPlaylistCollection.AddRange(_playlistCollectionCopy.Where(c => (c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            c.Description.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            c.Owner.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase))));
                        }
                    }
                    else
                    {
                        if (SelectedPlaylistCategory.CategoryType == PlaylistCategoryType.All)
                        {
                            AdvancedCollectionView.Filter = c => c != null;
                            _filteredPlaylistCollection.AddRange(_playlistCollectionCopy);
                        }
                        else
                        {
                            AdvancedCollectionView.Filter = c => ((Playlist)c).CategoryType == SelectedPlaylistCategory.CategoryType;
                            _filteredPlaylistCollection.AddRange(_playlistCollectionCopy);
                        }
                    }
                }
            }


            UpdatePlaylistViewSubTitle();
            UpdateAlphabet(_filteredPlaylistCollection, true);
            UpdatePlaylistCategoryCount();
        }

        public void UpdatePlaylistCategoryCount()
        {
            if (!string.IsNullOrEmpty(SearchText))
            {
                foreach (var item in PlaylistCategoryList)
                {
                    if (item.CategoryType != PlaylistCategoryType.All)
                    {
                        item.Count = _filteredPlaylistCollection.Where(c => c.CategoryType == item.CategoryType).Count();
                        item.TracksCount = _filteredPlaylistCollection.Where(x => x.CategoryType == item.CategoryType).Sum(c => c.ItemsCount);
                    }
                    else
                    {
                        item.Count = _filteredPlaylistCollection.Count();
                        item.TracksCount = _filteredPlaylistCollection.Sum(c => c.ItemsCount);
                    }
                }
            }
            else
            {
                foreach (var item in PlaylistCategoryList)
                {
                    if (item.CategoryType != PlaylistCategoryType.All)
                    {
                        item.Count = _playlistCollectionCopy.Where(c => c.CategoryType == item.CategoryType).Count();
                        item.TracksCount = _playlistCollectionCopy.Where(x => x.CategoryType == item.CategoryType).Sum(c => c.ItemsCount);
                    }
                    else
                    {
                        item.Count = _playlistCollectionCopy.Count();
                        item.TracksCount = _playlistCollectionCopy.Sum(c => c.ItemsCount);
                    }
                }
            }

            foreach (var item in PlaylistCategoryList)
            {
                if (item.Count > 0)
                    item.HasResults = true;
                else
                    item.HasResults = false;
            }
        }

        private void ScrollToPlaylistAlphabet(string alphabet)
        {
            if (AdvancedCollectionView != null && AdvancedCollectionView.Source != null)
            {
                object item = null;
                if (alphabet == "#")
                {
                    item = _playlistCollectionCopy.Where(c => c.Title.StartsWith("0") ||
                    c.Title.StartsWith("1") ||
                    c.Title.StartsWith("2") ||
                    c.Title.StartsWith("3") ||
                    c.Title.StartsWith("4") ||
                    c.Title.StartsWith("5") ||
                    c.Title.StartsWith("6") ||
                    c.Title.StartsWith("7") ||
                    c.Title.StartsWith("8") ||
                    c.Title.StartsWith("9") ||
                    c.Title.StartsWith("_") ||
                    c.Title.StartsWith(".") ||
                    c.Title.StartsWith("&") ||
                    c.Title.StartsWith("$") ||
                    c.Title.StartsWith("#") ||
                    c.Title.StartsWith("@")).FirstOrDefault();
                }
                else
                {
                    item = AdvancedCollectionView.Where(c => ((Playlist)c).Title.ToLower().StartsWith(alphabet.ToLower())).FirstOrDefault();
                }
                Messenger.Default.Send(new MessengerHelper
                {
                    Item = item,
                    Action = MessengerAction.ScrollToItem,
                    Target = TargetView.Alphabet
                });
            }
        }

        private void SwitchCategory(Models.Category category)
        {
            if (category != null)
            {
                //change
                IsCompactCategory = true;

                ActiveCategory = category;
                PageTitle = category.Title;

                switch (category.Type)
                {
                    case CategoryType.Playlist:
                        IsPlaylistView = true;
                        break;
                    case CategoryType.Liked:
                        IsPlaylistView = false;
                        break;
                    case CategoryType.MadeForYou:
                        IsPlaylistView = false;
                        break;
                    case CategoryType.Convert:
                        IsPlaylistView = false;
                        break;
                }
            }
        }

        private void AddToSelected(IEnumerable<Playlist> items)
        {
            IsLoading = true;

            foreach (var item in items)
            {
                if (SelectedPlaylistCollection.Where(c => c.Id == item.Id).FirstOrDefault() == null)
                    SelectedPlaylistCollection.Add(item);
            }

            IsLoading = false;
        }

        private void RemoveFromSelected(IEnumerable<Playlist> items)
        {
            IsLoading = true;

            foreach (var item in items)
            {
                if (SelectedPlaylistCollection.Where(c => c.Id == item.Id).FirstOrDefault() != null)
                    SelectedPlaylistCollection.Remove(item);
            }

            IsLoading = false;
        }

        private async void PlayItem(Playlist item)
        {
            IsLoading = true;
            await DataSource.Current.PlaySpotifyItems(new List<Playlist> { item });
            IsLoading = false;
        }

        private async void PlayItems(IEnumerable<Playlist> items, bool shuffle = false)
        {
            IsLoading = true;

            if (items != null && items.Count() > 0)
                await DataSource.Current.PlaySpotifyItems(items.ToList(), shuffle);

            IsLoading = false;
        }

        private RelayCommand _mergeImagePickerCommand;
        public RelayCommand MergeImagePickerCommand
        {
            get
            {
                if (_mergeImagePickerCommand == null)
                {
                    _mergeImagePickerCommand = new RelayCommand(() =>
                    {
                        PickImageFromFilePicker();
                    });
                }
                return _mergeImagePickerCommand;
            }
        }

        private RelayCommand _mergeSelectedPlaylistCommand;
        public RelayCommand MergeSelectedPlaylistCommand
        {
            get
            {
                if (_mergeSelectedPlaylistCommand == null)
                {
                    _mergeSelectedPlaylistCommand = new RelayCommand(() =>
                    {
                        MergeSelectedPlaylist();
                    });
                }
                return _mergeSelectedPlaylistCommand;
            }
        }

        private RelayCommand _showMergeDialogCommand;
        public RelayCommand ShowMergeDialogCommand
        {
            get
            {
                if (_showMergeDialogCommand == null)
                {
                    _showMergeDialogCommand = new RelayCommand(() =>
                    {
                        HandleCreatePlaylistMode(CreatePlaylistMode.Merge);
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.CreatePlaylist,
                            Action = DialogAction.Show,
                        });
                    });
                }
                return _showMergeDialogCommand;
            }
        }

        private RelayCommand _cancelMergeCommand;
        public RelayCommand CancelMergeCommand
        {
            get
            {
                if (_cancelMergeCommand == null)
                {
                    _cancelMergeCommand = new RelayCommand(() =>
                    {
                        ResetCreatePlaylistDialog();
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.Merge,
                            Action = DialogAction.Hide
                        });
                    });
                }
                return _cancelMergeCommand;
            }
        }

        private RelayCommand<Playlist> _playlistItemClickCommand;
        public RelayCommand<Playlist> PlaylistItemClickCommand
        {
            get
            {
                if (_playlistItemClickCommand == null)
                {
                    _playlistItemClickCommand = new RelayCommand<Playlist>((item) =>
                    {
                        if (ShowQuickAccessView) _useConnectedAnimation = false;
                        LoadPlaylistTracks(item);
                    });
                }
                return _playlistItemClickCommand;
            }
        }

        private RelayCommand _shuffleSelectedPlaylistsCommand;
        public RelayCommand ShuffleSelectedPlaylistsCommand
        {
            get
            {
                if (_shuffleSelectedPlaylistsCommand == null)
                {
                    _shuffleSelectedPlaylistsCommand = new RelayCommand(() =>
                    {
                        PlayItems(SelectedPlaylistCollection);
                    });
                }
                return _shuffleSelectedPlaylistsCommand;
            }
        }

        private RelayCommand _unfollowSelectedPlaylistsCommand;
        public RelayCommand UnfollowSelectedPlaylistsCommand
        {
            get
            {
                if (_unfollowSelectedPlaylistsCommand == null)
                {
                    _unfollowSelectedPlaylistsCommand = new RelayCommand(() =>
                    {
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.Unfollow,
                            Action = DialogAction.Show,
                            Title = "Unfollow playlists?",
                            Message = "Are you sure you want to unfollow the selected playlists?",
                            PrimaryButtonText = "Unfollow",
                            SecondaryButtonText = "Cancel"
                        });
                    });
                }
                return _unfollowSelectedPlaylistsCommand;
            }
        }

        private RelayCommand<string> _toggleExpandedPlaylistSelectedCommand;
        public RelayCommand<string> ToggleExpandedPlaylistSelectedCommand
        {

            get
            {
                if (_toggleExpandedPlaylistSelectedCommand == null)
                {
                    _toggleExpandedPlaylistSelectedCommand = new RelayCommand<string>((show) =>
                    {
                        if (show.ToLower().Equals("true"))
                        {
                            IsPopupActive = true;
                            IsSelectedPlaylistExpanded = true;
                        }
                        else
                        {
                            IsPopupActive = false;
                            IsSelectedPlaylistExpanded = false;
                        }
                    });
                }
                return _toggleExpandedPlaylistSelectedCommand;
            }
        }

        private RelayCommand<Playlist> _addPlaylistToSelectedCommand;
        public RelayCommand<Playlist> AddPlaylistToSelectedCommand
        {
            get
            {
                if (_addPlaylistToSelectedCommand == null)
                {
                    _addPlaylistToSelectedCommand = new RelayCommand<Playlist>((item) =>
                    {
                        if (SelectedPlaylistCollection.Where(c => c.Id == item.Id).FirstOrDefault() == null)
                            SelectedPlaylistCollection.Add(item);
                    });
                }
                return _addPlaylistToSelectedCommand;
            }
        }

        private RelayCommand<Playlist> _removePlaylistToSelectedCommand;
        public RelayCommand<Playlist> RemovePlaylistToSelectedCommand
        {
            get
            {
                if (_removePlaylistToSelectedCommand == null)
                {
                    _removePlaylistToSelectedCommand = new RelayCommand<Playlist>((item) =>
                    {
                        if (SelectedPlaylistCollection.Where(c => c.Id == item.Id).FirstOrDefault() != null)
                            SelectedPlaylistCollection.Remove(item);
                    });
                }
                return _removePlaylistToSelectedCommand;
            }
        }

        private RelayCommand<Playlist> _clearPlaylistToSelectedCommand;
        public RelayCommand<Playlist> ClearPlaylistToSelectedCommand
        {
            get
            {
                if (_clearPlaylistToSelectedCommand == null)
                {
                    _clearPlaylistToSelectedCommand = new RelayCommand<Playlist>((item) =>
                    {
                        var list = SelectedPlaylistCollection.ToList();
                        foreach (var pl in list)
                        {
                            SelectedPlaylistCollection.Remove(pl);
                        }
                    });
                }
                return _clearPlaylistToSelectedCommand;
            }
        }

        private string _base64JpegData;

        private string _newPlaylistName;
        public string NewPlaylistName
        {
            get => _newPlaylistName;
            set
            {
                _newPlaylistName = value;
                RaisePropertyChanged("NewPlaylistName");
            }
        }

        private string _mergeImageFilePath;
        public string MergeImageFilePath
        {
            get => _mergeImageFilePath;
            set
            {
                _mergeImageFilePath = value;
                RaisePropertyChanged("MergeImageFilePath");
            }
        }

        private bool _isAlphabetEnabled;
        public bool IsAlphabetEnabled
        {
            get => _isAlphabetEnabled;
            set
            {
                _isAlphabetEnabled = value;
                RaisePropertyChanged("IsAlphabetEnabled");
            }
        }

        private bool _isCreatePlaylistMergeMode;
        public bool IsCreatePlaylistMergeMode
        {
            get => _isCreatePlaylistMergeMode;
            set
            {
                _isCreatePlaylistMergeMode = value;
                RaisePropertyChanged("IsCreatePlaylistMergeMode");
            }
        }

        private readonly List<Playlist> _filteredPlaylistCollection = new List<Playlist>();
        public ObservableCollection<Sorting> PlaylistSortList { get; } = new ObservableCollection<Sorting>(Sorting._playlistSortList);
        readonly List<Playlist> _playlistCollectionCopy = new List<Playlist>();

        ObservableCollection<Models.Category> _categoryList = new ObservableCollection<Models.Category>();
        public ObservableCollection<Models.Category> CategoryList
        {
            get => _categoryList;
            set { _categoryList = value; RaisePropertyChanged("CategoryList"); }
        }

        ObservableCollection<PlaylistCategory> _playlistCategoryList = new ObservableCollection<PlaylistCategory>();
        public ObservableCollection<PlaylistCategory> PlaylistCategoryList
        {
            get => _playlistCategoryList;
            set { _playlistCategoryList = value; RaisePropertyChanged("PlaylistCategoryList"); }
        }

        ObservableCollection<Playlist> _selectedPlaylistCollection = new ObservableCollection<Playlist>();
        public ObservableCollection<Playlist> SelectedPlaylistCollection
        {
            get => _selectedPlaylistCollection;
            set
            {
                _selectedPlaylistCollection = value;
                RaisePropertyChanged("SelectedPlaylistCollection");
            }
        }

        private ObservableCollection<string> _alphabet = new ObservableCollection<string>();
        public ObservableCollection<string> Alphabet
        {
            get => _alphabet;
            set
            {
                _alphabet = value;
                RaisePropertyChanged("Alphabet");
            }
        }

        private AdvancedCollectionView _advancedCollectionView;
        public AdvancedCollectionView AdvancedCollectionView
        {
            get => _advancedCollectionView;
            set
            {
                _advancedCollectionView = value;
                RaisePropertyChanged("AdvancedCollectionView");
            }
        }

        private void SelectedPlaylistCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (SelectedPlaylistCollection.Count > 0)
                HasSelectedItems = true;
            else
            {
                HasSelectedItems = false;
                IsSelectAllChecked = false;

                if (IsSelectedPlaylistExpanded)
                {
                    IsSelectedPlaylistExpanded = false;
                    IsPopupActive = false;
                }

            }

            //update selected state
            int totalTracks = 0;

            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is Playlist playlist)
                    {
                        playlist.IsSelected = true;

                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is Playlist playlist)
                        playlist.IsSelected = false;
                }
            }

            foreach (var item in SelectedPlaylistCollection)
            {
                totalTracks += item.ItemsCount;
            }

            SelectedPlaylistsTracksCount = totalTracks;
        }

        #endregion

        #region TracksView

        private async void RemoveTracksFromCurrentPlaylist(IEnumerable<Track> items)
        {
            IsLoading = true;

            if (CurrentPlaylist != null)
            {
                try
                {
                    if (await DataSource.Current.RemoveItemsFromPlaylist(CurrentPlaylist.Id, new List<string>(items.Select(c => c.Uri))))
                    {
                        var files = items.ToList();
                        using (TracksCollectionView.DeferRefresh())
                        {
                            foreach (var item in files)
                            {
                                try
                                {
                                    TracksCollectionView.Remove(item);
                                    _tracksCollectionCopy.Remove(item);
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }

            IsLoading = false;
        }

        private async void PlayCurrentPlaylistTracks(Track item)
        {
            IsLoading = true;
            if (CurrentPlaylist != null)
            {
                int index;
                if (CurrentTrackSorting != null && _filteredTracksCollection.Count != 0 || !string.IsNullOrEmpty(TrackSearchText))
                {
                    if (item != null)
                        index = _filteredTracksCollection.IndexOf(item);
                    else
                        index = -1;

                    if (index < 0) index = 0;
                    await DataSource.Current.PlaySpotifyTracks(_filteredTracksCollection, index);
                }
                else
                {
                    index = _tracksCollectionCopy.IndexOf(item);
                    if (index < 0) index = 0;
                    await DataSource.Current.PlaySpotifyTracks(_tracksCollectionCopy, index);
                }



            }
            IsLoading = false;
        }

        private async void PlayTracks(IEnumerable<Track> items, int index = 0)
        {
            IsLoading = true;

            await DataSource.Current.PlaySpotifyTracks(items.ToList(), index);

            IsLoading = false;
        }

        private void ResetTracksView()
        {
            try
            {
                if (TracksCollectionView != null) TracksCollectionView.Clear();
                TrackSearchText = "";
                CurrentTrackSorting = TracksSortList.FirstOrDefault();
                _tracksCollectionCopy.Clear();
                TracksViewHasSelectedItems = false;
                UpdateTracksViewSubTitle();
                UpdateTracksViewSelectedItems();
                IsSelectAllTracksChecked = false;
            }
            catch (Exception)
            {

            }
        }

        private void SortTrackCollection(Sorting sorting)
        {
            if (TracksCollectionView == null)
                return;

            try
            {
                using (TracksCollectionView.DeferRefresh())
                {
                    TracksCollectionView.SortDescriptions.Clear();

                    switch (sorting.Type)
                    {
                        case Sorting.SortType.Name:
                        case Sorting.SortType.Artist:
                        case Sorting.SortType.Album:
                            TracksCollectionView.SortDescriptions.Add(new SortDescription(sorting.Property, sorting.SortDirection));
                            break;
                        case Sorting.SortType.Default:
                            break;
                        default:
                            TracksCollectionView.SortDescriptions.Add(new SortDescription("Title", SortDirection.Ascending));
                            break;
                    }
                }

                if (TracksCollectionView.FirstOrDefault() != null)
                {
                    Messenger.Default.Send(new MessengerHelper
                    {
                        Item = TracksCollectionView.FirstOrDefault(),
                        Action = MessengerAction.ScrollToItem,
                        Target = TargetView.Tracks
                    });
                }
            }
            catch (Exception)
            {
                //
            }
        }

        public void FilterTracksCollectionView()
        {
            if (TracksCollectionView == null)
                return;

            if (!string.IsNullOrEmpty(TrackSearchText))
                IsTracksViewFilterActive = true;
            else
                IsTracksViewFilterActive = false;

            //make sure the filtered items are clear
            string searchStr = TrackSearchText;
            _filteredTracksCollection.Clear();
            using (TracksCollectionView.DeferRefresh())
            {
                if (!string.IsNullOrEmpty(searchStr))
                {
                    TracksCollectionView.Filter = c => (((Track)c).Title).Contains(TrackSearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Track)c).Album).Contains(TrackSearchText, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Track)c).Artist).Contains(TrackSearchText, StringComparison.CurrentCultureIgnoreCase); //add artist search
                    _filteredTracksCollection.AddRange(_tracksCollectionCopy.Where(c => c.Title.Contains(TrackSearchText, StringComparison.CurrentCultureIgnoreCase) ||
                    c.Album.Contains(TrackSearchText, StringComparison.CurrentCultureIgnoreCase) ||
                    c.Artist.Contains(TrackSearchText, StringComparison.CurrentCultureIgnoreCase)));
                }
                else
                {
                    TracksCollectionView.Filter = c => c != null; //bit of a hack to clear filters
                    _filteredTracksCollection.AddRange(_tracksCollectionCopy);
                }
            }

            UpdateTracksViewSubTitle();
        }

        private void UpdateTracksViewSubTitle()
        {
            if (!string.IsNullOrEmpty(TrackSearchText))
            {
                TracksViewSubTitle = "Filter results for `" + TrackSearchText + "` (" + _filteredTracksCollection.Count + ")";
            }
            else if(TracksCollectionView != null)
            {
                TracksViewSubTitle = "Tracks (" + TracksCollectionView.Count + ")";
            }
        }

        private void UpdateTracksViewSelectedItems()
        {
            if (_tracksCollectionCopy != null)
            {
                var count = _tracksCollectionCopy.Where(c => c.IsSelected).Count();
                TracksViewHasSelectedItems = (count > 0);
                TracksViewSelectedItemsCount = (count > 0) ? count : 0;
            }
        }

        private async void LoadPlaylistTracks(Playlist item)
        {
            IsLoading = true;

           if(ShowQuickAccessView) ShowQuickAccessView = false;
            CurrentTrackSorting = TracksSortList.FirstOrDefault();
            TracksViewHasSelectedItems = false;

            if (item != null)
            {
                if (TracksCollectionView == null)
                    TracksCollectionView = new AdvancedCollectionView();
                else
                    TracksCollectionView.Clear();

                //get first set of results to have something to show the user quickly
                using (TracksCollectionView.DeferRefresh())
                {
                    CurrentPlaylist = item;
                    IsPopupActive = true;
                    IsTracksView = true;

                    if (_useConnectedAnimation)
                        Views.MainPage.Current.DoIt(item);

                    var items = await DataSource.Current.GetSpotifyTracks(item.Id);
                    if (items != null)
                    {
                        _tracksCollectionCopy.Clear();
                        _tracksCollectionCopy.AddRange(items);

                        foreach (var track in items)
                        {
                            if (item.CategoryType == PlaylistCategoryType.MyPlaylist)
                                track.CanModify = true;
                            else
                                track.CanModify = false;

                            TracksCollectionView.Add(track);
                        }
                    }
                }

                UpdateTracksViewSubTitle();

                //get the result of the items
                if (_tracksCollectionCopy != null && _tracksCollectionCopy.Count < item.ItemsCount)
                {
                    using (TracksCollectionView.DeferRefresh())
                    {
                        var items = await DataSource.Current.GetOtherSpotifyTracks();
                        if (items != null)
                        {
                            foreach (var track in items)
                            {
                                if (!_tracksCollectionCopy.Where(c => c.Id == track.Id).Any())
                                    _tracksCollectionCopy.Add(track);

                                if (!TracksCollectionView.Where(c => (((Track)c)).Id == track.Id).Any())
                                    TracksCollectionView.Add(track);
                            }
                        }
                    }
                }
            }

            //update duration
            item.DurationStr = Helpers.MillisecondsToString(_tracksCollectionCopy.Sum(c => c.DurationMs));

            IsLoading = false;
        }

        private async void CreatePlaylistSelectedTracks()
        {
            IsLoading = true;

            var selected = _tracksCollectionCopy.Where(c => c.IsSelected);
            if (selected != null && selected.Count() > 0)
            {
                var fullPlaylist = await DataSource.Current.CreateSpotifyPlaylist(NewPlaylistName, selected, _base64JpegData);
                if (fullPlaylist != null)
                {
                    var playlist = await DataSource.Current.ConvertPlaylists(new List<FullPlaylist> { fullPlaylist });
                    Messenger.Default.Send(new DialogManager
                    {
                        Type = DialogType.CreatePlaylist,
                        Action = DialogAction.Hide
                    });
                    ResetTracksView();
                    ResetCreatePlaylistDialog();

                    if (playlist != null)
                    {
                        LoadPlaylistTracks(playlist.FirstOrDefault()); ;
                    }
                }
            }

            IsLoading = false;
        }

        private string _addToPlaylistSearchText;
        public string AddToPlaylistSearchText
        {
            get { return _addToPlaylistSearchText; }
            set
            {
                _addToPlaylistSearchText = value;
                RaisePropertyChanged("AddToPlaylistSearchText");
                FilterAddToPlaylistCollectionView();

                //if searchtext is cleared, show all items
            }
        }

        private void LoadAddToListView()
        {
            IsLoading = true;
            AddToPlaylistCollectionView = new AdvancedCollectionView(_playlistCollectionCopy, true);
            IsLoading = false;
        }

        private async void AddSelectedTracksToPlaylist(string playlistId)
        {
            IsLoading = true;

            try
            {
                
                var tracks = _tracksCollectionCopy.Where(c => c.IsSelected);
                await DataSource.Current.AddToSpotifyPlaylist(playlistId, tracks.Select(c => c.Uri));

                //update playlist in collection view
                try
                {
                    var added = AdvancedCollectionView.Where(c => ((Playlist)c).Id == playlistId);
                    var removed = AdvancedCollectionView.Where(c => ((Playlist)c).Id == playlistId);

                    if (added != null && added is Playlist playlistAdded) playlistAdded.ItemsCount += tracks.Count();
                    if (removed != null && removed is Playlist playlistRemoved) playlistRemoved.ItemsCount -= tracks.Count();
                }
                catch (Exception)
                {

                }

                //show playlist?
                HandleCreatePlaylistMode(CreatePlaylistMode.Tracks);
                Messenger.Default.Send(new DialogManager
                {
                    Type = DialogType.AddToPlaylist,
                    Action = DialogAction.Hide,
                });

                //reset
                AddToPlaylistSearchText = null;
            }
            catch (Exception)
            {

                throw;
            }

            IsLoading = false;
        }

        public void FilterAddToPlaylistCollectionView()
        {
            //make sure the filtered items are clear
            using (AddToPlaylistCollectionView.DeferRefresh())
            {
                if (!string.IsNullOrEmpty(AddToPlaylistSearchText))
                {
                    AddToPlaylistCollectionView.Filter = c => (((Playlist)c).Title).Contains(AddToPlaylistSearchText, StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    AddToPlaylistCollectionView.Filter = c => c != null; //bit of a hack to clear filters
                }
            }

            UpdateTracksViewSubTitle();
        }

        private RelayCommand<Playlist> _addToPlaylistCommand;
        public RelayCommand<Playlist> AddToPlaylistCommand
        {
            get
            {
                if (_addToPlaylistCommand == null)
                {
                    _addToPlaylistCommand = new RelayCommand<Playlist>((item) =>
                    {
                        AddSelectedTracksToPlaylist(item.Id);
                    });
                }
                return _addToPlaylistCommand;
            }
        }

        private RelayCommand _showAddToPlaylistDialogCommand;
        public RelayCommand ShowAddToPlaylistDialogCommand
        {
            get
            {
                if (_showAddToPlaylistDialogCommand == null)
                {
                    _showAddToPlaylistDialogCommand = new RelayCommand(() =>
                    {
                        HandleCreatePlaylistMode(CreatePlaylistMode.Tracks);
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.AddToPlaylist,
                            Action = DialogAction.Show,
                        });
                        LoadAddToListView();
                    });
                }
                return _showAddToPlaylistDialogCommand;
            }
        }

        private RelayCommand _closeAddToPlaylistDialogCommand;
        public RelayCommand CloseAddToPlaylistDialogCommand
        {
            get
            {
                if (_closeAddToPlaylistDialogCommand == null)
                {
                    _closeAddToPlaylistDialogCommand = new RelayCommand(() =>
                    {
                        HandleCreatePlaylistMode(CreatePlaylistMode.Tracks);
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.AddToPlaylist,
                            Action = DialogAction.Hide,
                        });
                    });
                }
                return _closeAddToPlaylistDialogCommand;
            }
        }

        private RelayCommand<Track> _tracksViewItemSelectionToggleCommand;
        public RelayCommand<Track> TracksViewItemSelectionToggleCommand
        {
            get
            {
                if (_tracksViewItemSelectionToggleCommand == null)
                {
                    _tracksViewItemSelectionToggleCommand = new RelayCommand<Track>((item) =>
                    {
                        if (item != null) item.IsSelected = !item.IsSelected;
                        UpdateTracksViewSelectedItems();
                    });
                }
                return _tracksViewItemSelectionToggleCommand;
            }
        }

        private RelayCommand<Track> _tracksViewItemClickCommand;
        public RelayCommand<Track> TracksViewItemClickCommand
        {
            get
            {
                if (_tracksViewItemClickCommand == null)
                {
                    _tracksViewItemClickCommand = new RelayCommand<Track>((item) =>
                    {
                        if (item != null) item.IsSelected = !item.IsSelected;
                        UpdateTracksViewSelectedItems();
                    });
                }
                return _tracksViewItemClickCommand;
            }
        }

        private RelayCommand _playSelectedTracksCommand;
        public RelayCommand PlaySelectedTracksCommand
        {
            get
            {
                if (_playSelectedTracksCommand == null)
                {
                    _playSelectedTracksCommand = new RelayCommand(() =>
                    {
                        if (_tracksCollectionCopy.Where(c => c.IsSelected).Count() > 0)
                            PlayTracks(_tracksCollectionCopy.Where(c => c.IsSelected));
                        else
                            Helpers.DisplayDialog("No items selected", "Please select some items first.");
                    });
                }
                return _playSelectedTracksCommand;
            }
        }

        private RelayCommand<Track> _removeTrackFromCurrentPlaylistCommand;
        public RelayCommand<Track> RemoveTrackFromCurrentPlaylistCommand
        {
            get
            {
                if (_removeTrackFromCurrentPlaylistCommand == null)
                {
                    _removeTrackFromCurrentPlaylistCommand = new RelayCommand<Track>((item) =>
                    {
                        RemoveTracksFromCurrentPlaylist(new List<Track> { item });
                        UpdateTracksViewSelectedItems();
                        UpdateTracksViewSubTitle();
                    });
                }
                return _removeTrackFromCurrentPlaylistCommand;
            }
        }

        private RelayCommand _removeSelectedFromCurrentPlaylistCommand;
        public RelayCommand RemoveSelectedFromCurrentPlaylistCommand
        {
            get
            {
                if (_removeSelectedFromCurrentPlaylistCommand == null)
                {
                    _removeSelectedFromCurrentPlaylistCommand = new RelayCommand(() =>
                    {
                        if (_tracksCollectionCopy.Where(c => c.IsSelected).Count() > 0)
                        {
                            RemoveTracksFromCurrentPlaylist(_tracksCollectionCopy.Where(c => c.IsSelected));
                            UpdateTracksViewSelectedItems();
                            UpdateTracksViewSubTitle();
                        }
                        else
                            Helpers.DisplayDialog("No items selected", "Please select some items first.");
                    });
                }
                return _removeSelectedFromCurrentPlaylistCommand;
            }
        }

        private RelayCommand _clearSelectedTracksCommand;
        public RelayCommand ClearSelectedTracksCommand
        {
            get
            {
                if (_clearSelectedTracksCommand == null)
                {
                    _clearSelectedTracksCommand = new RelayCommand(() =>
                    {
                        if (_tracksCollectionCopy.Where(c => c.IsSelected).Count() > 0)
                        {
                            IsSelectAllTracksChecked = false;
                            UpdateTracksViewSelectedItems();
                            UpdateTracksViewSubTitle();
                        }                  
                    });
                }
                return _clearSelectedTracksCommand;
            }
        }

        private RelayCommand _createPlaylistFromSelectedTracksCommand;
        public RelayCommand CreatePlaylistFromSelectedTracksCommand
        {
            get
            {
                if (_createPlaylistFromSelectedTracksCommand == null)
                {
                    _createPlaylistFromSelectedTracksCommand = new RelayCommand(() =>
                    {
                        CreatePlaylistSelectedTracks();
                    });
                }
                return _createPlaylistFromSelectedTracksCommand;
            }
        }

        private RelayCommand _showCreatePlaylistTracksDialogCommand;
        public RelayCommand ShowCreatePlaylistTracksDialogCommand
        {
            get
            {
                if (_showCreatePlaylistTracksDialogCommand == null)
                {
                    _showCreatePlaylistTracksDialogCommand = new RelayCommand(() =>
                    {
                        HandleCreatePlaylistMode(CreatePlaylistMode.Tracks);
                        Messenger.Default.Send(new DialogManager
                        {
                            Type = DialogType.CreatePlaylist,
                            Action = DialogAction.Show,
                        });
                    });
                }
                return _showCreatePlaylistTracksDialogCommand;
            }
        }

        private bool _isCreatePlaylistTracksMode;
        public bool IsCreatePlaylistTracksMode
        {
            get => _isCreatePlaylistTracksMode;
            set
            {
                _isCreatePlaylistTracksMode = value;
                RaisePropertyChanged("IsCreatePlaylistTracksMode");
            }
        }

        private bool _tracksViewHasSelectedItems;
        public bool TracksViewHasSelectedItems
        {
            get => _tracksViewHasSelectedItems;
            set
            {
                _tracksViewHasSelectedItems = value;
                RaisePropertyChanged("TracksViewHasSelectedItems");
            }
        }

        private int _tracksViewSelectedItemsCount;
        public int TracksViewSelectedItemsCount
        {
            get => _tracksViewSelectedItemsCount;
            set
            {
                _tracksViewSelectedItemsCount = value;
                RaisePropertyChanged("TracksViewSelectedItemsCount");
            }
        }

        private bool _isTracksViewFilterActive;
        public bool IsTracksViewFilterActive
        {
            get => _isTracksViewFilterActive;
            set
            {
                _isTracksViewFilterActive = value;
                RaisePropertyChanged("IsTracksViewFilterActive");
            }
        }

        private string _tracksViewSubTitle = "Tracks (0)";
        public string TracksViewSubTitle
        {
            get => _tracksViewSubTitle;
            set
            {
                _tracksViewSubTitle = value;
                RaisePropertyChanged("TracksViewSubTitle");
            }
        }

        private Sorting _currentTrackSorting;
        public Sorting CurrentTrackSorting
        {
            get => _currentTrackSorting;
            set
            {
                _currentTrackSorting = value;
                RaisePropertyChanged("CurrentTrackSorting");
                if (value != null) SortTrackCollection(value);
            }
        }

        private string _trackSearchText;
        public string TrackSearchText
        {
            get { return _trackSearchText; }
            set
            {
                _trackSearchText = value;
                RaisePropertyChanged("TrackSearchText");
                FilterTracksCollectionView();

                //if searchtext is cleared, show all items
            }
        }

        public ObservableCollection<Sorting> TracksSortList { get; } = new ObservableCollection<Sorting>(Sorting._tracksSortList);
        readonly List<Track> _tracksCollectionCopy = new List<Track>();
        readonly List<Track> _filteredTracksCollection = new List<Track>();

        private ObservableCollection<Track> _selectedTracks = new ObservableCollection<Track>();
        public ObservableCollection<Track> SelectedTracks
        {
            get => _selectedTracks;
            set
            {
                _selectedTracks = value;
                RaisePropertyChanged("SelectedTracks");
            }
        }

        private AdvancedCollectionView _tracksCollectionView;
        public AdvancedCollectionView TracksCollectionView
        {
            get => _tracksCollectionView;
            set
            {
                _tracksCollectionView = value;
                RaisePropertyChanged("TracksCollectionView");
            }
        }

        private AdvancedCollectionView _addToPlaylistCollectionView;
        public AdvancedCollectionView AddToPlaylistCollectionView
        {
            get => _addToPlaylistCollectionView;
            set
            {
                _addToPlaylistCollectionView = value;
                RaisePropertyChanged("AddToPlaylistCollectionView");
            }
        }

        #endregion

        #region Properties

        private double _tracksMaxWidth = 700;
        public double TracksMaxWidth
        {
            get => _tracksMaxWidth;
            set
            {
                _tracksMaxWidth = value;
                RaisePropertyChanged("TracksMaxWidth");
            }
        }

        private double _tracksViewWidth;
        public double TracksViewWidth
        {
            get => _tracksViewWidth;
            set
            {
                _tracksViewWidth = value;
                RaisePropertyChanged("TracksViewWidth");
            }
        }

        private bool _showQuickAccessView;
        public bool ShowQuickAccessView
        {
            get => _showQuickAccessView;
            set
            {
                _showQuickAccessView = value;
                RaisePropertyChanged("ShowQuickAccessView");
            }
        }

        private Uri _mediaElementSource;
        public Uri MediaElementSource
        {
            get { return _mediaElementSource; }
            set
            {
                _mediaElementSource = value;
                RaisePropertyChanged("MediaElementSource");
            }
        }

        private string _createPlaylistTitle = "Create playlist";
        public string CreatePlaylistTitle
        {
            get { return _createPlaylistTitle; }
            set
            {
                _createPlaylistTitle = value;
                RaisePropertyChanged("CreatePlaylistTitle");
            }
        }

        private Playlist _currentPlaylist;
        public Playlist CurrentPlaylist
        {
            get => _currentPlaylist;
            set
            {
                _currentPlaylist = value;
                RaisePropertyChanged("CurrentPlaylist");
            }
        }

        private Sorting _currentSorting;
        public Sorting CurrentSorting
        {
            get => _currentSorting;
            set
            {
                _currentSorting = value;
                RaisePropertyChanged("CurrentSorting");
                if (value != null) SortPlaylistCollection(value);
            }
        }

        private int _selectedPlaylistsTracksCount;
        public int SelectedPlaylistsTracksCount
        {
            get => _selectedPlaylistsTracksCount;
            set
            {
                _selectedPlaylistsTracksCount = value;
                RaisePropertyChanged("SelectedPlaylistsTracksCount");
            }
        }

        private bool _isDarkThemeEnabled = true;
        public bool IsDarkThemeEnabled
        {
            get => _isDarkThemeEnabled;
            set
            {
                _isDarkThemeEnabled = value;
                RaisePropertyChanged("IsDarkThemeEnabled");
                ToggleTheme();
            }
        }

        private string _selectedPlaylistAlphabet;
        public string SelectedPlaylistAlphabet
        {
            get => _selectedPlaylistAlphabet;
            set
            {
                _selectedPlaylistAlphabet = value;
                RaisePropertyChanged("SelectedPlaylistAlphabet");
                if (!string.IsNullOrEmpty(value)) ScrollToPlaylistAlphabet(value);
            }
        }

        private double _expandedSelectPlaylistWidth = 352;
        public double ExpandedSelectPlaylistWidth
        {
            get => _expandedSelectPlaylistWidth;
            set
            {
                _expandedSelectPlaylistWidth = value;
                RaisePropertyChanged("ExpandedSelectPlaylistWidth");
            }
        }

        private bool _isPopupActive;
        public bool IsPopupActive
        {
            get => _isPopupActive;
            set
            {
                if (!value && IsSelectedPlaylistExpanded) IsSelectedPlaylistExpanded = false;
                if (!value && IsTracksView) IsTracksView = false;
                _isPopupActive = value;
                RaisePropertyChanged("IsPopupActive");
            }
        }

        private bool _isTracksView;
        public bool IsTracksView
        {
            get => _isTracksView;
            set
            {
                if (IsSelectedPlaylistExpanded) IsSelectedPlaylistExpanded = false;
                _isTracksView = value;
                RaisePropertyChanged("IsTracksView");
                if (!value) ResetTracksView();
            }
        }

        private bool _isSelectAllTracksChecked;
        public bool IsSelectAllTracksChecked
        {
            get => _isSelectAllTracksChecked;
            set
            {
                _isSelectAllTracksChecked = value;
                RaisePropertyChanged("IsSelectAllTracksChecked");
                foreach (var item in _tracksCollectionCopy)
                {
                    item.IsSelected = value;
                }
                UpdateTracksViewSelectedItems();
            }
        }

        private bool _isSelectedPlaylistExpanded;
        public bool IsSelectedPlaylistExpanded
        {
            get => _isSelectedPlaylistExpanded;
            set
            {
                _isSelectedPlaylistExpanded = value;
                RaisePropertyChanged("IsSelectedPlaylistExpanded");

                if (value && SelectedPlaylistCollection.FirstOrDefault() != null)
                {
                    Messenger.Default.Send(new MessengerHelper
                    {
                        Item = SelectedPlaylistCollection.FirstOrDefault(),
                        Action = MessengerAction.ScrollToItem,
                        Target = TargetView.SelectedPlaylist
                    });
                }
            }
        }

        private bool _isSelectAllChecked;
        public bool IsSelectAllChecked
        {
            get => _isSelectAllChecked;
            set
            {
                _isSelectAllChecked = value;
                RaisePropertyChanged("IsSelectAllChecked");
                if (value)
                {
                    List<Playlist> items = new List<Playlist>();
                    foreach (var item in AdvancedCollectionView)
                        items.Add((Playlist)item);

                    AddToSelected(items);
                }
                else
                {
                    List<Playlist> items = new List<Playlist>();
                    foreach (var item in AdvancedCollectionView)
                        items.Add((Playlist)item);

                    RemoveFromSelected(items);
                }
            }
        }

        private string _pageTitle;
        public string PageTitle
        {
            get { return this._pageTitle; }
            set
            {
                _pageTitle = value;
                RaisePropertyChanged("PageTitle");
            }
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                _searchText = value;
                RaisePropertyChanged("SearchText");
                FilterPlaylistCollectionView();
            }
        }

        private bool _isLoading = false;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                RaisePropertyChanged("IsLoading");
            }
        }

        private PlaylistCategory _selectedPlaylistCategory;
        public PlaylistCategory SelectedPlaylistCategory
        {
            get { return _selectedPlaylistCategory; }
            set
            {
                _selectedPlaylistCategory = value;
                RaisePropertyChanged("SelectedPlaylistCategory");
                if (value != null)
                {
                    foreach (var item in PlaylistCategoryList)
                    {
                        if (SelectedPlaylistCategory != item)
                            item.IsSelected = false;
                        else
                            item.IsSelected = true;
                    }
                    FilterPlaylistCollectionView(true);
                }
            }
        }

        private Models.Category _activeCategory;
        public Models.Category ActiveCategory
        {
            get { return _activeCategory; }
            set
            {
                _activeCategory = value;
                RaisePropertyChanged("ActiveCategory");
            }
        }

        private bool _hasSelectedItems;
        public bool HasSelectedItems
        {
            get { return _hasSelectedItems; }
            set { _hasSelectedItems = value; RaisePropertyChanged("HasSelectedItems"); }
        }

        private ProfileUser _profile;
        public ProfileUser Profile
        {
            get => _profile;
            set { _profile = value; RaisePropertyChanged("Profile"); }
        }

        private bool _isPlaylistView;
        public bool IsPlaylistView
        {
            get { return _isPlaylistView; }
            set { _isPlaylistView = value; RaisePropertyChanged("IsPlaylistView"); }
        }

        private bool _isCompactCategory;
        public bool IsCompactCategory
        {
            get { return _isCompactCategory; }
            set { _isCompactCategory = value; RaisePropertyChanged("IsCompactCategory"); }
        }

        #endregion

        #region Methods

        public async void PlayPreview(ItemMediaBase item)
        {
            try
            {
                item.IsLoadingPreview = true;

                string url = item.MediaPreviewUrl;

                if (string.IsNullOrEmpty(url))
                {
                    switch (item.MediaBaseType)
                    {
                        case ItemMediaBaseType.Playlist:
                            var _track = await DataSource.Current.GetSigleTrackFromPlaylist(item.Id);
                            if (_track != null)
                                url = _track.MediaPreviewUrl;
                            break;
                        case ItemMediaBaseType.Track:
                            break;
                    }

                    item.MediaPreviewUrl = url;
                }

                if (!string.IsNullOrEmpty(url))
                {
                    MediaElementSource = new Uri(url);
                    Messenger.Default.Send(MediaControlType.Play); //mediaElement.Play();
                }

                item.IsLoadingPreview = false;
            }
            catch (Exception)
            {
            }
        }

        private void ResetCreatePlaylistDialog()
        {
            NewPlaylistName = null;
            _base64JpegData = null;
            MergeImageFilePath = null;
        }

        private void HandleCreatePlaylistMode(CreatePlaylistMode mode)
        {
            switch (mode)
            {
                case CreatePlaylistMode.Merge:
                    IsCreatePlaylistTracksMode = false;
                    IsCreatePlaylistMergeMode = true;
                    CreatePlaylistTitle = "Merge playlists";
                    break;
                case CreatePlaylistMode.Tracks:
                    IsCreatePlaylistMergeMode = false;
                    IsCreatePlaylistTracksMode = true;
                    CreatePlaylistTitle = "Create playlist from tracks";
                    break;
                case CreatePlaylistMode.Default:
                    CreatePlaylistTitle = "Create playlist";
                    break;
            }
        }

        private void Home()
        {
            //remove selected
            var list = SelectedPlaylistCollection.ToList();
            foreach (var pl in list)
            {
                SelectedPlaylistCollection.Remove(pl);
            }

            IsPlaylistView = false;
            IsCompactCategory = false;
        }

        private void ToggleTheme()
        {
            ElementTheme theme = ElementTheme.Dark;
            if (IsDarkThemeEnabled)
            {
                Helpers.localSettings.Values["theme"] = "dark";
            }
            else
            {
                Helpers.localSettings.Values["theme"] = "light";
                theme = ElementTheme.Light;
            }

            Messenger.Default.Send(theme);
        }

        private void LoadTheme()
        {
            ElementTheme activeTheme = ElementTheme.Dark;
            var theme = Helpers.localSettings.Values["theme"] as string;

            //settings not found, save the default dark theme
            if (string.IsNullOrEmpty(theme))
            {
                IsDarkThemeEnabled = true;
            }
            else
            {
                if (theme == "light")
                {
                    IsDarkThemeEnabled = false;
                    activeTheme = ElementTheme.Light;
                }
                else
                    IsDarkThemeEnabled = true;
            }
            Messenger.Default.Send(activeTheme);
        }

        #endregion

        #region Commands

        private RelayCommand<ItemMediaBase> _addQuickAccessCommand;
        public RelayCommand<ItemMediaBase> AddQuickAccessCommand
        {
            get
            {
                if (_addQuickAccessCommand == null)
                {
                    _addQuickAccessCommand = new RelayCommand<ItemMediaBase>(async(item) =>
                    {
                        IsLoading = true;
                        if(item != null)
                        {
                            await QuickAccessItem.Add(new QuickAccessItem(item.Id, item.Uri));
                        }

                        IsLoading = false;
                    });
                }
                return _addQuickAccessCommand;
            }
        }

        private RelayCommand _toggleQuickAccessCommand;
        public RelayCommand ToggleQuickAccessCommand
        {
            get
            {
                if (_toggleQuickAccessCommand == null)
                {
                    _toggleQuickAccessCommand = new RelayCommand(() =>
                    {
                        ShowQuickAccessView = !ShowQuickAccessView;
                    });
                }
                return _toggleQuickAccessCommand;
            }
        }

        private RelayCommand _refreshCommand;
        public RelayCommand RefreshCommand
        {
            get
            {
                if (_refreshCommand == null)
                {
                    _refreshCommand = new RelayCommand(() =>
                    {
                        Refresh();
                    });
                }
                return _refreshCommand;
            }
        }

        private RelayCommand<ItemMediaBase> _stopPreviewCommand;
        public RelayCommand<ItemMediaBase> StopPreviewCommand
        {
            get
            {
                if (_stopPreviewCommand == null)
                {
                    _stopPreviewCommand = new RelayCommand<ItemMediaBase>((item) =>
                    {
                        if (item != null) item.IsLoadingPreview = false;
                        MediaElementSource = null;
                        Messenger.Default.Send(MediaControlType.Stop); //mediaElement.Stop();
                    });
                }
                return _stopPreviewCommand;
            }
        }

        private RelayCommand<ItemMediaBase> _playPreviewCommand;
        public RelayCommand<ItemMediaBase> PlayPreviewCommand
        {
            get
            {
                if (_playPreviewCommand == null)
                {
                    _playPreviewCommand = new RelayCommand<ItemMediaBase>((item) =>
                    {
                        if (item != null)
                        {
                            PlayPreview(item);
                        }
                    });
                }
                return _playPreviewCommand;
            }
        }

        private RelayCommand<Models.Category> _categoryItemClickedCommand;
        public RelayCommand<Models.Category> CategoryItemClickedCommand
        {
            get
            {
                if (_categoryItemClickedCommand == null)
                {
                    _categoryItemClickedCommand = new RelayCommand<Models.Category>((item) =>
                    {
                        SwitchCategory(item);
                    });
                }
                return _categoryItemClickedCommand;
            }
        }

        private RelayCommand<Track> _playTrackCommand;
        public RelayCommand<Track> PlayTrackCommand
        {
            get
            {
                if (_playTrackCommand == null)
                {
                    _playTrackCommand = new RelayCommand<Track>((item) =>
                    {
                        if (item != null)
                        {
                            PlayCurrentPlaylistTracks(item);
                        }
                    });
                }
                return _playTrackCommand;
            }
        }

        private RelayCommand<Playlist> _playPlaylistCommand;
        public RelayCommand<Playlist> PlayPlaylistCommand
        {
            get
            {
                if (_playPlaylistCommand == null)
                {
                    _playPlaylistCommand = new RelayCommand<Playlist>((item) =>
                    {
                        PlayItem(item);
                    });
                }
                return _playPlaylistCommand;
            }
        }       

        private RelayCommand _closeButtonCommand;
        public RelayCommand CloseButtonCommand
        {
            get
            {
                if (_closeButtonCommand == null)
                {
                    _closeButtonCommand = new RelayCommand(() =>
                    {
                        Home();
                    });
                }
                return _closeButtonCommand;
            }
        }

        private RelayCommand _closeTracksButtonCommand;
        public RelayCommand CloseTracksButtonCommand
        {
            get
            {
                if (_closeTracksButtonCommand == null)
                {
                    _closeTracksButtonCommand = new RelayCommand(() =>
                    {
                        if (_useConnectedAnimation)
                            Views.MainPage.Current.UndoIt(CurrentPlaylist);
                        else
                            IsPopupActive = false;

                        // reset
                        _useConnectedAnimation = true;
                    });
                }
                return _closeTracksButtonCommand;
            }
        }

        #endregion
    }

    public enum CreatePlaylistMode
    {
        Merge,
        Tracks,
        Default
    }
}
