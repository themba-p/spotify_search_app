using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Spotify_search_helper.Models
{
    public class QuickAccessItem
    {
        private static readonly string _filePath = ApplicationData.Current.LocalFolder.Path + "\\quick_access.json";
        private static List<QuickAccessItem> _items = new List<QuickAccessItem>();

        public QuickAccessItem(string id, string uri)
        {
            this.Id = id;
            this.Uri = uri;
        }

        public string Id { get; set; }
        public string Uri { get; set; }
        //Date added?

        private static async Task<bool> FileExists()
        {
            try
            {
                if (!File.Exists(_filePath))
                    await ApplicationData.Current.LocalFolder.CreateFileAsync("quick_access.json");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task<bool> Save()
        {
            try
            {
                if (await FileExists())
                    await File.WriteAllTextAsync(_filePath, JsonConvert.SerializeObject(_items));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<IEnumerable<QuickAccessItem>> Get()
        {
            try
            {
                if (await FileExists())
                {
                    var json = await File.ReadAllTextAsync(_filePath);
                    var items = JsonConvert.DeserializeObject<List<QuickAccessItem>>(json);
                    if (_items != null) _items = items;
                    return _items;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<bool> Add(QuickAccessItem item)
        {
            try
            {
                if (_items == null) _items = new List<QuickAccessItem>();
                if (_items.Where(c => c.Id == item.Id).FirstOrDefault() == null)
                {
                    _items.Add(item);
                    return await Save();
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<bool> Remove(string id)
        {
            try
            {
                _items.Remove(_items.Where(c => c.Id == id).FirstOrDefault());
                return await Save();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<bool> Clear()
        {
            try
            {
                if (_items == null) _items = new List<QuickAccessItem>();
                _items.Clear();
                return await Save();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static async Task<bool> IsQuickAccessItem(string id)
        {
            try
            {
                if (_items == null) await Get();
                return _items.Where(c => c.Id == id).Any();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
