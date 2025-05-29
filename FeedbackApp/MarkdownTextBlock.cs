using System.Windows;
using System.Windows.Controls;
using FeedbackApp.Markdown;

namespace FeedbackApp
{
    public class MarkdownTextBlock : TextBlock
    {
        public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
            nameof(Markdown), typeof(string), typeof(MarkdownTextBlock),
            new PropertyMetadata(string.Empty, OnMarkdownChanged));

        public MarkdownTextBlock()
        {
            // Set default font weight to SemiBold to match the original design
            this.FontWeight = FontWeights.SemiBold;
        }

        public string Markdown
        {
            get => (string)GetValue(MarkdownProperty);
            set => SetValue(MarkdownProperty, value);
        }

        private static void OnMarkdownChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MarkdownTextBlock markdownTextBlock)
            {
                markdownTextBlock.UpdateMarkdown();
            }
        }

        private void UpdateMarkdown()
        {
            this.Inlines.Clear();

            if (string.IsNullOrEmpty(Markdown))
            {
                return;
            }

            // Use the extracted markdown parser
            MarkdownParser.ParseMarkdown(Markdown, this.Inlines, this.FontSize);
        }


    }
} 