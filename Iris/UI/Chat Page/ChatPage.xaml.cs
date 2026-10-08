using System.Collections;
using System.Collections.ObjectModel;
using Core = Iris.Core;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Specialized;
using System;
using System.Linq;
using Microsoft.UI.Xaml;

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

        public static UserControl AddControl(Core.Message message)
        {
            if (message.Role == "user")
            {
                return new UserMessage(message, (f) => { });
            }
            else if (message.Role == "assistant")
            {
                return new AssistantMessage(message);
            }
            else
            {
                return new UserControl() { Visibility = Visibility.Collapsed};
            }
        }

    }
}