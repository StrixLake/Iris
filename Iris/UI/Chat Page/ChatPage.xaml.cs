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

        ObservableCollection<UserControl> controls = [];
        readonly UI.PromptField promptField;
        
        Core.Worker worker;
        
        public ChatPage(Core.Worker worker)
        {
            InitializeComponent();

            this.worker = worker;
            promptField = new PromptField(worker, (e) => {});
            prompt.Content = promptField;

            AddControls(this.worker.Context);
            
            this.worker.Context.CollectionChanged += ContextChanged;
        }

        void ContextChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
            {
                AddControls(e.NewItems);
                // this was to make the last message into view
                // but i didn't find that very userful, it works though
                // controlRepeater.UpdateLayout();
                // int index = controls.Count - 1;
                // UIElement element = controlRepeater.GetOrCreateElement(index);
                // element.StartBringIntoView();
            }
        }

        void AddControls(IList messages)
        {
            foreach(Core.Message message in messages)
            {
                if(message.Role == "user")
                {
                    controls.Add(new UserMessage(message, (f)=>{}));
                }
                else if (message.Role == "assistant")
                {
                    controls.Add(new AssistantMessage(message));
                }
            }
        }

    }
}