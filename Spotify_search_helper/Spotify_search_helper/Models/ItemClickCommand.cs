using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Spotify_search_helper.Models
{
    public static class ItemClickCommand
    {
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached("Command", typeof(ICommand),
        typeof(ItemClickCommand), new PropertyMetadata(null, OnCommandPropertyChanged));

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        public static ICommand GetCommand(DependencyObject d)
        {
            return (ICommand)d.GetValue(CommandProperty);
        }

        private static void OnCommandPropertyChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
        {
            ListViewBase control = d as ListViewBase;
            if (control != null)
                control.ItemClick += OnItemClick;
        }

        private static void OnItemClick(object sender, ItemClickEventArgs e)
        {
            ListViewBase control = sender as ListViewBase;
            var command = GetCommand(control);

            if (command != null && command.CanExecute(e.ClickedItem))
                command.Execute(e.ClickedItem);
        }
    }

}
