using System.Collections;
using System.Collections.ObjectModel;
using Core = Iris.Core;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Iris.Core;

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
                contentControl.Content = new UserMessage(message, (f) => { }, (msgToDelete) => { worker.Context.Remove(msgToDelete); });
            }
            else if (message.Role == "assistant")
            {
                contentControl.Content = new AssistantMessage(message, (msgToDelete) => { worker.Context.Remove(msgToDelete); }, worker.RegenerateMessage);
            }
            else
            {
                contentControl.Content = new UserControl() { Visibility = Visibility.Collapsed };
            }
        }

    }
}