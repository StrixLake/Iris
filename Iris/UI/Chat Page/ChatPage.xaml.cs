using System.Collections.Generic;
using System.Collections.ObjectModel;
using Core = Iris.Core;
using Microsoft.UI.Xaml.Controls;

namespace Iris.UI
{
    public partial class ChatPage : UserControl
    {
        Core.Session session;

        ObservableCollection<UserControl> controls = [];
        UI.PromptField promptField;
        public ChatPage(Core.Session chatSession)
        {
            InitializeComponent();

            session = chatSession;

            foreach(Core.Message message in session.worker.Item1)
            {
                if(message.role == "user")
                {
                    controls.Add(new UI.UserMessage(message, (i) => { }));
                }
                if(message.role == "assistant")
                {
                    controls.Add(new UI.AssistantMessage(message));
                }
            }

            promptField = new(session.worker.Item2, (msg) => { });

            prompt.Content = promptField;
        }

    }
}