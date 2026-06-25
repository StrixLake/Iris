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

        void SendMessage(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (sender == null || activeChat == null) return;

            // let the main window handle
            // it being a new chat
            if (activeChat.title == "New Chat")
            {
                NewMessage?.Invoke();
            }
            activeChat.SendMessage(promptField.Text);
            promptField.Text = "";
                
        }
        void Cancel(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (activeChat != null)
            {
                activeChat.cancellationToken.Cancel();
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

        private void Regenerate(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem regen_item = (MenuFlyoutItem)sender;
            Binding_Message regen_messege =  (Binding_Message)regen_item.DataContext;
            int regen_index = activeChat?.messages.IndexOf(regen_messege) ?? 1;

            if (regen_index == -1) return;

            if (regen_messege.role != "assistant") regen_index++;

            activeChat?.SendPartialMessage(regen_index);

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
