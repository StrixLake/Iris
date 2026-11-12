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
        readonly Core.Message message;
        Markdown? reasoningMarkdown;
        readonly Markdown contentMarkdown;

        readonly Action<Core.Message> requestDeletion;
        readonly Action<Core.Message> requestRegeneration;

        public AssistantMessage(Core.Message backingMessage, Action<Core.Message> RequestDeletion, Action<Core.Message> RequestRegen)
        {
            InitializeComponent();
            message = backingMessage;

            requestDeletion = RequestDeletion; 
            requestRegeneration = RequestRegen;

            contentMarkdown = new(message, "content", requestDeletion, requestRegeneration);

            contentBlock.Content = contentMarkdown;

            reasoningHeader.Expanding += ExpanderOpen;

            Loaded += (_,_) => message.messageLog!.PropertyChanged += StatusHandler;
            Unloaded += (_,_) => message.messageLog!.PropertyChanged -= StatusHandler;

            StatusHandler(null, new PropertyChangedEventArgs(nameof(message.messageLog.Status)));
        }

        bool alreadyOpened = false;
        private void ExpanderOpen(Expander sender, ExpanderExpandingEventArgs args)
        {
            if(alreadyOpened) return;
            alreadyOpened = true;

            reasoningMarkdown = new(message, "reasoning", requestDeletion, requestRegeneration);
            reasoningBlock.Content = reasoningMarkdown;
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