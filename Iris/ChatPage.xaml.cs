using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Iris
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ChatPage : Page, INotifyPropertyChanged
    {
        public delegate void NewMessageEvent();
        public ChatPage()
        {
            InitializeComponent();
        }

        void SendMessage(object sender, KeyRoutedEventArgs eventArgs)
        {
            if (sender == null || activeChat == null) return;

            if (eventArgs.Key == Windows.System.VirtualKey.Control)
            {
                promptField.AcceptsReturn = false;
                return;
            }

            if (!promptField.AcceptsReturn)
            {
                if (eventArgs.Key == Windows.System.VirtualKey.Enter)
                {
                    // let the main window handle
                    // it being a new chat
                    if (activeChat.title == "New Chat")
                    {
                        NewMessage?.Invoke();
                    }
                    activeChat.SendMessage(promptField.Text);
                    promptField.Text = "";
                }

            }
        }
        void Cancel(object sender, KeyRoutedEventArgs args)
        {
            CoreVirtualKeyStates ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
            if (ctrl == CoreVirtualKeyStates.Down && args.Key == VirtualKey.Back && activeChat != null)
            {
                activeChat.cancellationToken.Cancel();
            }
        }
        private void promptField_KeyUp(object sender, KeyRoutedEventArgs args)
        {
            if(args.Key == Windows.System.VirtualKey.Control)
            {
                promptField.AcceptsReturn = true;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event NewMessageEvent? NewMessage;

        Chat? activeChat_;
        public Chat? activeChat
        {
            get { return activeChat_; }
            set
            {
                activeChat_ = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(activeChat)));
            }
        }

    }


    public class BoolToColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool is_white = (bool)value;
            if (is_white)
            {
                return new SolidColorBrush(Colors.White);
            }
            else return new SolidColorBrush(Colors.DeepSkyBlue);
        }
        public object ConvertBack(object value, Type targetType,
            object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
