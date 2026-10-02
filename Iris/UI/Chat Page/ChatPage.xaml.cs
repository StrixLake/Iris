using System.Collections.Generic;
using System.Collections.ObjectModel;
using Core = Iris.Core;
using Microsoft.UI.Xaml.Controls;

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

        }

    }
}