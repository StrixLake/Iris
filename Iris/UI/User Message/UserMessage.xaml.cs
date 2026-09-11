using System;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Linq;



namespace Iris.UI
{
    public delegate void StringEvent(string eventarg);

    public partial class UserMessage : UserControl
    {

        Core.Message message;

        // the markdown control
        Markdown markdown;

        event StringEvent? OpenFile;

        public UserMessage(Core.Message message, StringEvent FileOpenMethod)
        {
            InitializeComponent();
            this.message = message;
            markdown = new(message, "content");

            // initialize the markdown
            MarkdownControl.Content = markdown;

            // configure visibility for file list and image list
            if (message.files.Count == 0) FileList.Visibility = Visibility.Collapsed;
            if (message.images.Count == 0) ImageList.Visibility = Visibility.Collapsed;

            OpenFile += FileOpenMethod;
        }

        void OpenFileViewer(object sender, RoutedEventArgs e)
        {
            if(sender is Button button)
            {
                if(button.DataContext is Tuple<string, string> tuple)
                {
                    string content = tuple.Item2;
                    OpenFile?.Invoke(content);

                }
            }
        }
    }


    // convert base 64 string to bitmap image
    // the string starts with "data:image/{type};base64,..."
    // and the encoding is after the ,
    public class StringToImage : IValueConverter
    {
        public object Convert(object value, Type target, object parameter, string language)
        {
            string base64_image = (string)value;
            int data_index = base64_image.IndexOf(",");

            string encoded_image = base64_image.Substring(data_index + 1);
            byte[] image = System.Convert.FromBase64String(encoded_image);

            using MemoryStream memStream = new(image);
            using IRandomAccessStream image_stream = memStream.AsRandomAccessStream();

            BitmapImage out_image = new BitmapImage();
            out_image.SetSource(image_stream);

            return out_image;

        }

        public object ConvertBack(object value, Type target, object parameter, string langauge)
        {
            throw new NotImplementedException();
        }
    }

    // give the filename to button content
    public class FileConverter : IValueConverter
    {
        public object Convert(object value, Type target, object parameter, string language)
        {
            // the object type is going to be
            // Tuple<string, string> <filename, content>
            // and we want to return the filename

            Tuple<string, string> tuple = (Tuple<string, string>)value;
            return tuple.Item1 + "\n" + tuple.Item2.Count(c => c == ' ') + " Words";

        }

        public object ConvertBack(object value, Type target, object parameter, string langauge)
        {
            throw new NotImplementedException();
        }
    }

}