using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI;
using Spotify_search_helper.Data;
using Spotify_search_helper.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;

namespace Spotify_search_helper.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public static MainPageViewModel Current = null;

        public MainPageViewModel()
        {
            Current = this;
            Initialize();

            PageTitle = "Testing playlists";
        }

        private async void Initialize()
        {
            IsLoading = true;

            var categories = Category.GetCategoryItems();
            foreach (var item in categories)
                CategoryList.Add(item);

            var playlistCategories = PlaylistCategory.GetCategoryItems();
            foreach (var item in playlistCategories)
                PlaylistCategoryList.Add(item);

            SelectedPlaylistCategory = PlaylistCategoryList.FirstOrDefault();

            this.Profile = await DataSource.Current.GetProfile();

            // Set up the AdvancedCollectionView with live shaping enabled to filter and sort the original list
            var sourceItems = await DataSource.Current.GetPlaylists();
            if (sourceItems != null) _playlistCollectionCopy.AddRange(sourceItems);
            AdvancedCollectionView = new AdvancedCollectionView(sourceItems, true);

            // And sort ascending by the property "Title"
            //AdvancedCollectionView.SortDescriptions.Add(new SortDescription("Title", SortDirection.Ascending));
            CurrentSorting = PlaylistSortList.FirstOrDefault();

            // IncrementalLoadingCollection can be bound to a GridView or a ListView. In this case it is a ListView called PeopleListView.
            //MyPlaylistSource = new IncrementalLoadingCollection<PlaylistSource, Playlist>();

            SelectedPlaylistCollection.CollectionChanged += SelectedPlaylistCollection_CollectionChanged;

            //PlaylistsFiltered = new ObservableCollection<Playlist>(MyPlaylistSource);

            //getting the rest of the playlists in the background 
            DataSource.Current.GetOther();

        }

        #region PlaylistView

        public ObservableCollection<Sorting> PlaylistSortList { get; } = new ObservableCollection<Sorting>(Sorting._playlistSortList);
        readonly List<Playlist> _playlistCollectionCopy = new List<Playlist>();

        ObservableCollection<PlaylistCategory> _playlistCategoryList = new ObservableCollection<PlaylistCategory>();
        public ObservableCollection<PlaylistCategory> PlaylistCategoryList
        {
            get => _playlistCategoryList;
            set { _playlistCategoryList = value; RaisePropertyChanged("PlaylistCategoryList"); }
        }

        ObservableCollection<Playlist> _searchSuggestionCollection = new ObservableCollection<Playlist>();
        public ObservableCollection<Playlist> SearchSuggestionCollection
        {
            get => _searchSuggestionCollection;
            set
            {
                _searchSuggestionCollection = value;
                RaisePropertyChanged("SearchSuggestionCollection");
            }
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

        private void SortPlaylistCollection(Sorting sorting)
        {
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

                //scroll to top
                Views.MainPage.Current.ScrollPlaylistViewToTop();
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

        public void FilterAdvancedCollectionView()
        {
            UpdatePlaylistCategory();
        }

        private void UpdatePlaylistCategory()
        {
            if (AdvancedCollectionView == null)
                return;

            List<Playlist> _filteredCollection = new List<Playlist>();
            using (AdvancedCollectionView.DeferRefresh())
            {
                if (SelectedPlaylistCategory != null && Profile != null)
                {
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        if (SelectedPlaylistCategory.CategoryType == PlaylistCategoryType.All)
                        {
                            AdvancedCollectionView.Filter = c => (((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Owner).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);
                            _filteredCollection.AddRange(_playlistCollectionCopy.Where(c => c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Owner).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)));
                        }
                        else
                        {
                            AdvancedCollectionView.Filter = c => (((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Owner).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) && 
                            ((Playlist)c).CategoryType == SelectedPlaylistCategory.CategoryType;
                            _filteredCollection.AddRange(_playlistCollectionCopy.Where(c => c.Title.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) ||
                            (((Playlist)c).Owner).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)  && c.CategoryType == SelectedPlaylistCategory.CategoryType));
                        }
                    }
                    else
                    {
                        if (SelectedPlaylistCategory.CategoryType == PlaylistCategoryType.All)
                        {
                            //clear filters
                            AdvancedCollectionView.Filter = c => c != null; //bit of a hack to clear filters
                            _filteredCollection.AddRange(_playlistCollectionCopy);
                        }
                        else
                        {
                            AdvancedCollectionView.Filter = c => ((Playlist)c).CategoryType == SelectedPlaylistCategory.CategoryType;
                            _filteredCollection.AddRange(_playlistCollectionCopy.Where(c => c.CategoryType == SelectedPlaylistCategory.CategoryType));
                        }
                    }
                }
            }
            UpdateAlphabet(_filteredCollection, true);
            UpdatePlaylistCategoryCount(_filteredCollection);
            
        }

        public void UpdatePlaylistCategoryCount(IEnumerable<Playlist> items = null)
        {
            if(items != null)
            {
                foreach (var item in PlaylistCategoryList)
                {
                    if (item.CategoryType != PlaylistCategoryType.All)
                    {
                        item.Count = items.Where(c => c.CategoryType == item.CategoryType).Count();
                        item.TracksCount = items.Where(x => x.CategoryType == item.CategoryType).Sum(c => c.ItemsCount);
                    }
                    else
                    {
                        item.Count = items.Count();
                        item.TracksCount = items.Sum(c => c.ItemsCount);
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
        }

        #endregion

        #region TracksView

        public void TrackSuggestionChosen(Track item)
        {
            if (item != null)
            {
                TrackSearchText = item.Title;
            }
        }

        private async void RemoveTracksFromCurrentPlaylist(IEnumerable<Track> items)
        {
            IsLoading = true;

            if (CurrentPlaylist != null)
            {
                try
                {
                    if (await DataSource.Current.RemoveItemFromPlaylist(CurrentPlaylist.Id, new List<string>(items.Select(c => c.Uri))))
                    {
                        using (TracksCollectionView.DeferRefresh())
                        {
                            foreach (var item in items)
                            {
                                TracksCollectionView.Remove(item);
                                _tracksCollectionCopy.Remove(item);
                            }
                        }
                    }
                }
                catch (Exception)
                {

                    throw;
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
            TracksCollectionView.Clear();
            TrackSearchText = "";
            CurrentTrackSorting = TracksSortList.FirstOrDefault();
            _tracksCollectionCopy.Clear();
            TracksViewHasSelectedItems = false;
            UpdateTracksViewSubTitle();
            UpdateTracksViewSelectedItems();
            IsSelectAllTracksChecked = false;
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
            }
            catch (Exception)
            {
                //
            }

            //scroll to top
            Views.MainPage.Current.ScrollTracksViewToTop();
        }

        public void FilterTracksCollectionView(string searchStr)
        {
            //make sure the filtered items are clear
            _filteredTracksCollection.Clear();
            using (TracksCollectionView.DeferRefresh())
            {
                if (!string.IsNullOrEmpty(searchStr))
                {
                    TracksCollectionView.Filter = c => (((Track)c).Title).Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Track)c).Album).Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                        (((Track)c).ArtistsStr).Contains(searchStr, StringComparison.CurrentCultureIgnoreCase); //add artist search
                    _filteredTracksCollection.AddRange(_tracksCollectionCopy.Where(c => c.Title.Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                    c.Album.Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                    c.ArtistsStr.Contains(searchStr, StringComparison.CurrentCultureIgnoreCase)));
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
            else
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
                FilterTracksCollectionView(value);

                //if searchtext is cleared, show all items
            }
        }

        public ObservableCollection<Sorting> TracksSortList { get; } = new ObservableCollection<Sorting>(Sorting._tracksSortList);
        readonly List<Track> _tracksCollectionCopy = new List<Track>();
        readonly List<Track> _filteredTracksCollection = new List<Track>();

        ObservableCollection<Playlist> _tracksViewSearchSuggestions = new ObservableCollection<Playlist>();
        public ObservableCollection<Playlist> TracksViewSearchSuggestions
        {
            get => _tracksViewSearchSuggestions;
            set
            {
                _tracksViewSearchSuggestions = value;
                RaisePropertyChanged("TracksViewSearchSuggestions");
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

        private RelayCommand<Track> _removeSelectedFromCurrentPlaylistCommand;
        public RelayCommand<Track> RemoveSelectedFromCurrentPlaylistCommand
        {
            get
            {
                if (_removeSelectedFromCurrentPlaylistCommand == null)
                {
                    _removeSelectedFromCurrentPlaylistCommand = new RelayCommand<Track>((item) =>
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
        #endregion

        #region Incremental loading

        //ObservableCollection<Playlist> PlaylistsFiltered;

        //     public void Filter(string str)
        //     {
        //         /* Perform a Linq query to find all Person objects (from the original People collection)
        //that fit the criteria of the filter, save them in a new List called TempFiltered. */
        //         List<Playlist> TempFiltered;

        //         /* Make sure all text is case-insensitive when comparing, and make sure 
        //         the filtered items are in a List object */
        //         TempFiltered = MyPlaylistSource.Where(c => c.Title.Contains(str, StringComparison.InvariantCultureIgnoreCase)).ToList();

        //         // First, remove any  objects in PlaylistFiltered that are not in TempFiltered
        //         for (int i = PlaylistsFiltered.Count - 1; i >= 0; i--)
        //         {
        //             var item = PlaylistsFiltered[i];
        //             if (!TempFiltered.Contains(item))
        //             {
        //                 PlaylistsFiltered.Remove(item);
        //             }
        //         }

        //         /* Next, add back any Person objects that are included in TempFiltered and may 
        //not currently be in PeopleFiltered (in case of a backspace) */

        //         foreach (var item in TempFiltered)
        //         {
        //             if (!PlaylistsFiltered.Contains(item))
        //             {
        //                 PlaylistsFiltered.Add(item);
        //             }
        //         }
        //     }

        #endregion

        #region External Calls

        public void SelectedSearchItem(Playlist item)
        {
            SearchText = item.Title;
            LoadPlaylistTracks(item);
        }

        private void ScrollToPlaylistAlphabet(string alphabet)
        {
            if (AdvancedCollectionView != null && AdvancedCollectionView.Source != null)
            {
                object item = null;
                if (alphabet == "#") {
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
                if (item != null) Views.MainPage.Current.ScrollToPlaylistAlphabet(item);
            }
        }

        private void ToggleTheme(bool isDarkThemeEnabled)
        {
            if (IsDarkThemeEnabled)
                Helpers.localSettings.Values["theme"] = "dark";
            else
                Helpers.localSettings.Values["theme"] = "light";

            Views.MainPage.Current.ToggleDarkTheme(isDarkThemeEnabled);
        }

        public void LoadTheme()
        {
            var theme = Helpers.localSettings.Values["theme"] as string;

            //settings not found, save the default dark theme
            if (string.IsNullOrEmpty(theme))
            {
                IsDarkThemeEnabled = true;
            }
            else
            {
                if (theme == "light")
                    IsDarkThemeEnabled = false;
                else
                    IsDarkThemeEnabled = true;
            }
        }

        public PlaylistCategoryType GetPlaylistCategory()
        {
            return SelectedPlaylistCategory != null ? SelectedPlaylistCategory.CategoryType : PlaylistCategoryType.All;
        }

        public void UpdateSelectedPlaylistCategory(int count)
        {
            if (SelectedPlaylistCategory != null)
                SelectedPlaylistCategory.Count = count;
        }

        #endregion

        #region Properties
        private const int _searchSuggestionsLimit = 5;

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
                ToggleTheme(value);
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
                _tracksCollectionCopy.Where(c => c.IsSelected = value);
                UpdateTracksViewSelectedItems();
                //if (value)
                //{
                //    foreach (var item in TracksCollectionView)
                //        ((Track)item).IsSelected = true;
                //}
                //else
                //{
                //    foreach (var item in TracksCollectionView)
                //        ((Track)item).IsSelected = false;
                //}
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

                if (!value) Views.MainPage.Current.ScrollSelectedPlaylistViewToTop();
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
                UpdateSearchSuggestions();

                //if searchtext is cleared, show all items
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
                //if category changes, clear list or should we rather use collectionViewSource?

                _selectedPlaylistCategory = value;
                RaisePropertyChanged("SelectedPlaylistCategory");
                if (value != null) UpdatePlaylistCategory();
            }
        }

        private Category _activeCategory;
        public Category ActiveCategory
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

        private async void LoadPlaylistTracks(Playlist item)
        {
            //PersonsGridView.ScrollIntoView(selectedItem, ScrollIntoViewAlignment.Default);
            //PersonsGridView.UpdateLayout();
            IsLoading = true;

            CurrentTrackSorting = (CurrentTrackSorting == null) ? TracksSortList.FirstOrDefault() : null;
            TracksViewHasSelectedItems = false;

            if (item != null)
            {
                if (TracksCollectionView == null)
                    TracksCollectionView = new AdvancedCollectionView();
                else
                    TracksCollectionView.Clear();

                if (CurrentTrackSorting == null) CurrentSorting = TracksSortList.FirstOrDefault();
                using (TracksCollectionView.DeferRefresh())
                {


                    CurrentPlaylist = item;
                    IsPopupActive = true;
                    IsTracksView = true;

                    Views.MainPage.Current.DoIt(item);

                    var items = await DataSource.Current.GetSpotifyTracks(item.Id);
                    if (items != null)
                    {
                        _tracksCollectionCopy.Clear();
                        _tracksCollectionCopy.AddRange(items);

                        foreach (var track in items)
                        {
                            TracksCollectionView.Add(track);
                        }
                    }
                }
            }

            UpdateTracksViewSubTitle();
            IsLoading = false;
        }

        private void UpdateSearchSuggestions()
        {
            //clear current list
            SearchSuggestionCollection.Clear();

            if (!string.IsNullOrEmpty(SearchText))
            {
                var matches = _playlistCollectionCopy.Where(c => c.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                c.Owner.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));

                if (matches != null && matches.Count() > 0)
                {
                    //limit the number of results to return here
                    foreach (var item in matches)
                    {
                        SearchSuggestionCollection.Add(item);
                        if (SearchSuggestionCollection.Count >= _searchSuggestionsLimit)
                            break;
                    }
                }
                else
                {
                    //Show option to search online
                    SearchSuggestionCollection.Add(new Playlist
                    {
                        Title = "No results found",
                        CategoryType = PlaylistCategoryType.All
                    });
                }
            }
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

        private void SwitchCategory(Category category)
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

        #endregion

        #region collections

        public IncrementalLoadingCollection<PlaylistSource, Playlist> _myPlaylistSource;
        public IncrementalLoadingCollection<PlaylistSource, Playlist> MyPlaylistSource
        {
            get => _myPlaylistSource;
            set
            {
                _myPlaylistSource = value;
                RaisePropertyChanged("MyPlaylistSource");
            }
        }

        ObservableCollection<Playlist> _playlistCollection = new ObservableCollection<Playlist>();
        public ObservableCollection<Playlist> PlaylistCollection
        {
            get => _playlistCollection;
            set { _playlistCollection = value; RaisePropertyChanged("PlaylistCollection"); }
        }

        ObservableCollection<Category> _categoryList = new ObservableCollection<Category>();
        public ObservableCollection<Category> CategoryList
        {
            get => _categoryList;
            set { _categoryList = value; RaisePropertyChanged("CategoryList"); }
        }

        #endregion

        #region Commands

        

        private RelayCommand<Playlist> _playlistItemClickCommand;
        public RelayCommand<Playlist> PlaylistItemClickCommand
        {
            get
            {
                if (_playlistItemClickCommand == null)
                {
                    _playlistItemClickCommand = new RelayCommand<Playlist>((item) =>
                    {
                        LoadPlaylistTracks(item);
                    });
                }
                return _playlistItemClickCommand;
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

        private RelayCommand<Category> _categoryItemClickedCommand;
        public RelayCommand<Category> CategoryItemClickedCommand
        {
            get
            {
                if (_categoryItemClickedCommand == null)
                {
                    _categoryItemClickedCommand = new RelayCommand<Category>((item) =>
                    {
                        SwitchCategory(item);
                    });
                }
                return _categoryItemClickedCommand;
            }
        }

        private RelayCommand<Category> _closeButtonCommand;
        public RelayCommand<Category> CloseButtonCommand
        {
            get
            {
                if (_closeButtonCommand == null)
                {
                    _closeButtonCommand = new RelayCommand<Category>((item) =>
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
                        //IsPopupActive = false;
                        //IsTracksView = false;
                        //_tracksCollectionCopy.Clear();
                        //TracksCollectionView.Clear();
                        Views.MainPage.Current.UndoIt(CurrentPlaylist);
                    });
                }
                return _closeTracksButtonCommand;
            }
        }

        #endregion
    }

}
