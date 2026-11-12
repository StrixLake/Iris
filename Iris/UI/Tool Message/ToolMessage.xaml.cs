using System;
using Core = Iris.Core;
using Iris.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace Iris.UI
{
    public partial class ToolMessage : UserControl
    {
        static Queue<ToolMessageLight> cachedMessage = [];

        static ToolMessage()
        {
            for(int i = 0; i < 150; ++i)
            {
                cachedMessage.Enqueue(new ToolMessageLight());
            }
        }

        public static ToolMessageLight GetCached(Core.Message message)
        {
            var x = cachedMessage.Dequeue();
            cachedMessage.Enqueue(x);
            x.Text = message.Reasoning;
            return x;
        }

        readonly Core.Message toolcall;

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
        }
    }
}