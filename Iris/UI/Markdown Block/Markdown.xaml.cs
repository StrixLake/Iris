using System.Collections.Generic;
using System.ComponentModel;
using Windows.Foundation;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;


// a wrapper over the markdown textblock
// to inject custom context flyout
// since community markdown textblock 
// does not let you modify it
namespace Iris.UI
{
    public partial class Markdown : UserControl
    {
        // we have to keep a reference to the
        // backing message that this markdown
        // displays so we can subscribe to 
        // property changes in it's content
        readonly Core.Message message;

        // "element" arg can be "reasoning" or "content" and is used to decide
        // whether to show resoning or text from Core.Message
        readonly string element;

        // this list holds all the rich text blocks so it can be used to
        // copy or select all elements within it
        List<RichTextBlock> richTextBlocks = [];

        RichTextBlock? richTextBlockInContext;

        public Markdown(Core.Message message, string element)
        {
            if (!(element == "reasoning" || element == "content")) throw new System.Exception("invalid element name passed to display in markdown");

            InitializeComponent();
            this.message = message;
            this.element = element;
            message.PropertyChanged += MessageChanged;

            if (element == "content") InnerMarkdown.Text = message.Content;
            if (element == "reasoning") InnerMarkdown.Text = message.Reasoning;
        }

        private void Markdown_Rendered(object sender, CommunityToolkit.WinUI.UI.Controls.MarkdownRenderedEventArgs e)
        {
            // traverse the visual tree and add the custom context flyout to each rich textblock
            List<RichTextBlock> richTextBlocksFound = [];
            FindVisualElement<RichTextBlock>(sender, richTextBlocksFound);

            foreach(RichTextBlock richTextBlock in richTextBlocksFound)
            {
                // richTextBlock.ContextFlyout = MarkdownFlyout;
                // this has to be set to null so it doesn't use it's default
                // flyout
                richTextBlock.ContextFlyout = null;
                // we subscribe to the context requested event
                // to save a reference to it so we can use it later for menu flyout click events
                // since it's not possible to get the rich text block from that event
                // by travelling up the visual tree
                // this event will only fire if we don't set the contextflyout
                // property of the ui element, so we have to manually show the
                // menu flyout at the mouse position
                richTextBlock.ContextRequested += (UIElement sender, ContextRequestedEventArgs args) =>
                {
                    richTextBlockInContext = (RichTextBlock)sender;
                    // get the position of the right click
                    args.TryGetPosition(sender, out Point point);
                    MarkdownFlyout.ShowAt(sender, point);
                };
            }

            richTextBlocks = richTextBlocksFound;

        }

        // when we start editing, make the edit text box visible
        // and make other blocks collapsed
        // also update the text that we are going to edit
        private void Edit_Start(object sender, RoutedEventArgs e)
        {
            InnerTextBox_Edit.Visibility = Visibility.Visible;
            InnerMarkdown.Visibility = Visibility.Collapsed;
            InnerTextBlock.Visibility = Visibility.Collapsed;

            if (element == "reasoning") InnerTextBox_Edit.Text = message.Reasoning;
            if (element == "content") InnerTextBox_Edit.Text = message.Content;
        }


        // once the edit completes, we change the visibility of text box back to collapsed, bring
        // markdown block back to visibility and update the content
        private void Edit_Complete(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
        {
            InnerTextBox_Edit.Visibility = Visibility.Collapsed;

            if (element == "content") message.Content = InnerTextBox_Edit.Text;
            if (element == "reasoning") message.Reasoning = InnerTextBox_Edit.Text;

            InnerMarkdown.Visibility = Visibility.Visible;

        }


        // we use this event to check even the content changes to decide
        // which of the 3 ui text elements to show
        // like don't show markdown element if content is streaming
        void MessageChanged(object? sender, PropertyChangedEventArgs args)
        {
            // if either of the display text changes, check the element we are updating
            // and update that
            if(args.PropertyName == nameof(message.Content) || args.PropertyName == nameof(message.Reasoning))
            {
                if (element == "reasoning") InnerTextBlock.Text = message.Reasoning;
                if (element == "content") InnerTextBlock.Text = message.Content;
                // this event is invoked when edit finishes
                // update the markdown block too if the message is not streaming
                if(!message.Streaming)
                {
                    if (element == "reasoning") InnerMarkdown.Text = message.Reasoning;
                    if (element == "content") InnerMarkdown.Text = message.Content;
                }
            }

            // if the streaming property changes to false, then we display the markdown block
            // if it's not streaming and collapse it if its streaming
            if(args.PropertyName == nameof(message.Streaming))
            {
                if (message.Streaming)
                { 
                    InnerMarkdown.Visibility = Visibility.Collapsed;
                    InnerTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    InnerMarkdown.Visibility = Visibility.Visible;
                    InnerTextBlock.Visibility = Visibility.Collapsed;
                    if (element == "content") InnerMarkdown.Text = message.Content;
                    if (element == "reasoning") InnerMarkdown.Text = message.Reasoning;
                }
            }
        }

        static void FindVisualElement<T>(object visualElement, List<T> elementsFound)  where T : DependencyObject
        {
            int elements = VisualTreeHelper.GetChildrenCount((DependencyObject)visualElement);

            for(int i = 0; i < elements; i++)
            {
                DependencyObject childElement = VisualTreeHelper.GetChild((DependencyObject)visualElement, i);

                if(childElement.GetType().Equals(typeof(T)))
                {
                    elementsFound.Add((T)childElement);
                }
                else
                {
                    FindVisualElement<T>(childElement, elementsFound);
                }
            }
        }

    }
}