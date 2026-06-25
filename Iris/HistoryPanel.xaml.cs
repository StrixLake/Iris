using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
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

            if (chats != null && context_item.title != "New Chat")
            {
                // get the index of this chat and
                // open the previous chat in the
                // history if this was the active chat
                if(context_item == chat_history.SelectedItem)
                {
                    int context_index = chats.IndexOf(context_item);
                    chat_history.SelectedIndex = context_index - 1;
                    ActiveChatChange?.Invoke((Chat)chat_history.SelectedItem);
                }

                chats.Remove(context_item);
                context_item.DeleteHistory();
            }
            return;
        }

        void Duplicate(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = (MenuFlyoutItem)sender;

            Chat context_item = (Chat)item.DataContext;

            if(chats != null && context_item.title != "New Chat")
            {
                string copyJson = JsonSerializer.Serialize(context_item, JsonContext.Default.Chat);
                Chat duplicate_chat = JsonSerializer.Deserialize<Chat>(copyJson, JsonContext.Default.Chat) ?? new Chat();
                Random rnd = new();
                duplicate_chat.filename = ((int)(rnd.NextDouble() * 1_000_000)).ToString() + ".json";
                duplicate_chat.title = "Chat";
                chats.Add(duplicate_chat);
            }
        }
    }
}
