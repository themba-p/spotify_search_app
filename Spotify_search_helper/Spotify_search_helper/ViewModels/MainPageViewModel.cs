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
        readonly ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

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

            DataSource source = new DataSource();
            await source.Initialize();
            this.Profile = await source.GetProfile();

            // Set up the AdvancedCollectionView with live shaping enabled to filter and sort the original list
            var sourceItems = await source.GetPlaylists();
            if (sourceItems != null) _playlistCollectionCopy.AddRange(sourceItems);
            AdvancedCollectionView = new AdvancedCollectionView(sourceItems, true);

            // And sort ascending by the property "Title"
            AdvancedCollectionView.SortDescriptions.Add(new SortDescription("Title", SortDirection.Ascending));

            // IncrementalLoadingCollection can be bound to a GridView or a ListView. In this case it is a ListView called PeopleListView.
            //MyPlaylistSource = new IncrementalLoadingCollection<PlaylistSource, Playlist>();

            SelectedPlaylistCollection.CollectionChanged += SelectedPlaylistCollection_CollectionChanged;

            //PlaylistsFiltered = new ObservableCollection<Playlist>(MyPlaylistSource);

            //getting the rest of the playlists in the background 
            source.GetOther();

        }

        #region AdvancedCollectionView

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

        readonly List<Playlist> _playlistCollectionCopy = new List<Playlist>();

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

        private void ScrollToPlaylistAlphabet(string alphabet)
        {
            if (AdvancedCollectionView != null && AdvancedCollectionView.Source != null)
            {
                var item = AdvancedCollectionView.Where(c => ((Playlist)c).Title.ToLower().StartsWith(alphabet.ToLower())).FirstOrDefault();
                if (item != null) Views.MainPage.Current.ScrollToPlaylistAlphabet(item);
            }
        }

        private void ToggleTheme(bool isDarkThemeEnabled)
        {
            if (IsDarkThemeEnabled)
                localSettings.Values["theme"] = "dark";
            else
                localSettings.Values["theme"] = "light";

            Views.MainPage.Current.ToggleDarkTheme(isDarkThemeEnabled);
        }

        public void LoadTheme()
        {
            var theme = localSettings.Values["theme"] as string;

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
                _isPopupActive = value;
                RaisePropertyChanged("IsPopupActive");
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

        private async void Login()
        {
            IsLoading = true;

            await DataSource.Current.Initialize();

            IsLoading = false;
        }

        private async void PlayItem(Playlist item)
        {
            IsLoading = true;
            await DataSource.Current.PlaySpotifyItem(item.Uri);
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

        ObservableCollection<PlaylistCategory> _playlistCategoryList = new ObservableCollection<PlaylistCategory>();
        public ObservableCollection<PlaylistCategory> PlaylistCategoryList
        {
            get => _playlistCategoryList;
            set { _playlistCategoryList = value; RaisePropertyChanged("PlaylistCategoryList"); }
        }

        #endregion

        #region Commands

        private RelayCommand _loginCommand;
        public RelayCommand LoginCommand
        {
            get
            {
                if (_loginCommand == null)
                {
                    _loginCommand = new RelayCommand(() =>
                    {
                        Login();
                    });
                }
                return _loginCommand;
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

        #endregion
    }

}
