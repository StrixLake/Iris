using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Threading;
using Iris.UI;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Streaming.Adaptive;
using Windows.System;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Iris
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window, INotifyPropertyChanged
    {
        ChatPage chatPage;
        public event PropertyChangedEventHandler? PropertyChanged;
        
        public MainWindow()
        {
            InitializeComponent();

            Core.Worker worker = new();
            
            chatPage = new ChatPage(worker);

            testcontrol.Content = chatPage;

            AppWindow.TitleBar.BackgroundColor = Colors.Black;

            // load settings
            // first load the saved models
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string DirPath = System.IO.Path.Combine(documents, ".iris");
            
            // subscribing to this event to force the rendering to
            // happen at 60 fps, this fixes the lag on the text box
            // CompositionTarget.Rendering += (_, _) => { };
        }

    }

}
