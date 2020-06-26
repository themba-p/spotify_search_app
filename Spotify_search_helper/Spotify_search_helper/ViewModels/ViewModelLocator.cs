using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spotify_search_helper.ViewModels
{
    /// <summary>
    /// This class contains static referenses to all the view models in the 
    /// application and provides an entry point 
    /// for the bindings
    /// </summary>
    public class ViewModelLocator
    {
        public const string StartPageKey = "StartPage";
        public const string MainPageKey = "MainPage";

        ///<summary>
        ///Initialize a new instance of the ViewModelLocator class.
        ///</summary>

        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            var nav = new NavigationService();
            nav.Configure(StartPageKey, typeof(Views.StartPage));
            nav.Configure(MainPageKey, typeof(Views.MainPage));
            if (ViewModelBase.IsInDesignModeStatic)
            {
                // Create design time view services and models
            }
            else
            {
                //create run time view services and models
            }

            SimpleIoc.Default.Register<INavigationService>(() => nav);
            SimpleIoc.Default.Register<MainPageViewModel>();
            SimpleIoc.Default.Register<StartPageViewModel>();
        }

        public StartPageViewModel StartPageInstance
        {
            get
            {
                return ServiceLocator.Current.GetInstance<StartPageViewModel>();
            }
        }

        ///<summary>
        /// Gets the MainPage view model.
        /// </summary>
        /// <value>
        /// The MainPageViewModel
        /// </value>
        public MainPageViewModel MainPageInstance
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MainPageViewModel>();
            }
        }

        ///<summary>
        ///The cleanup
        /// </summary>
        public static void Cleanup()
        {
            //TODO Clear the ViewModels
        }
    }
}
