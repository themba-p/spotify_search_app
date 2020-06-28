using Spotify_search_helper.ViewModels;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class ProfileUser : NotificationBase
    {
        public ProfileUser() { }

        public ProfileUser(string id, string displayName, ImageSource image)
        {
            this.Id = id;
            this.DisplayName = displayName;
            this.Image = image;
        }

        private string _id;
        public string Id
        {
            get => _id;
            set => _ = SetProperty(_id, value, () => _id = value);
        }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set => _ = SetProperty(_displayName, value, () => _displayName = value);
        }

        private ImageSource _image;
        public ImageSource Image
        {
            get => _image;
            set
            {
                if (_image != value)
                    _image = value;
                _ = SetProperty(_image, value, () => _image = value);
            }
        }
    }
}
