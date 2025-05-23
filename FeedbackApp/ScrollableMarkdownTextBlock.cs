using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FeedbackApp
{
    public class ScrollableMarkdownTextBlock : ScrollViewer
    {
        private MarkdownTextBlock _markdownTextBlock;

        public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
            nameof(Markdown), typeof(string), typeof(ScrollableMarkdownTextBlock),
            new PropertyMetadata(string.Empty, OnMarkdownChanged));

        public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(
            nameof(TextWrapping), typeof(TextWrapping), typeof(ScrollableMarkdownTextBlock),
            new PropertyMetadata(TextWrapping.Wrap, OnTextWrappingChanged));

        public ScrollableMarkdownTextBlock()
        {
            // Initialize the MarkdownTextBlock
            _markdownTextBlock = new MarkdownTextBlock
            {
                TextWrapping = TextWrapping.Wrap
            };
            
            // Set up the ScrollViewer properties
            this.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            this.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            this.Padding = new Thickness(12);
            this.MaxHeight = 150; // Set a reasonable max height
            
            // Set the content of the ScrollViewer to the MarkdownTextBlock
            this.Content = _markdownTextBlock;
        }

        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        // Override FontSize to pass it to the inner TextBlock
        public new double FontSize
        {
            get => _markdownTextBlock.FontSize;
            set
            {
                if (_markdownTextBlock != null)
                {
                    _markdownTextBlock.FontSize = value;
                }
                base.FontSize = value;
            }
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollableMarkdownTextBlock scrollableMarkdownTextBlock)
            {
                scrollableMarkdownTextBlock.UpdateMarkdown();
            }
        }

        private static void OnTextWrappingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollableMarkdownTextBlock scrollableMarkdownTextBlock &&
                scrollableMarkdownTextBlock._markdownTextBlock != null)
            {
                scrollableMarkdownTextBlock._markdownTextBlock.TextWrapping = (TextWrapping)e.NewValue;
            }
        }

        private void UpdateMarkdown()
        {
            if (_markdownTextBlock != null)
            {
                _markdownTextBlock.Markdown = this.Markdown;
            }
        }
    }
} 