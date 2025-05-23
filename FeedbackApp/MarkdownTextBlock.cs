using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Markdig;
using Markdig.Wpf;
using System.IO;
using System.Windows.Markup;
using System.Windows.Media;
using System.Text.RegularExpressions;

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

            try
            {
                // Process the markdown directly
                string[] lines = Markdown.Split('\n');
                int currentLine = 0;

                while (currentLine < lines.Length)
                {
                    string line = lines[currentLine].TrimEnd();

                    // Check if this is a header
                    if (line.StartsWith("# "))
                    {
                        // Level 1 header
                        var headerText = line.Substring(2);
                        var run = new Run(headerText);
                        run.FontSize = this.FontSize * 1.5;
                        run.FontWeight = FontWeights.Bold;
                        this.Inlines.Add(run);
                        this.Inlines.Add(new LineBreak());
                    }
                    else if (line.StartsWith("## "))
                    {
                        // Level 2 header
                        var headerText = line.Substring(3);
                        var run = new Run(headerText);
                        run.FontSize = this.FontSize * 1.3;
                        run.FontWeight = FontWeights.Bold;
                        this.Inlines.Add(run);
                        this.Inlines.Add(new LineBreak());
                    }
                    else if (line.StartsWith("### "))
                    {
                        // Level 3 header
                        var headerText = line.Substring(4);
                        var run = new Run(headerText);
                        run.FontSize = this.FontSize * 1.15;
                        run.FontWeight = FontWeights.Bold;
                        this.Inlines.Add(run);
                        this.Inlines.Add(new LineBreak());
                    }
                    else if (Regex.IsMatch(line, @"^\d+\.\s"))
                    {
                        // Ordered list - find all consecutive list items
                        ProcessOrderedList(lines, ref currentLine);
                        continue; // currentLine has been updated in the function
                    }
                    else if (line.StartsWith("- ") || line.StartsWith("* "))
                    {
                        // Unordered list - find all consecutive list items
                        ProcessUnorderedList(lines, ref currentLine);
                        continue; // currentLine has been updated in the function
                    }
                    else if (string.IsNullOrWhiteSpace(line))
                    {
                        // Empty line
                        this.Inlines.Add(new LineBreak());
                    }
                    else
                    {
                        // Regular paragraph
                        ProcessFormattedText(line);
                        this.Inlines.Add(new LineBreak());
                    }

                    currentLine++;
                }
            }
            catch (Exception ex)
            {
                // Fallback to plain text if parsing fails
                this.Inlines.Add(new Run(Markdown));
                System.Diagnostics.Debug.WriteLine($"Markdown parsing error: {ex.Message}");
            }
        }

        private void ProcessOrderedList(string[] lines, ref int currentLine)
        {
            while (currentLine < lines.Length && Regex.IsMatch(lines[currentLine].TrimEnd(), @"^\d+\.\s"))
            {
                string line = lines[currentLine].TrimEnd();
                
                // Find the content after the number and dot
                int contentStart = line.IndexOf(". ") + 2;
                if (contentStart > 1 && contentStart < line.Length)
                {
                    // Add the bullet point
                    string number = line.Substring(0, contentStart - 1); // Get "1."
                    this.Inlines.Add(new Run(number + " ") { FontWeight = FontWeights.Bold });
                    
                    // Add the content with formatting
                    string content = line.Substring(contentStart);
                    ProcessFormattedText(content);
                    this.Inlines.Add(new LineBreak());
                    
                    currentLine++;
                }
                else
                {
                    // Malformed list item, just add the line
                    this.Inlines.Add(new Run(line));
                    this.Inlines.Add(new LineBreak());
                    currentLine++;
                }
            }
        }

        private void ProcessUnorderedList(string[] lines, ref int currentLine)
        {
            while (currentLine < lines.Length && 
                  (lines[currentLine].TrimEnd().StartsWith("- ") || lines[currentLine].TrimEnd().StartsWith("* ")))
            {
                string line = lines[currentLine].TrimEnd();
                
                // Find the content after the bullet
                int contentStart = line.StartsWith("- ") ? 2 : 2; // Both "- " and "* " are 2 chars
                
                if (contentStart < line.Length)
                {
                    // Add the bullet point
                    this.Inlines.Add(new Run("• ") { Foreground = Brushes.DimGray });
                    
                    // Add the content with formatting
                    string content = line.Substring(contentStart);
                    ProcessFormattedText(content);
                    this.Inlines.Add(new LineBreak());
                    
                    currentLine++;
                }
                else
                {
                    // Malformed list item, just add the line
                    this.Inlines.Add(new Run(line));
                    this.Inlines.Add(new LineBreak());
                    currentLine++;
                }
            }
        }

        private void ProcessFormattedText(string text)
        {
            int currentIndex = 0;

            while (currentIndex < text.Length)
            {
                // Look for different formatting markers
                int boldStartDoubleAsterisk = text.IndexOf("**", currentIndex);
                int boldStartDoubleUnderscore = text.IndexOf("__", currentIndex);
                int italicStartSingleAsterisk = text.IndexOf("*", currentIndex);
                if (italicStartSingleAsterisk >= 0 && italicStartSingleAsterisk == boldStartDoubleAsterisk)
                {
                    italicStartSingleAsterisk = text.IndexOf("*", currentIndex + 1);
                }
                int italicStartSingleUnderscore = text.IndexOf("_", currentIndex);
                if (italicStartSingleUnderscore >= 0 && italicStartSingleUnderscore == boldStartDoubleUnderscore)
                {
                    italicStartSingleUnderscore = text.IndexOf("_", currentIndex + 1);
                }

                // Find the earliest marker
                int earliestMarker = int.MaxValue;
                string markerType = "";

                if (boldStartDoubleAsterisk >= 0 && boldStartDoubleAsterisk < earliestMarker)
                {
                    earliestMarker = boldStartDoubleAsterisk;
                    markerType = "**";
                }
                if (boldStartDoubleUnderscore >= 0 && boldStartDoubleUnderscore < earliestMarker)
                {
                    earliestMarker = boldStartDoubleUnderscore;
                    markerType = "__";
                }
                if (italicStartSingleAsterisk >= 0 && italicStartSingleAsterisk < earliestMarker &&
                    text.Substring(italicStartSingleAsterisk, 1) != "**")
                {
                    earliestMarker = italicStartSingleAsterisk;
                    markerType = "*";
                }
                if (italicStartSingleUnderscore >= 0 && italicStartSingleUnderscore < earliestMarker &&
                    text.Substring(italicStartSingleUnderscore, 1) != "__")
                {
                    earliestMarker = italicStartSingleUnderscore;
                    markerType = "_";
                }

                if (earliestMarker == int.MaxValue || markerType == "")
                {
                    // No more formatting markers, add remaining text
                    if (currentIndex < text.Length)
                    {
                        this.Inlines.Add(new Run(text.Substring(currentIndex)));
                    }
                    break;
                }

                // Add text before the marker
                if (earliestMarker > currentIndex)
                {
                    this.Inlines.Add(new Run(text.Substring(currentIndex, earliestMarker - currentIndex)));
                }

                // Find the closing marker
                int endIndex = text.IndexOf(markerType, earliestMarker + markerType.Length);
                if (endIndex == -1)
                {
                    // No closing marker, treat as plain text
                    this.Inlines.Add(new Run(text.Substring(earliestMarker)));
                    break;
                }

                // Get the text between markers
                string formattedText = text.Substring(earliestMarker + markerType.Length, endIndex - earliestMarker - markerType.Length);

                // Add the formatted text with appropriate style
                var run = new Run(formattedText);
                if (markerType == "**" || markerType == "__")
                {
                    run.FontWeight = FontWeights.Bold;
                }
                else if (markerType == "*" || markerType == "_")
                {
                    run.FontStyle = FontStyles.Italic;
                }
                this.Inlines.Add(run);

                // Move to after the closing marker
                currentIndex = endIndex + markerType.Length;
            }
        }
    }
} 