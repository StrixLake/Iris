using System;
using Core = Iris.Core;
using Iris.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml;

namespace Iris.UI
{
    public partial class ToolMessage : UserControl
    {
        readonly Core.Message toolcall;
        readonly Markdown toolResult;

        public ToolMessage(Core.Message message)
        {
            InitializeComponent();

            toolcall = message;
            
            if(toolcall.Reasoning == "Invalid Tool Call")
            {
                header.BorderBrush = new SolidColorBrush(color: Colors.Red);
            }
            else
            {
                header.BorderBrush = new SolidColorBrush()
                {
                    Color = (Windows.UI.Color)Application.Current.Resources.ThemeDictionaries[new String("SystemAccentColorDark1")]
                };
            }

            toolResult = new Markdown(toolcall, "content", (_) => { }, (_) => { });

            toolResponse.Content = toolResult;
        }
    }
}