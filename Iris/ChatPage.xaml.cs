using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.AccessControl;
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
            args.Handled = true;
                
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
            Binding_Message regen_message =  (Binding_Message)regen_item.DataContext;
            int regen_index = activeChat?.messages.IndexOf(regen_message) ?? 1;

            if (regen_index == -1) return;

            // if the message to regen is a user, then we check the role of the next message
            // if that role is also user or is the end of list, then we create an assistant message and insert
            // it after this one
            if (regen_message.role == "user")
            {
                // if the regen index is the last message
                if (regen_index == activeChat?.messages.Count - 1)
                {
                    activeChat?.messages.Add(new Binding_Message() { role = "assistant", isAssistantMessage = true });
                }
                // if the next message is also from user
                else if (activeChat?.messages[regen_index + 1].role == "user")
                {
                    activeChat?.messages.Insert(regen_index +1, new Binding_Message() { role = "assistant", isAssistantMessage = true });
                }
                regen_index++;
            }

            activeChat?.SendPartialMessage(regen_index);

        }
        void Delete(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item_to_delete = (MenuFlyoutItem)sender;
            Binding_Message msg_to_delete = (Binding_Message)item_to_delete.DataContext;

            // delete both one by one
            if (item_to_delete.Text == "Reasoning")
                msg_to_delete.reasoning = "";
            if (item_to_delete.Text == "Content")
                msg_to_delete.content = "";
            
            // if both of them have been deleted, remove this
            // msg from the list
            if(msg_to_delete.content == "" && msg_to_delete.reasoning == "")
            {
                activeChat?.messages.Remove(msg_to_delete);
            }

            activeChat?.SaveChat();
        }

        void Edit(object sender, RoutedEventArgs e)
        {
            Binding_Message msg_to_edit = (Binding_Message)((MenuFlyoutItem)sender).DataContext;
            msg_to_edit.editing = true;
            msg_to_edit.notEditing = false;
        }
        void FinishEditing(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            TextBox editBox = (TextBox)args.Element;
            Binding_Message msg_to_edit = (Binding_Message)editBox.DataContext;

            msg_to_edit.editing = false;
            msg_to_edit.notEditing = true;
            args.Handled = true;
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
