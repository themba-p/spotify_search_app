using GalaSoft.MvvmLight;
using Spotify_search_helper.Data;
using Spotify_search_helper.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_search_helper.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        public static MainPageViewModel Current = null;
        public MainPageViewModel()
        {
            Current = this;
            Initialize();
            /*var catefories = Category.GetCategories();
            foreach (var item in catefories)
                CategoryList.Add(item);*/
        }

        private async void Initialize()
        {
            IsLoading = true;

            //CategoryList = new ObservableCollection<Category>();
            var catefories = Category.GetCategories();
            foreach (var item in catefories)
                CategoryList.Add(item);
            DataSource source = new DataSource();
            await source.Initialize();
            this.Profile = await source.GetProfile();

            //handle closing loading progress in DataSource, need an alternative
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

        private ProfileUser _profile;
        public ProfileUser Profile
        {
            get => _profile; 
            set { _profile = value; RaisePropertyChanged("Profile"); }
        }

        #region collections

        ObservableCollection<Category> _categoryList = new ObservableCollection<Category>();
        public ObservableCollection<Category> CategoryList
        {
            get => _categoryList; 
            set { _categoryList = value; RaisePropertyChanged("CategoryList"); }
        }

        #endregion
    }
}
