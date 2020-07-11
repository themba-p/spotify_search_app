using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace Spotify_search_helper.Models
{
    public class User : ItemBase
    {
        public User() { }

        public User(string id, string title, ImageSource image, string uri, Dictionary<string, string> externalUrls) 
            :base(id, title, image, uri, externalUrls)
        {

        }
    }
}
