using System;
using Iris;
using Iris.UI;
using Core = Iris.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Buffer = Windows.Storage.Streams.Buffer;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ComponentModel;

namespace Iris.UI
{
    public delegate void SendMessageEvent(Core.Message? message);
    
    public partial class PromptField : UserControl, INotifyPropertyChanged
    {
        readonly Core.Worker settings;
        event SendMessageEvent? sendMessageEvent;
        public event PropertyChangedEventHandler? PropertyChanged;
        EndpointPage endpoint;

        ObservableCollection<Tuple<string, string>> fileAttachments { get; set; } = [];
        ObservableCollection<string> images { get; set; } = [];
        
        public PromptField(Core.Worker worker, SendMessageEvent sendMessageEvent)
        {
            InitializeComponent();
            settings = worker;
            endpoint = new(worker);
            Endpoints.Content = endpoint;
            this.sendMessageEvent += sendMessageEvent;

            VisualStateManager.GoToState(this, "DescriptionState", true);
            VisualStateManager.GoToState(this, "Normal", false);

            DescriptionButton.Click += (o, e) => VisualStateManager.GoToState(this, "DescriptionState", true);
            ProviderButton.Click += (o, e) => VisualStateManager.GoToState(this, "ProviderState", true);

            textField.LostFocus += (o, e) =>
            {
                if(!ModelButton.IsPressed) VisualStateManager.GoToState(this, "Normal", false);
            };
            textField.GotFocus += (o, e) => VisualStateManager.GoToState(this, "Expanded", false);
            modelFlyout.Closing += (o, e) => textField.Focus(FocusState.Keyboard);
        }

        async void Paste(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // don't paste if the menu flyout is open
            if (ModelButton.Flyout.IsOpen) return;
            // get the image or the text from the clipboard
            DataPackageView paste_content = Clipboard.GetContent();

            if(paste_content.Contains(StandardDataFormats.Text))
            {
                args.Handled = true;
                string text = await paste_content.GetTextAsync();
                // if it contains text larger than 500 characters, then add
                // it in the list
                if(text.Length > 500)
                {
                    fileAttachments.Add(new Tuple<string, string>("Pasted Text\n", text));
                }
                else
                {
                    textField.Text += text;
                }
            }

            else if(paste_content.Contains(StandardDataFormats.Bitmap))
            {
                args.Handled = true;
                RandomAccessStreamReference image_stream = await paste_content.GetBitmapAsync();
                using IRandomAccessStreamWithContentType image = await image_stream.OpenReadAsync();
                Buffer image_buffer = new Buffer((uint)image.Size);

                await image.ReadAsync(image_buffer, (uint)image.Size, InputStreamOptions.ReadAhead);

                string base64image = System.Convert.ToBase64String(image_buffer.ToArray());

                images.Add(base64image);
                
            }
        }

        void SendMessage(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            args.Handled = true;
            // construct the message object and invoke the event
            // if the text field is empty or attachments
            // are empty, send a null object
            if(textField.Text.Trim() == "" && fileAttachments.Count == 0 && images.Count == 0)
            {
                sendMessageEvent?.Invoke(null);
                settings.SendMessage(null);
                return;
            }

            Core.Message message = new()
            {
                Role = "user",
                Content = textField.Text.Trim(),
                images = this.images,
                files = this.fileAttachments
            };

            settings.SendMessage(message);

            this.images = [];
            this.fileAttachments = [];
            textField.Text = "";
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(images)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(fileAttachments)));
        }

        private void Grid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Grid grid = (Grid)sender;
            object button = grid.FindName("DeleteButton");
            if (button != null) ((Button)button).Visibility = Visibility.Visible;
            
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Grid grid = (Grid)sender;
            object button = grid.FindName("DeleteButton");
            if (button != null) ((Button)button).Visibility = Visibility.Collapsed;
        }

        private void Delete_Image(object sender, RoutedEventArgs e)
        {
            images.Remove((string)((Button)sender).DataContext);
        }
        private void Delete_file(object sender, RoutedEventArgs e)
        {
            fileAttachments.Remove((Tuple<string, string>)((Button)sender).DataContext);
        }

        // only remove the model if there is at least 2 models in the list
        // when the model is removed, check if that model was selected
        // and if it was, change the model to the first one
        private void ChangeSelectedModel(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count != 0) settings.Model = (string)e.AddedItems[0];
            if(e.RemovedItems.Count != 0 && settings.Model == (string)e.RemovedItems[0])
            {
                settings.Model = settings.Models[0];
            }
        }

        private void RemoveModel(object sender, RoutedEventArgs e)
        {
            if(settings.Models.Count > 1) settings.Models.Remove((string)((MenuFlyoutItem)sender).DataContext);
        }

        private void AddModel(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (ModelAddField.Text != "")
            {
                settings.Models.Add(ModelAddField.Text);
                ModelAddField.Text = "";
            }
        }
    }
}