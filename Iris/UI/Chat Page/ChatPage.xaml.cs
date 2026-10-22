using System.Collections;
using System.Collections.ObjectModel;
using Core = Iris.Core;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Iris.UI
{
    public partial class ChatPage : UserControl
    {

        readonly UI.PromptField promptField;
        
        Core.Worker worker;
        
        public ChatPage(Core.Worker worker)
        {
            InitializeComponent();

            this.worker = worker;
            promptField = new PromptField(worker, (e) => {});
            prompt.Content = promptField;
        }

        void PrepareControl(ItemsRepeater repeater, ItemsRepeaterElementPreparedEventArgs args)
        {
            if (args.Element is not ContentControl contentControl) return;

            Core.Message message = worker.Context[args.Index];

            if (message.Role == "user")
            {
                contentControl.Content = new UserMessage(message, (msgToDelete) => { worker.Context.Remove(msgToDelete); });
            }
            else if (message.Role == "assistant")
            {
                contentControl.Content = new AssistantMessage(message, (msgToDelete) => { worker.Context.Remove(msgToDelete); }, worker.RegenerateMessage);
            }
            else if (message.Role == "tool")
            {
                contentControl.Content = new ToolMessage(message);
            }
            else
            {
                contentControl.Content = new UserControl() { Visibility = Visibility.Collapsed };
            }
        }

        void ItemDragOver(object sender, DragEventArgs args)
        {
            args.AcceptedOperation = DataPackageOperation.Copy;
        }

        async void ItemsDrop(object sender, DragEventArgs args)
        {
            if (!args.DataView.Contains(StandardDataFormats.StorageItems)) return;
            
            IReadOnlyList<IStorageItem> items = await args.DataView.GetStorageItemsAsync();
            
            foreach(IStorageItem item in items)
            {
                if(item is StorageFile storageFile)
                {
                    if (storageFile.ContentType.StartsWith("image"))
                    {
                        using Stream stream = await storageFile.OpenStreamForReadAsync();
                        byte[] bytes = new byte[stream.Length];
                        _ = await stream.ReadAsync(bytes, 0, bytes.Length);
                        string image = $"data:{storageFile.ContentType};base64," + Convert.ToBase64String(bytes);
                        promptField.images.Add(image);
                    }

                    else if (storageFile.ContentType.StartsWith("text"))
                    {
                        using Stream stream = await storageFile.OpenStreamForReadAsync();
                        byte[] bytes = new byte[stream.Length];
                        _ = await stream.ReadAsync(bytes, 0, bytes.Length);
                        string text = Encoding.UTF8.GetString(bytes);

                        promptField.fileAttachments.Add(new Tuple<string, string>(storageFile.Name, text));
                    }
                }
            }
            
        }

    }
}