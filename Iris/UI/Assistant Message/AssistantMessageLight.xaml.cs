using System;
using Microsoft.UI.Xaml.Controls;

namespace Iris.UI
{
    public sealed partial class AssistantMessageLight : UserControl
    {
        public AssistantMessageLight()
        {
            InitializeComponent();
        }

        public string Text
        {
            set => responseBlock.Text = value;
        }

    }
}
