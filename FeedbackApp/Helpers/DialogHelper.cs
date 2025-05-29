using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FeedbackApp.Helpers
{
    /// <summary>
    /// Helper class for creating common dialog elements and styles
    /// </summary>
    public static class DialogHelper
    {
        /// <summary>
        /// Creates a standard dialog window with common properties
        /// </summary>
        public static Window CreateStandardDialog(string title, double width, double height, Window? owner = null)
        {
            return new Window
            {
                Title = title,
                Width = width,
                Height = height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = owner,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };
        }

        /// <summary>
        /// Creates a main border for dialog content
        /// </summary>
        public static Border CreateMainBorder()
        {
            return new Border
            {
                Margin = new Thickness(15),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1)
            };
        }

        /// <summary>
        /// Creates a labeled text input with border
        /// </summary>
        public static (TextBlock label, Border border, TextBox textBox) CreateLabeledTextInput(
            string labelText, 
            string? initialValue = null, 
            bool multiline = false,
            string? placeholder = null)
        {
            var label = new TextBlock
            {
                Text = labelText,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Medium
            };

            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.White
            };

            var textBox = new TextBox
            {
                Text = initialValue ?? string.Empty,
                Margin = new Thickness(8),
                VerticalAlignment = multiline ? VerticalAlignment.Stretch : VerticalAlignment.Center,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                AcceptsReturn = multiline,
                TextWrapping = multiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                VerticalScrollBarVisibility = multiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Disabled
            };

            if (multiline)
            {
                textBox.Padding = new Thickness(3);
            }

            // Add placeholder functionality if specified
            if (!string.IsNullOrEmpty(placeholder))
            {
                AddPlaceholder(border, textBox, placeholder);
            }
            else
            {
                border.Child = textBox;
            }

            return (label, border, textBox);
        }

        /// <summary>
        /// Creates a button panel with standard layout
        /// </summary>
        public static StackPanel CreateButtonPanel()
        {
            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
        }

        /// <summary>
        /// Creates a standard button with the specified style
        /// </summary>
        public static Button CreateStandardButton(
            string content, 
            double width, 
            double height, 
            Style? style = null,
            bool isDefault = false,
            bool isCancel = false)
        {
            return new Button
            {
                Content = content,
                Width = width,
                Height = height,
                IsDefault = isDefault,
                IsCancel = isCancel,
                Style = style,
                Margin = new Thickness(5, 0, 0, 0)
            };
        }

        /// <summary>
        /// Adds placeholder functionality to a text input
        /// </summary>
        private static void AddPlaceholder(Border border, TextBox textBox, string placeholder)
        {
            var grid = new Grid();
            
            var placeholderText = new TextBlock
            {
                Text = placeholder,
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            grid.Children.Add(textBox);
            grid.Children.Add(placeholderText);

            // Set up binding to hide placeholder when text is entered
            textBox.TextChanged += (s, args) =>
            {
                placeholderText.Visibility = string.IsNullOrEmpty(textBox.Text) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            };

            border.Child = grid;
        }

        /// <summary>
        /// Shows a validation error message
        /// </summary>
        public static void ShowValidationError(string message, string title = "Validation Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Shows a confirmation dialog
        /// </summary>
        public static bool ShowConfirmation(string message, string title = "Confirm")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Shows an error message
        /// </summary>
        public static void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
