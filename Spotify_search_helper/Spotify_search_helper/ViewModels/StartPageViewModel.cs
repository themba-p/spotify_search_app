﻿using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Views;
using Spotify_search_helper.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_search_helper.ViewModels
{
    public class StartPageViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        public StartPageViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            Initialize();
        }

        private void NavigateToMain()
        {
            // Do Something
            _navigationService.NavigateTo("MainPage");
        }

        private async void Initialize()
        {
            //check if user is logged in
            //we can even initialize getting data here
            IsLoading = true;
            _ = new DataSource(); //initialize datasource
            IsAuthenticated = await DataSource.Current.IsAuthenticated();
            IsLoading = false;
            if (IsAuthenticated)
            {
                //get initial data here
                NavigateToMain();
            
            }
        }

        private async void Login()
        {
            IsLoading = true;

            await DataSource.Current.Authenticate();

            IsLoading = false;
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

        private bool _isAuthenticated = true; //just to hide the login view initialy
        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            set
            {
                _isAuthenticated = value;
                RaisePropertyChanged("IsAuthenticated");
            }
        }

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
    }
}
