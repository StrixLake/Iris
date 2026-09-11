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

namespace Iris.UI
{
    public partial class PromptField : UserControl
    {
        Core.Worker settings;

        ObservableCollection<Tuple<string, string>> fileAttachments = [];
        ObservableCollection<string> images = [];
        
        public PromptField(Core.Worker worker)
        {
            InitializeComponent();
            settings = worker;
        }

        async void Paste(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
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
    }
}