using System;
using System.IO;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using System.Linq;
using Iris.UI;
using Core = Iris.Core;
using System.ComponentModel;
using Microsoft.UI;


namespace Iris.UI
{
    public partial class AssistantMessage : UserControl
    {
        Core.Message message;
        Markdown reasoningMarkdown;
        Markdown contentMarkdown;
        public AssistantMessage(Core.Message backingMessage, Action<Core.Message> RequestDeletion, Action<Core.Message> RequestRegen)
        {
            InitializeComponent();
            message = backingMessage;

            reasoningMarkdown = new(message, "reasoning", RequestDeletion, RequestRegen);
            contentMarkdown = new(message, "content", RequestDeletion, RequestRegen);

            reasoningBlock.Content = reasoningMarkdown;
            contentBlock.Content = contentMarkdown;

            message.messageLog!.PropertyChanged += StatusHandler;
            StatusHandler(null, new PropertyChangedEventArgs(nameof(message.messageLog.Status)));
        }

        void StatusHandler(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName != nameof(message.messageLog.Status)) return;

            if (message.messageLog?.Status == "Network Error") ErrorGradientStop.Color = Colors.Red;
            if (message.messageLog?.Status == "Starting...") ErrorGradientStop.Color = Colors.HotPink;
            else if (message.messageLog?.Status == "Cancelled") ErrorGradientStop.Color = Colors.Yellow;
            else if (message.messageLog?.Status == "Streaming...") ErrorGradientStop.Color = Colors.SkyBlue;
            else if (message.messageLog?.Status == "Finished") ErrorGradientStop.Color = (Windows.UI.Color)Application.Current.Resources.ThemeDictionaries[new String("SystemAccentColorDark1")];
        }
    }
}