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


namespace Iris.UI
{
    public partial class AssistantMessage : UserControl
    {
        Core.Message message;
        Markdown reasoningMarkdown;
        Markdown contentMarkdown;
        public AssistantMessage(Core.Message backingMessage)
        {
            InitializeComponent();
            message = backingMessage;

            reasoningMarkdown = new(message, "reasoning");
            contentMarkdown = new(message, "content");

            reasoningBlock.Content = reasoningMarkdown;
            contentBlock.Content = contentMarkdown;
        }
    }
}