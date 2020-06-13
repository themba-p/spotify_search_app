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
        ///<summary>
        ///Initialize a new instance of the ViewModelLocator class.
        ///</summary>

        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            if (ViewModelBase.IsInDesignModeStatic)
            {
                // Create design time view services and models
            }
            else
            {
                //create run time view services and models
            }

            SimpleIoc.Default.Register<INavigationService, NavigationService>();
            SimpleIoc.Default.Register<MainPageViewModel>();
        }

        ///<summary>
        /// Gets the MainPage view model.
        /// </summary>
        /// <value>
        /// The MainPageViewModel
        /// </value>
        public MainPageViewModel StartPageInstance
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
