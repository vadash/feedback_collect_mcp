using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FeedbackApp.Handlers
{
    /// <summary>
    /// Handles scroll indicator logic and visual tree operations
    /// </summary>
    public class ScrollIndicatorHandler
    {
        private readonly TextBlock _scrollIndicator;

        public ScrollIndicatorHandler(TextBlock scrollIndicator)
        {
            _scrollIndicator = scrollIndicator ?? throw new ArgumentNullException(nameof(scrollIndicator));
        }

        /// <summary>
        /// Updates the scroll indicator visibility and text based on scroll position
        /// </summary>
        public void UpdateScrollIndicator(double verticalOffset, double viewportHeight, double extentHeight)
        {
            if (extentHeight > viewportHeight)
            {
                _scrollIndicator.Visibility = Visibility.Visible;
                
                // Determine text based on scroll position
                if (Math.Abs(verticalOffset + viewportHeight - extentHeight) < 0.5) // At the bottom
                {
                    _scrollIndicator.Text = "⬆ Scroll for more ⬆";
                }
                else if (verticalOffset < 0.5) // At the top
                {
                    _scrollIndicator.Text = "⬇ Scroll for more ⬇";
                }
                else // Somewhere in the middle
                {
                    _scrollIndicator.Text = "⬆⬇ Scroll for more ⬆⬇";
                }
            }
            else
            {
                _scrollIndicator.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Updates the scroll indicator visibility for a specific TextBox
        /// </summary>
        public void UpdateScrollIndicatorForTextBox(TextBox textBox)
        {
            if (textBox == null || _scrollIndicator == null) return; // Guard clause

            try
            {
                var scrollViewer = GetScrollViewerForTextBox(textBox);
                if (scrollViewer != null) 
                {
                    // Check if the ScrollViewer itself is visible; otherwise, its scroll properties might not be relevant
                    if (scrollViewer.IsVisible && scrollViewer.ExtentHeight > scrollViewer.ViewportHeight)
                    {
                        _scrollIndicator.Visibility = Visibility.Visible;
                        
                        if (Math.Abs(scrollViewer.VerticalOffset + scrollViewer.ViewportHeight - scrollViewer.ExtentHeight) < 0.5)
                        {
                            _scrollIndicator.Text = "⬆ Scroll for more ⬆";
                        }
                        else if (scrollViewer.VerticalOffset < 0.5)
                        {
                            _scrollIndicator.Text = "⬇ Scroll for more ⬇";
                        }
                        else
                        {
                            _scrollIndicator.Text = "⬆⬇ Scroll for more ⬆⬇";
                        }
                    }
                    else
                    {
                        _scrollIndicator.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    _scrollIndicator.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // Log this or handle; a crash here means something is very wrong with UI state
                Console.WriteLine($"Error in UpdateScrollIndicatorForTextBox: {ex.Message}");
                if (_scrollIndicator != null) // Check if ScrollIndicator is null before accessing it
                {
                    _scrollIndicator.Visibility = Visibility.Collapsed; // Try to fail safe
                }
            }
        }
        
        /// <summary>
        /// Gets the ScrollViewer associated with a TextBox
        /// </summary>
        private ScrollViewer? GetScrollViewerForTextBox(TextBox textBox)
        {
            if (textBox == null) return null;

            // Applying the template ensures parts like PART_ContentHost are loaded.
            textBox.ApplyTemplate(); 
            
            var partContentHost = textBox.Template?.FindName("PART_ContentHost", textBox) as ScrollViewer;
            if (partContentHost != null)
            {
                return partContentHost;
            }

            // Fallback: if PART_ContentHost isn't found (e.g., custom template),
            // recursively search the visual tree of the TextBox.
            return FindVisualChildRecursive<ScrollViewer>(textBox);
        }

        /// <summary>
        /// Generic recursive helper to find a visual child of a specific type
        /// </summary>
        private static T? FindVisualChildRecursive<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }
                
                T? childOfChild = FindVisualChildRecursive<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }
    }
}
