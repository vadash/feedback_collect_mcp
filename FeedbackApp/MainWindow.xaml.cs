using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;

namespace FeedbackApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _windowTitle = "AI Feedback Collection";
        private string _promptText = "Please provide your feedback or describe your issue:";
        private string? _outputFilePath;
        private bool _isSubmitSuccess = false;
        private string? _feedbackText;
        
        // New properties for multiple images
        private List<ImageItem> _images = new List<ImageItem>();
        private const int MaxImages = 5;
        // Directory for temp images
        private string _tempImageDirectory;
        
        // Added for text changes
        private System.Windows.Threading.DispatcherTimer? _textChangedTimer;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }

        public string PromptText
        {
            get => _promptText;
            set
            {
                _promptText = value;
                OnPropertyChanged(nameof(PromptText));
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            
            // Create temp directory for clipboard images
            _tempImageDirectory = Path.Combine(Path.GetTempPath(), "FeedbackApp_Images");
            Directory.CreateDirectory(_tempImageDirectory);
            
            // Process command line arguments
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                WindowTitle = args[1];
            }
            if (args.Length >= 3)
            {
                PromptText = args[2];
            }
            if (args.Length >= 4)
            {
                _outputFilePath = args[3];
            }

            // Handle Closing event to save feedback when window is closed
            this.Closing += MainWindow_Closing;
            
            // Initialize the image count display
            UpdateImageCount();
            
            // Add event handler for text changes to recalculate window size
            FeedbackTextBox.TextChanged += FeedbackTextBox_TextChanged;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // Only save feedback if Submit button was clicked (not if Cancel was clicked)
            if (_isSubmitSuccess)
            {
                SaveFeedback();
            }
            
            // Clean up temp images
            TryCleanupTempDirectory();
        }
        
        // Clean up temporary image directory
        private void TryCleanupTempDirectory()
        {
            try
            {
                if (Directory.Exists(_tempImageDirectory))
                {
                    // Only delete files that are not in our current images list
                    var currentImagePaths = new HashSet<string>();
                    foreach (var img in _images)
                    {
                        currentImagePaths.Add(img.FilePath);
                    }
                    
                    // Get all files in the temp directory
                    string[] files = Directory.GetFiles(_tempImageDirectory);
                    foreach (string file in files)
                    {
                        // Only delete files that are not in our current list
                        if (!currentImagePaths.Contains(file))
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to delete temp file {file}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during temp directory cleanup: {ex.Message}");
            }
        }

        // Handle keyboard events for the FeedbackTextBox
        private void FeedbackTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check for Ctrl+V (paste)
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Try to extract image from clipboard
                if (TryGetImageFromClipboard(out BitmapSource? image))
                {
                    // Check if we've reached the maximum number of images
                    if (_images.Count >= MaxImages)
                    {
                        MessageBox.Show($"You can attach a maximum of {MaxImages} images.", "Maximum Reached", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                            
                        // Don't suppress the default paste behavior for text
                        return;
                    }
                    
                    // We have a valid image, save it to a temporary file
                    string imageFilePath = SaveClipboardImageToFile(image);
                    string imageType = "image/png"; // Default to PNG for clipboard images
                    
                    // Add the image to our collection
                    var imageItem = new ImageItem
                    {
                        FilePath = imageFilePath,
                        MimeType = imageType
                    };
                    
                    // Add to our list
                    _images.Add(imageItem);
                    
                    // Add to UI
                    AddImageToPanel(imageItem);
                    
                    // Update image count
                    UpdateImageCount();
                    
                    // Hide placeholder if needed
                    if (_images.Count > 0)
                    {
                        NoImagesPlaceholder.Visibility = Visibility.Collapsed;
                    }
                    
                    // Continue with normal paste for any text
                    return;
                }
            }
        }
        
        // Try to get an image from the clipboard
        private bool TryGetImageFromClipboard(out BitmapSource? image)
        {
            image = null;
            
            try
            {
                // Check if clipboard contains an image
                if (Clipboard.ContainsImage())
                {
                    // Get the image from clipboard
                    image = Clipboard.GetImage();
                    return image != null;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting image from clipboard: {ex.Message}");
                return false;
            }
        }
        
        // Save clipboard image to a temporary file and return the file path
        private string SaveClipboardImageToFile(BitmapSource image)
        {
            string fileName = $"clipboard_image_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
            string filePath = Path.Combine(_tempImageDirectory, fileName);
            
            try
            {
                using (FileStream stream = new FileStream(filePath, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(stream);
                }
                
                return filePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving clipboard image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Rethrow to handle in the calling method
            }
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if we've reached the maximum number of images
            if (_images.Count >= MaxImages)
            {
                MessageBox.Show($"You can attach a maximum of {MaxImages} images.", "Maximum Reached", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Image",
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var imagePath = openFileDialog.FileName;
                    var imageType = DetermineImageType(imagePath);
                    
                    // Create a new image item
                    var imageItem = new ImageItem
                    {
                        FilePath = imagePath,
                        MimeType = imageType
                    };
                    
                    // Add to our list
                    _images.Add(imageItem);
                    
                    // Add to the UI
                    AddImageToPanel(imageItem);
                    
                    // Update image count
                    UpdateImageCount();
                    
                    // Hide the placeholder if this is the first image
                    if (_images.Count > 0)
                    {
                        NoImagesPlaceholder.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddImageToPanel(ImageItem imageItem)
        {
            try
            {
                // Expand the images section if it's not already expanded
                if (!ImagesExpander.IsExpanded)
                {
                    ImagesExpander.IsExpanded = true;
                }
                
                // Create a container for the image and its remove button
                var container = new Grid { Width = 150, Height = 180, Margin = new Thickness(5) };
                
                // Add rows to the grid
                container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });
                container.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
                
                // Create a border for the image
                var border = new Border
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204))
                };
                Grid.SetRow(border, 0);
                
                // Create the image element
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
                
                // Create a remove button
                var removeButton = new Button
                {
                    Content = "Remove",
                    Padding = new Thickness(5, 2, 5, 2),
                    Margin = new Thickness(5, 2, 5, 2),
                    Tag = imageItem
                };
                removeButton.Click += RemoveImage_Click;
                Grid.SetRow(removeButton, 1);
                
                // Add elements to the container
                container.Children.Add(border);
                container.Children.Add(removeButton);
                
                // Store the container in the image item for later removal
                imageItem.UiElement = container;
                
                // Add the container to the images panel
                ImagesPanel.Children.Add(container);
                
                // Update window size to accommodate new content
                this.SizeToContent = SizeToContent.Height;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is ImageItem imageItem)
                {
                    // Remove from UI
                    if (imageItem.UiElement != null)
                    {
                        ImagesPanel.Children.Remove(imageItem.UiElement);
                    }
                    
                    // Remove from our list
                    _images.Remove(imageItem);
                    
                    // Update image count
                    UpdateImageCount();
                    
                    // Show placeholder if no images left
                    if (_images.Count == 0)
                    {
                        NoImagesPlaceholder.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateImageCount()
        {
            // Update the image count text
            ImageCountText.Text = $"{_images.Count}/{MaxImages} Images";
            
            // Also update the Expander header to show the count
            if (_images.Count > 0)
            {
                ImagesExpander.Header = $"Images ({_images.Count}/{MaxImages})";
            }
            else
            {
                ImagesExpander.Header = "Images";
            }
        }

        private string DetermineImageType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".bmp" => "image/bmp",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text))
                {
                    MessageBox.Show("Please enter your feedback before submitting.", "Required Field", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Store feedback data to be saved when window closes
                _feedbackText = FeedbackTextBox.Text;
                _isSubmitSuccess = true;
                
                // Close the window - feedback will be saved in the Closing event handler
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing feedback: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _isSubmitSuccess = false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _isSubmitSuccess = false;
            Close();
        }

        private void SaveFeedback()
        {
            try
            {
                // Create a list of image objects for the JSON
                var imagesList = new List<object>();
                foreach (var img in _images)
                {
                    imagesList.Add(new
                    {
                        path = img.FilePath,
                        type = img.MimeType
                    });
                }
                
                // Create the feedback data object
                var feedbackData = new
                {
                    text = _feedbackText,
                    hasImages = _images.Count > 0,
                    imageCount = _images.Count,
                    images = imagesList,
                    timestamp = DateTime.Now.ToString("o")
                };

                // Serialize to JSON
                string jsonData = JsonSerializer.Serialize(feedbackData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Determine where to save the feedback data
                string outputPath;
                if (string.IsNullOrWhiteSpace(_outputFilePath))
                {
                    // If no output path was provided, use the temp directory
                    outputPath = Path.Combine(
                        Path.GetTempPath(), $"feedback_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                }
                else
                {
                    outputPath = _outputFilePath;
                }

                // Ensure directory exists if the path contains a directory
                string? dirName = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                
                // Save the feedback data
                File.WriteAllText(outputPath, jsonData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving feedback: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Handle drag-over event for the FeedbackTextBox
        private void FeedbackTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Allow only if contains files
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif")
                        {
                            e.Effects = DragDropEffects.Copy;
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
            
            // Default for text or other content
            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }
        
        // Handle drop event for the FeedbackTextBox
        private void FeedbackTextBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    // Filter for image files
                    List<string> imageFiles = new List<string>();
                    foreach (string file in files)
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif")
                        {
                            imageFiles.Add(file);
                        }
                    }
                    
                    // Process the valid image files
                    if (imageFiles.Count > 0)
                    {
                        // Check if we can add all or just some
                        int availableSlots = MaxImages - _images.Count;
                        int imagesToAdd = Math.Min(availableSlots, imageFiles.Count);
                        
                        if (availableSlots <= 0)
                        {
                            MessageBox.Show($"You can attach a maximum of {MaxImages} images. No more images can be added.", 
                                "Maximum Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        
                        // Add images up to the maximum
                        for (int i = 0; i < imagesToAdd; i++)
                        {
                            try
                            {
                                string filePath = imageFiles[i];
                                string mimeType = DetermineImageType(filePath);
                                
                                // Create a new image item
                                var imageItem = new ImageItem
                                {
                                    FilePath = filePath,
                                    MimeType = mimeType
                                };
                                
                                // Add to our list
                                _images.Add(imageItem);
                                
                                // Add to the UI
                                AddImageToPanel(imageItem);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error adding image: {ex.Message}", "Error", 
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        
                        // Update image count
                        UpdateImageCount();
                        
                        // Hide the placeholder if needed
                        if (_images.Count > 0)
                        {
                            NoImagesPlaceholder.Visibility = Visibility.Collapsed;
                        }
                        
                        e.Handled = true;
                    }
                }
            }
        }

        // Event handler for when the Images expander is expanded
        private void ImagesExpander_Expanded(object sender, RoutedEventArgs e)
        {
            // Trigger SizeToContent to recalculate window height
            this.SizeToContent = SizeToContent.Height;
            
            // Animate the window size change after a slight delay
            // This gives time for the expander to complete its animation
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += (s, args) =>
            {
                // Recalculate size again after content has fully expanded
                this.SizeToContent = SizeToContent.Height;
                
                // Stop the timer after one tick
                ((System.Windows.Threading.DispatcherTimer)s).Stop();
            };
            timer.Interval = TimeSpan.FromMilliseconds(300);
            timer.Start();
        }
        
        // Event handler for when the Images expander is collapsed
        private void ImagesExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            // Trigger SizeToContent to recalculate window height
            this.SizeToContent = SizeToContent.Height;
        }

        // Handle scroll changes in the TextBox
        private void FeedbackTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Show scroll indicator if there's vertical scrolling available
            if (e.ExtentHeight > e.ViewportHeight)
            {
                ScrollIndicator.Visibility = Visibility.Visible;
                
                // Show bottom arrow only if not at the bottom
                if (Math.Abs(e.VerticalOffset + e.ViewportHeight - e.ExtentHeight) < 0.5)
                {
                    ScrollIndicator.Text = "⬆ Scroll for more ⬆";
                }
                else if (e.VerticalOffset < 0.5)
                {
                    ScrollIndicator.Text = "⬇ Scroll for more ⬇";
                }
                else
                {
                    ScrollIndicator.Text = "⬆⬇ Scroll for more ⬆⬇";
                }
            }
            else
            {
                ScrollIndicator.Visibility = Visibility.Collapsed;
            }
        }
        
        // Handler for text changes in the feedback textbox
        private void FeedbackTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Check if scrolling is needed and update the scroll indicator
            UpdateScrollIndicatorVisibility();
            
            // Use a timer to avoid excessive resizing on rapid typing
            if (_textChangedTimer == null)
            {
                _textChangedTimer = new System.Windows.Threading.DispatcherTimer();
                _textChangedTimer.Interval = TimeSpan.FromMilliseconds(500);
                _textChangedTimer.Tick += (s, args) =>
                {
                    this.SizeToContent = SizeToContent.Height;
                    _textChangedTimer.Stop();
                    
                    // Re-check scrolling after resize
                    UpdateScrollIndicatorVisibility();
                };
            }
            
            // Reset and restart the timer when text changes
            _textChangedTimer.Stop();
            _textChangedTimer.Start();
        }
        
        // Helper method to update the scroll indicator visibility
        private void UpdateScrollIndicatorVisibility()
        {
            // Get the ScrollViewer from the TextBox
            var scrollViewer = GetScrollViewer(FeedbackTextBox);
            if (scrollViewer != null)
            {
                if (scrollViewer.ExtentHeight > scrollViewer.ViewportHeight)
                {
                    ScrollIndicator.Visibility = Visibility.Visible;
                    
                    // Determine scroll direction indicators
                    if (Math.Abs(scrollViewer.VerticalOffset + scrollViewer.ViewportHeight - scrollViewer.ExtentHeight) < 0.5)
                    {
                        ScrollIndicator.Text = "⬆ Scroll for more ⬆";
                    }
                    else if (scrollViewer.VerticalOffset < 0.5)
                    {
                        ScrollIndicator.Text = "⬇ Scroll for more ⬇";
                    }
                    else
                    {
                        ScrollIndicator.Text = "⬆⬇ Scroll for more ⬆⬇";
                    }
                }
                else
                {
                    ScrollIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }
        
        // Helper method to get the ScrollViewer of a TextBox
        private ScrollViewer GetScrollViewer(TextBox textBox)
        {
            if (VisualTreeHelper.GetChildrenCount(textBox) == 0)
            {
                return null;
            }
            
            // Try to find the ScrollViewer
            DependencyObject firstChild = VisualTreeHelper.GetChild(textBox, 0);
            if (firstChild is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }
            
            // If not found directly, try recursively
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(firstChild); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(firstChild, i);
                if (child is ScrollViewer sv)
                {
                    return sv;
                }
                
                // Check one more level deep
                for (int j = 0; j < VisualTreeHelper.GetChildrenCount(child); j++)
                {
                    DependencyObject grandchild = VisualTreeHelper.GetChild(child, j);
                    if (grandchild is ScrollViewer gsv)
                    {
                        return gsv;
                    }
                }
            }
            
            return null;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Class to store information about an image
    public class ImageItem
    {
        public string FilePath { get; set; } = "";
        public string MimeType { get; set; } = "";
        public UIElement? UiElement { get; set; }
    }
} 