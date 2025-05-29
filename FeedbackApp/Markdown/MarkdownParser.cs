using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace FeedbackApp.Markdown
{
    /// <summary>
    /// Handles parsing of markdown text into WPF inline elements
    /// </summary>
    public static class MarkdownParser
    {
        /// <summary>
        /// Parses markdown text and adds inline elements to the provided collection
        /// </summary>
        public static void ParseMarkdown(string markdown, InlineCollection inlines, double baseFontSize)
        {
            if (string.IsNullOrEmpty(markdown) || inlines == null)
                return;

            try
            {
                string[] lines = markdown.Split('\n');
                int currentLine = 0;

                while (currentLine < lines.Length)
                {
                    string line = lines[currentLine].TrimEnd();

                    if (TryProcessHeader(line, inlines, baseFontSize))
                    {
                        // Header processed
                    }
                    else if (Regex.IsMatch(line, @"^\d+\.\s"))
                    {
                        // Ordered list - find all consecutive list items
                        ProcessOrderedList(lines, ref currentLine, inlines);
                        continue; // currentLine has been updated in the function
                    }
                    else if (line.StartsWith("- ") || line.StartsWith("* "))
                    {
                        // Unordered list - find all consecutive list items
                        ProcessUnorderedList(lines, ref currentLine, inlines);
                        continue; // currentLine has been updated in the function
                    }
                    else if (string.IsNullOrWhiteSpace(line))
                    {
                        // Empty line
                        inlines.Add(new LineBreak());
                    }
                    else
                    {
                        // Regular paragraph
                        ProcessFormattedText(line, inlines);
                        inlines.Add(new LineBreak());
                    }

                    currentLine++;
                }
            }
            catch (Exception ex)
            {
                // Fallback to plain text if parsing fails
                inlines.Add(new Run(markdown));
                System.Diagnostics.Debug.WriteLine($"Markdown parsing error: {ex.Message}");
            }
        }

        /// <summary>
        /// Tries to process a line as a header
        /// </summary>
        private static bool TryProcessHeader(string line, InlineCollection inlines, double baseFontSize)
        {
            if (line.StartsWith("# "))
            {
                // Level 1 header
                var headerText = line.Substring(2);
                var run = new Run(headerText)
                {
                    FontSize = baseFontSize * 1.5,
                    FontWeight = FontWeights.Bold
                };
                inlines.Add(run);
                inlines.Add(new LineBreak());
                return true;
            }
            else if (line.StartsWith("## "))
            {
                // Level 2 header
                var headerText = line.Substring(3);
                var run = new Run(headerText)
                {
                    FontSize = baseFontSize * 1.3,
                    FontWeight = FontWeights.Bold
                };
                inlines.Add(run);
                inlines.Add(new LineBreak());
                return true;
            }
            else if (line.StartsWith("### "))
            {
                // Level 3 header
                var headerText = line.Substring(4);
                var run = new Run(headerText)
                {
                    FontSize = baseFontSize * 1.15,
                    FontWeight = FontWeights.Bold
                };
                inlines.Add(run);
                inlines.Add(new LineBreak());
                return true;
            }

            return false;
        }

        /// <summary>
        /// Processes ordered list items
        /// </summary>
        private static void ProcessOrderedList(string[] lines, ref int currentLine, InlineCollection inlines)
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
                    inlines.Add(new Run(number + " ") { FontWeight = FontWeights.Bold });
                    
                    // Add the content with formatting
                    string content = line.Substring(contentStart);
                    ProcessFormattedText(content, inlines);
                    inlines.Add(new LineBreak());
                    
                    currentLine++;
                }
                else
                {
                    // Malformed list item, just add the line
                    inlines.Add(new Run(line));
                    inlines.Add(new LineBreak());
                    currentLine++;
                }
            }
        }

        /// <summary>
        /// Processes unordered list items
        /// </summary>
        private static void ProcessUnorderedList(string[] lines, ref int currentLine, InlineCollection inlines)
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
                    inlines.Add(new Run("• ") { Foreground = Brushes.DimGray });
                    
                    // Add the content with formatting
                    string content = line.Substring(contentStart);
                    ProcessFormattedText(content, inlines);
                    inlines.Add(new LineBreak());
                    
                    currentLine++;
                }
                else
                {
                    // Malformed list item, just add the line
                    inlines.Add(new Run(line));
                    inlines.Add(new LineBreak());
                    currentLine++;
                }
            }
        }

        /// <summary>
        /// Processes text with inline formatting (bold, italic)
        /// </summary>
        private static void ProcessFormattedText(string text, InlineCollection inlines)
        {
            int currentIndex = 0;

            while (currentIndex < text.Length)
            {
                var (markerIndex, markerType) = FindEarliestMarker(text, currentIndex);

                if (markerIndex == -1)
                {
                    // No more formatting markers, add remaining text
                    if (currentIndex < text.Length)
                    {
                        inlines.Add(new Run(text.Substring(currentIndex)));
                    }
                    break;
                }

                // Add text before the marker
                if (markerIndex > currentIndex)
                {
                    inlines.Add(new Run(text.Substring(currentIndex, markerIndex - currentIndex)));
                }

                // Find the closing marker
                int endIndex = text.IndexOf(markerType, markerIndex + markerType.Length);
                if (endIndex == -1)
                {
                    // No closing marker, treat as plain text
                    inlines.Add(new Run(text.Substring(markerIndex)));
                    break;
                }

                // Get the text between markers
                string formattedText = text.Substring(markerIndex + markerType.Length, endIndex - markerIndex - markerType.Length);

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
                inlines.Add(run);

                // Move to after the closing marker
                currentIndex = endIndex + markerType.Length;
            }
        }

        /// <summary>
        /// Finds the earliest formatting marker in the text
        /// </summary>
        private static (int index, string markerType) FindEarliestMarker(string text, int startIndex)
        {
            int boldStartDoubleAsterisk = text.IndexOf("**", startIndex);
            int boldStartDoubleUnderscore = text.IndexOf("__", startIndex);
            int italicStartSingleAsterisk = text.IndexOf("*", startIndex);
            if (italicStartSingleAsterisk >= 0 && italicStartSingleAsterisk == boldStartDoubleAsterisk)
            {
                italicStartSingleAsterisk = text.IndexOf("*", startIndex + 1);
            }
            int italicStartSingleUnderscore = text.IndexOf("_", startIndex);
            if (italicStartSingleUnderscore >= 0 && italicStartSingleUnderscore == boldStartDoubleUnderscore)
            {
                italicStartSingleUnderscore = text.IndexOf("_", startIndex + 1);
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
            if (italicStartSingleAsterisk >= 0 && italicStartSingleAsterisk < earliestMarker)
            {
                earliestMarker = italicStartSingleAsterisk;
                markerType = "*";
            }
            if (italicStartSingleUnderscore >= 0 && italicStartSingleUnderscore < earliestMarker)
            {
                earliestMarker = italicStartSingleUnderscore;
                markerType = "_";
            }

            return earliestMarker == int.MaxValue ? (-1, "") : (earliestMarker, markerType);
        }
    }
}
