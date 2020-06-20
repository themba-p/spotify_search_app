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

            DataSource source = new DataSource();
            await source.Initialize();
            this.Profile = await source.GetProfile();

            // Set up the AdvancedCollectionView with live shaping enabled to filter and sort the original list
            var sourceItems = await source.GetPlaylists();
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

        public void AddToCollection(IEnumerable<Playlist> items)
        {
            using (AdvancedCollectionView.DeferRefresh())
            {
                List<string> alphabet = new List<string>();
                foreach (var item in items)
                {
                    if (item.Title.Length > 0 && alphabet.Where(c => c.ToUpper() == item.Title[0].ToString().ToUpper()).FirstOrDefault() == null)
                        alphabet.Add(item.Title[0].ToString().ToUpper());

                    AdvancedCollectionView.Add(item);
                }

                alphabet = alphabet.OrderBy(c => c).ToList();
                foreach (var str in alphabet)
                {
                    if(Alphabet.Where(c => c.ToUpper().Equals(str.ToUpper())).FirstOrDefault() == null)
                        Alphabet.Add(str);
                }
            }
        }

        public void FilterAdvancedCollectionView()
        {
            UpdatePlaylistCategory();
        }

        private void UpdatePlaylistCategory()
        {
            if (AdvancedCollectionView == null)
                return;

            using (AdvancedCollectionView.DeferRefresh())
            {
                if (SelectedPlaylistCategory != null && Profile != null)
                {
                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        if (SelectedPlaylistCategory.CategoryType == PlaylistCategoryType.All)
                            AdvancedCollectionView.Filter = c => (((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase);
                        else
                        {
                            AdvancedCollectionView.Filter = c => (((Playlist)c).Title).Contains(SearchText, StringComparison.CurrentCultureIgnoreCase) && ((Playlist)c).CategoryType == SelectedPlaylistCategory.CategoryType;
                        }
                    }
                    else
                    {
                        if (SelectedPlaylistCategory.CategoryType == PlaylistCategoryType.All)
                        {
                            //clear filters
                            AdvancedCollectionView.Filter = c => c != null; //bit of a hack to clear filters
                        }
                        else
                        {
                            AdvancedCollectionView.Filter = c => ((Playlist)c).CategoryType == SelectedPlaylistCategory.CategoryType;
                        }
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

        public void UpdatePlaylistCategoryCounts()
        {
            if (AdvancedCollectionView != null)
            {
                foreach (var item in PlaylistCategoryList)
                {
                    if (item.CategoryType != PlaylistCategoryType.All)
                        item.Count = AdvancedCollectionView.Where(c => ((Playlist)c).CategoryType == item.CategoryType).Count();
                    else
                        item.Count = AdvancedCollectionView.Count;
                }
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

        private void ScrollToPlaylistAlphabet(string alphabet)
        {
            if(AdvancedCollectionView != null && AdvancedCollectionView.Source != null)
            {
                var item = AdvancedCollectionView.Where(c => ((Playlist)c).Title.ToLower().StartsWith(alphabet.ToLower())).FirstOrDefault();
                if(item != null) Views.MainPage.Current.ScrollToPlaylistAlphabet(item);
            }
        }

        #region Properties

        private string _selectedPlaylistAlphabet;
        public string SelectedPlaylistAlphabet
        {
            get => _selectedPlaylistAlphabet;
            set
            {
                _selectedPlaylistAlphabet = value;
                RaisePropertyChanged("SelectedPlaylistAlphabet");
                if(!string.IsNullOrEmpty(value)) ScrollToPlaylistAlphabet(value);
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
                if(value)
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
                //if category changes, clear list or should we rather use collectionViewSource?

                _searchText = value;
                RaisePropertyChanged("SearchText");
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

        private void Home()
        {
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

        private ObservableCollection<string> _alphabet = new ObservableCollection<string>();
        //{
        //    "a","b","c","d","e","f","g","h","i","j","k","l","m","n","o","p","q","r","s","t","u","v","w","x","y","z"
        //};
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
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is Playlist playlist)
                        playlist.IsSelected = true;
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
