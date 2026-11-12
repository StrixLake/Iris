using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;

namespace Iris.UI
{
    public sealed partial class ToolMessageLight : UserControl
    {
        public ToolMessageLight()
        {
            InitializeComponent();
            header.BorderBrush = brush;
        }

        static Color accentColor = (Color)Application.Current.Resources.ThemeDictionaries[new String("SystemAccentColorDark1")];
        static Color redColor = Colors.Red;
        SolidColorBrush brush = new();

        public string Text
        {
            set
            {
                headerText.Text = value;
                if(value == "Invalid Tool Call") brush.Color = redColor;
                else brush.Color = accentColor;
            }
        }
    }
}
