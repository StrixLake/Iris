using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Iris
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HistoryPanel : Page
    {
        public delegate void ItemClickEvent(Chat clickedItem);
        public HistoryPanel()
        {
            InitializeComponent();
            chat_history.SelectedIndex = 0;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ActiveChatChange?.Invoke((Chat)e.ClickedItem);
        }

        public void SelectItem(Chat chat)
        {
            chat_history.SelectedItem = chat;
        }

        public ObservableCollection<Chat>? chats { get; set; }
        public event ItemClickEvent? ActiveChatChange;

        private void Delete(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;

            Chat context_item = (Chat)item.DataContext;

            if (chats != null) chats.Remove(context_item);

            return;

        }
    }
}
