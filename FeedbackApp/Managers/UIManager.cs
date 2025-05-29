using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FeedbackApp.Models;
using FeedbackApp.Services;

namespace FeedbackApp.Managers
{
    /// <summary>
    /// Manages UI state and operations for the main window
    /// </summary>
    public class UIManager
    {
        private readonly WrapPanel _imagesPanel;
        private readonly Border _noImagesPlaceholder;
        private readonly Expander _imagesExpander;
        private readonly TextBlock _imageCountText;
        private readonly TextBlock _scrollIndicator;
        private readonly TextBox _feedbackTextBox;
        private readonly Window _mainWindow;

        public UIManager(
            WrapPanel imagesPanel,
            Border noImagesPlaceholder,
            Expander imagesExpander,
            TextBlock imageCountText,
            TextBlock scrollIndicator,
            TextBox feedbackTextBox,
            Window mainWindow)
        {
            _imagesPanel = imagesPanel ?? throw new ArgumentNullException(nameof(imagesPanel));
            _noImagesPlaceholder = noImagesPlaceholder ?? throw new ArgumentNullException(nameof(noImagesPlaceholder));
            _imagesExpander = imagesExpander ?? throw new ArgumentNullException(nameof(imagesExpander));
            _imageCountText = imageCountText ?? throw new ArgumentNullException(nameof(imageCountText));
            _scrollIndicator = scrollIndicator ?? throw new ArgumentNullException(nameof(scrollIndicator));
            _feedbackTextBox = feedbackTextBox ?? throw new ArgumentNullException(nameof(feedbackTextBox));
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
        }

        /// <summary>
        /// Updates the image count display
        /// </summary>
        public void UpdateImageCount(int currentCount, int maxCount)
        {
            _imageCountText.Text = $"{currentCount}/{maxCount} Images";
            
            if (currentCount > 0)
            {
                _imagesExpander.Header = $"Images ({currentCount}/{maxCount})";
            }
            else
            {
                _imagesExpander.Header = "Images";
            }
        }

        /// <summary>
        /// Adds an image to the UI panel
        /// </summary>
        public void AddImageToPanel(ImageItemModel imageItem, Action<ImageItemModel> onRemoveClick)
        {
            try
            {
                // Expand the images section if it's not already expanded
                if (!_imagesExpander.IsExpanded)
                {
                    _imagesExpander.IsExpanded = true;
                }

                var container = CreateImageContainer(imageItem, onRemoveClick);
                
                // Store the container in the image item for later removal
                imageItem.UiElement = container;
                
                // Add the container to the images panel
                _imagesPanel.Children.Add(container);
                
                // Update window size to accommodate new content
                _mainWindow.SizeToContent = SizeToContent.Height;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Removes an image from the UI panel
        /// </summary>
        public void RemoveImageFromPanel(ImageItemModel imageItem)
        {
            if (imageItem.UiElement != null)
            {
                _imagesPanel.Children.Remove(imageItem.UiElement);
                imageItem.UiElement = null;
            }
        }

        /// <summary>
        /// Updates the visibility of the no images placeholder
        /// </summary>
        public void UpdateNoImagesPlaceholder(bool hasImages)
        {
            _noImagesPlaceholder.Visibility = hasImages ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// Updates the scroll indicator based on scroll position
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
        /// Triggers window size recalculation
        /// </summary>
        public void RecalculateWindowSize()
        {
            _mainWindow.SizeToContent = SizeToContent.Height;
        }

        /// <summary>
        /// Creates a container for an image with remove button
        /// </summary>
        private Grid CreateImageContainer(ImageItemModel imageItem, Action<ImageItemModel> onRemoveClick)
        {
            var container = new Grid 
            { 
                Width = 150, 
                Height = 180, 
                Margin = new Thickness(5) 
            };
            
            // Add rows to the grid
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });
            container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            
            // Create image display
            var imageBorder = CreateImageBorder(imageItem);
            Grid.SetRow(imageBorder, 0);
            
            // Create remove button
            var removeButton = CreateRemoveButton(imageItem, onRemoveClick);
            Grid.SetRow(removeButton, 1);
            
            // Add elements to the container
            container.Children.Add(imageBorder);
            container.Children.Add(removeButton);
            
            return container;
        }

        /// <summary>
        /// Creates the border and image display for an image item
        /// </summary>
        private Border CreateImageBorder(ImageItemModel imageItem)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204))
            };
            
            var image = new Image
            {
                Stretch = Stretch.Uniform,
                Margin = new Thickness(5)
            };
            
            // Set RenderOptions for quality
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            
            // Load the image
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(imageItem.FilePath);
            bitmap.EndInit();
            
            image.Source = bitmap;
            border.Child = image;
            
            return border;
        }

        /// <summary>
        /// Creates the remove button for an image item
        /// </summary>
        private Button CreateRemoveButton(ImageItemModel imageItem, Action<ImageItemModel> onRemoveClick)
        {
            var removeButton = new Button
            {
                Content = "Remove",
                Padding = new Thickness(5, 2, 5, 2),
                Margin = new Thickness(5, 2, 5, 2),
                Tag = imageItem
            };
            
            removeButton.Click += (sender, e) => onRemoveClick(imageItem);
            
            return removeButton;
        }
    }
}
