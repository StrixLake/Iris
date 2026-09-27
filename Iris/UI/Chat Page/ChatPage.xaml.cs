using System.Collections.Generic;
using System.Collections.ObjectModel;
using Core = Iris.Core;
using Microsoft.UI.Xaml.Controls;

namespace Iris.UI
{
    public partial class ChatPage : UserControl
    {

        ObservableCollection<UserControl> controls = [];
        UI.PromptField promptField;
        
        public ChatPage()
        {
            InitializeComponent();
        }

    }
}