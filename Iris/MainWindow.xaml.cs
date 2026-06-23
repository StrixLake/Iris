using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Iris
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            AppWindow.TitleBar.BackgroundColor = Colors.Black;
            
            chats = new();
            activeChat = new();
            chats.Add(activeChat);

            chatPage.NewMessage += NewMessage;
            historyPanel.ActiveChatChange += optionPanel.ActiveChatChanged;
            historyPanel.ActiveChatChange += ActiveChatChanged;

            chatPage.activeChat = activeChat;
            historyPanel.chats = chats;

            // load settings
            // first load the saved models
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string DirPath = System.IO.Path.Combine(documents, ".iris");
            string modelPath = System.IO.Path.Combine(DirPath, "models.txt");

            Directory.CreateDirectory(DirPath);
            if(!File.Exists(modelPath))
            {
                File.Create(modelPath).Dispose();
            }
            using StreamReader FileReader = new StreamReader(modelPath);
            while(!FileReader.EndOfStream)
            {
                string line = FileReader.ReadLine() ?? "";
                optionPanel.Models.Add(line);
            }

            // load the chat history
            string historyDir = System.IO.Path.Combine(DirPath, "history");
            Directory.CreateDirectory(historyDir);

            foreach(string file in Directory.EnumerateFiles(historyDir))
            {
                using StreamReader filestream = new StreamReader(file);
                Chat? newChat = JsonSerializer.Deserialize<Chat>(filestream.ReadToEnd(), JsonContext.Default.Chat);
                if(newChat != null)
                {
                    chats.Add(newChat);
                }
            }
            
            // load the api key
            string KeyFile = System.IO.Path.Combine(DirPath, "apikey.txt");
            using StreamReader keyStream = new StreamReader(KeyFile);
            Client.apikey = keyStream.ReadLine() ?? "";

        }

        // new messages
        void NewMessage()
        {
            // this method is subscribed to ChatPanel.NewMessage
            // and is invoked when the activeChat.title is "New Chat"
            // we create a new active chat with "Chat" title
            Chat newchat = new() { title = "Chat", model = optionPanel.SelectedModel};
            chatPage.activeChat = newchat;

            // and add this new chat in the chats list
            // and set that as the active chat
            chats.Add(newchat);
            activeChat = newchat;
            historyPanel.SelectItem(newchat);
        }

        void ActiveChatChanged(Chat newChat)
        {
            activeChat = newChat;
            chatPage.activeChat = newChat;
        }


        public ObservableCollection<Chat> chats { get; set; }
        public Chat activeChat { get; set; }
        

        // animation events
        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            optionPanelGrid.Visibility = Visibility.Visible;
            panelSlideAnimation.Begin();
            optionPanelGrid.IsHitTestVisible = false;
        }
        private void Grid_PointerEntered_History(object sender, PointerRoutedEventArgs e)
        {
            optionPanelGrid.Visibility = Visibility.Visible;
            historyPanelSlideAnimation.Begin();
            optionPanelGrid.IsHitTestVisible = false;
        }
        private void panelSlideAnimation_Completed(object sender, object e)
        {
            optionPanelGrid.IsHitTestVisible = true;
        }

        private void optionPanel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            optionPanelGrid.IsHitTestVisible = false;
            panelCollapseAnimation.Begin();
        }
        private void historyPanel_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            optionPanelGrid.IsHitTestVisible = false;
            historyPanelCollapseAnimation.Begin();
        }
        private void panelCollapseAnimation_Completed(object sender, object e)
        {
            optionPanelGrid.Visibility = Visibility.Collapsed;
        }

    }

}
