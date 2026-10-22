using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Iris.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Iris
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<Tuple<Core.Worker, ChatPage>> sessions {get; set;} = [];
        
        public MainWindow()
        {
            InitializeComponent();
 
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string DirPath = System.IO.Path.Combine(documents, ".iris", "newhistory");

            foreach(string file in Directory.GetFiles(DirPath))
            {
                Core.Worker session = JsonSerializer.Deserialize<Core.Worker>(File.ReadAllText(file), Core.Worker.serializerOptions) ?? new();
                sessions.Add(new Tuple<Core.Worker, ChatPage>(session, new ChatPage(session)));
            }

            if(sessions.Count == 0)
            {
                Core.Worker session = new();
                sessions.Add(new Tuple<Core.Worker, ChatPage>(session, new ChatPage(session)));
            }
            else MainContent.Content = sessions[0].Item2;

            SessionList.SelectedItem = sessions[0];
            
            AppWindow.TitleBar.BackgroundColor = Colors.Black;

            HistoryAreaOpen.PointerEntered += (o, e) => VisualStateManager.GoToState(rootPage, "HistoryOpen", true);
            HistoryAreaClose.PointerEntered += (o, e) => VisualStateManager.GoToState(rootPage, "HistoryClose", true);

            UIState.StateInstance.PropertyChanged += FileChanged;
            FileOverlayGrid.PointerPressed += (_, _) => FileOverlayGrid.Visibility = Visibility.Collapsed;

            
            // subscribing to this event to force the rendering to
            // happen at 60 fps, this fixes the lag on the text box
            // CompositionTarget.Rendering += (_, _) => { };
        }

        public static string TitleConverter(object tuple)
        {
            if (tuple is Tuple<Core.Worker, ChatPage> session)
            {
                return session.Item1.Title;
            }
            else return "Exception, check";
        }

        private void NewChat(object sender, RoutedEventArgs e)
        {
            Core.Worker session = new();
            var sessionPage = new Tuple<Core.Worker, ChatPage>(session, new ChatPage(session));
            sessions.Add(sessionPage);
            SessionList.SelectedItem = sessionPage;
        }

        void ChangeSession(object sender, SelectionChangedEventArgs e) => MainContent.Content = ((Tuple<Core.Worker, ChatPage>)SessionList.SelectedItem).Item2;

        void FileChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(UIState.StateInstance.DisplayFile)) return;

            FileOverlayGrid.Visibility = Visibility.Visible;
            FileViewerMarkdownBlock.Text = $"### {UIState.StateInstance.DisplayFile.Item1} \n\n{UIState.StateInstance.DisplayFile.Item2}";
        }

        private void DeleteChat(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuFlyoutItem delete) return;
            if (delete.DataContext is not Tuple<Core.Worker, ChatPage> session) return;

            if(SessionList.SelectedItem == session)
            {
                // create a new chat and set that to selected
                // and then remove it
                Core.Worker worker = new();
                ChatPage chatPage = new ChatPage(worker);
                var newSession = new Tuple<Core.Worker, ChatPage>(worker, chatPage);
                sessions.Add(newSession);
                SessionList.SelectedItem = newSession;
            }
            sessions.Remove(session);
        }

        private void RenameChat(object sender, RoutedEventArgs e)
        {
            contextParent!.Children[0].Visibility = Visibility.Collapsed;
            contextParent.Children[1].Visibility = Visibility.Visible;

            TextBox textbox = (TextBox)contextParent.Children[1];
            textbox.Text = ((Tuple<Core.Worker, ChatPage>)contextParent.DataContext).Item1.Title;

            // increase the fps when text box is active
            CompositionTarget.Rendering += fps;
        }


        private void HistoryListContextRequested(object sender, object e)
        {
            contextParent = (Grid)((MenuFlyout)sender).Target;
        }

        Grid? contextParent;
        EventHandler<object> fps = (_, _) => { };

        private void EnterRename(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Grid grid = (Grid)args.Element;

            grid.Children[0].Visibility = Visibility.Visible;
            grid.Children[1].Visibility = Visibility.Collapsed;

            TextBox textbox = (TextBox)grid.Children[1];
            TextBlock textblock= (TextBlock)grid.Children[0];

            // sometimes the enter key may invoke this accelerator for a grid that wasn't
            // editing and the textbox text would be empty in that case, so we guard that case
            if (textbox.Text == "") return;

            ((Tuple<Core.Worker, ChatPage>)grid.DataContext).Item1.Title = textbox.Text;
            textblock.Text = textbox.Text;

            // set fps to normal when editing is finished
            CompositionTarget.Rendering -= fps;
        }

    }

}
