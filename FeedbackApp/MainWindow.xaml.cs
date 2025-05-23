using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using System.Windows.Data;

namespace FeedbackApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string _windowTitle = "AI Feedback Collection";
        private string _promptText = "Please provide your feedback or describe your issue:";
        private string? _outputFilePath;
        private bool _isSubmitSuccess = false;
        private string? _feedbackText;
        private string _actionType = "submit"; // Action types: "submit", "approve", "reject", "ai_decide"
        
        // New properties for multiple images
        private List<ImageItem> _images = new List<ImageItem>();
        private const int MaxImages = 5;
        // Directory for temp images
        private string _tempImageDirectory;
        
        // Added for text changes
        private System.Windows.Threading.DispatcherTimer? _textChangedTimer;
        
        // Auto-close timer properties
        private DispatcherTimer? _autoCloseTimer;
        private const int AutoCloseTimeoutSeconds = 15; // Close after 15 seconds of inactivity
        private const string DefaultFeedbackMessage = "User did not provide feedback";
        private int _remainingSeconds = AutoCloseTimeoutSeconds;
        private DispatcherTimer? _countdownTimer;
        private bool _isAutoClosePaused = false; // State for pause/resume

        // Snippets collection
        private ObservableCollection<Snippet> _snippets = new ObservableCollection<Snippet>();
        private string _snippetsFilePath;

        public ObservableCollection<Snippet> Snippets
        {
            get => _snippets;
            set
            {
                _snippets = value;
                OnPropertyChanged(nameof(Snippets));
            }
        }

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
            
            // Set up snippets file path
            string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FeedbackApp");
            Directory.CreateDirectory(appDataPath);
            _snippetsFilePath = Path.Combine(appDataPath, "snippets.json");
            
            // Load snippets
            LoadSnippets();
            
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
            
            // Start the auto-close timer
            StartAutoCloseTimer();
            
            // Start the countdown timer
            StartCountdownTimer();
            
            // Add event handlers to reset timer on user interaction
            this.PreviewMouseMove += ResetAutoCloseTimer;
            this.PreviewKeyDown += ResetAutoCloseTimer;
            this.Activated += ResetAutoCloseTimer;
        }

        // Load snippets from file
        private void LoadSnippets()
        {
            try
            {
                if (File.Exists(_snippetsFilePath))
                {
                    string json = File.ReadAllText(_snippetsFilePath);
                    var loadedSnippets = JsonSerializer.Deserialize<List<Snippet>>(json);
                    if (loadedSnippets != null)
                    {
                        Snippets.Clear();
                        foreach (var snippet in loadedSnippets)
                        {
                            Snippets.Add(snippet);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading snippets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Save snippets to file
        private void SaveSnippets()
        {
            try
            {
                string json = JsonSerializer.Serialize(Snippets, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_snippetsFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving snippets: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Handle snippet selection
        private void SnippetsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SnippetsComboBox.SelectedItem is Snippet selectedSnippet)
            {
                string currentText = FeedbackTextBox.Text;
                string snippetContent = selectedSnippet.Content;

                if (!string.IsNullOrEmpty(currentText) && !currentText.EndsWith(Environment.NewLine))
                {
                    FeedbackTextBox.AppendText(Environment.NewLine);
                }
                FeedbackTextBox.AppendText(snippetContent);
                FeedbackTextBox.CaretIndex = FeedbackTextBox.Text.Length;
                FeedbackTextBox.ScrollToEnd();
                
                // Reset the combo box selection
                SnippetsComboBox.SelectedIndex = -1;
                
                // Set focus back to the text box
                FeedbackTextBox.Focus();
            }
        }

        // Handle add new snippet button click
        private void AddSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop the auto-close timer while dialog is open
            if (_autoCloseTimer != null)
            {
                _autoCloseTimer.Stop();
            }
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
            }
            
            // Create a new dialog window to add a snippet
            var dialog = new Window
            {
                Title = "Add New Snippet",
                Width = 450,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            // Create the content
            var mainBorder = new Border
            {
                Margin = new Thickness(15),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1)
            };
            
            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // Spacer
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Title label and textbox
            var titleLabel = new TextBlock { 
                Text = "Title:", 
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Medium
            };
            Grid.SetRow(titleLabel, 0);
            Grid.SetColumn(titleLabel, 0);
            
            var titleBorder = new Border {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.White
            };
            Grid.SetRow(titleBorder, 0);
            Grid.SetColumn(titleBorder, 1);
            
            // Create a grid for the title textbox and watermark
            var titleGrid = new Grid();
            
            var titleTextBox = new TextBox { 
                Margin = new Thickness(5), 
                VerticalAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Padding = new Thickness(3)
            };
            
            // Create watermark for title
            var titleWatermark = new TextBlock {
                Text = "Enter snippet title here",
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            
            // Add both to the grid
            titleGrid.Children.Add(titleTextBox);
            titleGrid.Children.Add(titleWatermark);
            
            // Set up binding to hide watermark when text is entered
            titleTextBox.TextChanged += (s, args) => {
                titleWatermark.Visibility = string.IsNullOrEmpty(titleTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            };
            
            titleBorder.Child = titleGrid;
            
            // Content label and textbox
            var contentLabel = new TextBlock { 
                Text = "Content:", 
                VerticalAlignment = VerticalAlignment.Top, 
                Margin = new Thickness(0, 5, 0, 0),
                FontWeight = FontWeights.Medium
            };
            Grid.SetRow(contentLabel, 2);
            Grid.SetColumn(contentLabel, 0);
            Grid.SetRowSpan(contentLabel, 2);
            
            var contentBorder = new Border {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.White
            };
            Grid.SetRow(contentBorder, 2);
            Grid.SetColumn(contentBorder, 1);
            Grid.SetRowSpan(contentBorder, 2);
            
            // Create a grid for the content textbox and watermark
            var contentGrid = new Grid();
            
            var contentTextBox = new TextBox { 
                AcceptsReturn = true, 
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Stretch,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Margin = new Thickness(5),
                Padding = new Thickness(3)
            };
            
            // Create watermark for content
            var contentWatermark = new TextBlock {
                Text = "Type your snippet content here",
                Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                Margin = new Thickness(8, 8, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false
            };
            
            // Add both to the grid
            contentGrid.Children.Add(contentTextBox);
            contentGrid.Children.Add(contentWatermark);
            
            // Set up binding to hide watermark when text is entered
            contentTextBox.TextChanged += (s, args) => {
                contentWatermark.Visibility = string.IsNullOrEmpty(contentTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            };
            
            contentBorder.Child = contentGrid;
            
            // Button panel
            var buttonPanel = new StackPanel { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(buttonPanel, 4);
            Grid.SetColumn(buttonPanel, 0);
            Grid.SetColumnSpan(buttonPanel, 2);
            
            // Create Button template with rounded corners
            ControlTemplate buttonTemplate = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            
            FrameworkElementFactory contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Button.PaddingProperty));
            
            borderFactory.AppendChild(contentPresenterFactory);
            buttonTemplate.VisualTree = borderFactory;
            
            // Add triggers for mouse over and pressed states
            Trigger mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(229, 229, 229))));
            mouseOverTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(204, 204, 204))));
            buttonTemplate.Triggers.Add(mouseOverTrigger);
            
            Trigger pressedTrigger = new Trigger { Property = Button.IsPressedProperty, Value = true };
            pressedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(208, 208, 208))));
            pressedTrigger.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(187, 187, 187))));
            buttonTemplate.Triggers.Add(pressedTrigger);
            
            // Create Button styles
            Style createSaveButtonStyle = new Style(typeof(Button));
            createSaveButtonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 120, 212))));
            createSaveButtonStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            createSaveButtonStyle.Setters.Add(new Setter(Button.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0, 103, 184))));
            createSaveButtonStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(1)));
            createSaveButtonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(15, 6, 15, 6)));
            createSaveButtonStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(5, 0, 0, 0)));
            createSaveButtonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));
            
            Style createCancelButtonStyle = new Style(typeof(Button));
            createCancelButtonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(240, 240, 240))));
            createCancelButtonStyle.Setters.Add(new Setter(Button.ForegroundProperty, new SolidColorBrush(Color.FromRgb(51, 51, 51))));
            createCancelButtonStyle.Setters.Add(new Setter(Button.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(224, 224, 224))));
            createCancelButtonStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(1)));
            createCancelButtonStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(15, 6, 15, 6)));
            createCancelButtonStyle.Setters.Add(new Setter(Button.MarginProperty, new Thickness(5, 0, 0, 0)));
            createCancelButtonStyle.Setters.Add(new Setter(Button.TemplateProperty, buttonTemplate));
            
            var saveButton = new Button { 
                Content = "Save", 
                Width = 100, 
                Height = 30, 
                IsDefault = true,
                Style = createSaveButtonStyle
            };
            
            var cancelButton = new Button { 
                Content = "Cancel", 
                Width = 100, 
                Height = 30, 
                IsCancel = true,
                Style = createCancelButtonStyle
            };
            
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);
            
            // Add all controls to the grid
            grid.Children.Add(titleLabel);
            grid.Children.Add(titleBorder);
            grid.Children.Add(contentLabel);
            grid.Children.Add(contentBorder);
            grid.Children.Add(buttonPanel);
            
            // Set the content
            mainBorder.Child = grid;
            dialog.Content = mainBorder;
            
            // Wire up the save button
            saveButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(titleTextBox.Text))
                {
                    MessageBox.Show("Please enter a title for the snippet.", "Required Field", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(contentTextBox.Text))
                {
                    MessageBox.Show("Please enter content for the snippet.", "Required Field", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Add the new snippet
                Snippets.Add(new Snippet
                {
                    Title = titleTextBox.Text.Trim(),
                    Content = contentTextBox.Text
                });
                
                // Save snippets to file
                SaveSnippets();
                
                // Close the dialog
                dialog.DialogResult = true;
            };
            
            // Show the dialog
            dialog.ShowDialog();
            
            // Restart auto-close timer after dialog is closed
            if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text) && !_isAutoClosePaused)
            {
                StartAutoCloseTimer();
                StartCountdownTimer();
                _remainingSeconds = AutoCloseTimeoutSeconds;
                UpdateCountdownDisplay();
            }
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
                    // Instead of showing a message box, use a special action type for empty submissions
                    _feedbackText = "User did not provide any feedback. Please continue without human guidance. Use your best judgment to proceed safely.";
                    _actionType = "no_feedback"; // New action type for empty submissions
                    _isSubmitSuccess = true;
                    Close();
                    return;
                }

                // Store feedback data to be saved when window closes
                _feedbackText = FeedbackTextBox.Text;
                _actionType = "submit"; // Set action type to submit
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

        // New event handler for Approve button
        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set default approval message if text box is empty
                _feedbackText = string.IsNullOrWhiteSpace(FeedbackTextBox.Text) ? 
                    "I approve. Please continue." : FeedbackTextBox.Text;
                
                _actionType = "approve"; // Set action type to approve
                _isSubmitSuccess = true;
                
                // Close the window - feedback will be saved in the Closing event handler
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing approval: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _isSubmitSuccess = false;
            }
        }

        // New event handler for Reject button
        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set default rejection message if text box is empty
                _feedbackText = string.IsNullOrWhiteSpace(FeedbackTextBox.Text) ? 
                    "I reject. Please think of a better solution." : FeedbackTextBox.Text;
                
                _actionType = "reject"; // Set action type to reject
                _isSubmitSuccess = true;
                
                // Close the window - feedback will be saved in the Closing event handler
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing rejection: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _isSubmitSuccess = false;
            }
        }

        // New event handler for AI Decide button
        private void AiDecideButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Set default message if text box is empty
                _feedbackText = string.IsNullOrWhiteSpace(FeedbackTextBox.Text) ? 
                    "I want you judge your own decision and decide, I give you free will to judge and decide. Please consider all implications and potential risks before proceeding." : FeedbackTextBox.Text;
                
                _actionType = "ai_decide"; // Set action type to ai_decide
                _isSubmitSuccess = true;
                
                // Close the window - feedback will be saved in the Closing event handler
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing AI decision request: {ex.Message}", "Error", 
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
                    actionType = _actionType, // Include the action type in the JSON output
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
            if (ScrollIndicator == null) return; // Guard clause if ScrollIndicator is not ready

            // Show scroll indicator if there's vertical scrolling available
            if (e.ExtentHeight > e.ViewportHeight)
            {
                ScrollIndicator.Visibility = Visibility.Visible;
                
                // Determine text based on scroll position
                if (Math.Abs(e.VerticalOffset + e.ViewportHeight - e.ExtentHeight) < 0.5) // At the bottom
                {
                    ScrollIndicator.Text = "⬆ Scroll for more ⬆";
                }
                else if (e.VerticalOffset < 0.5) // At the top
                {
                    ScrollIndicator.Text = "⬇ Scroll for more ⬇";
                }
                else // Somewhere in the middle
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
            // Reset the auto-close timer when text changes
            if (_autoCloseTimer != null && !string.IsNullOrWhiteSpace(FeedbackTextBox.Text))
            {
                _autoCloseTimer.Stop();
                _countdownTimer?.Stop();
                CountdownTimer.Text = "";
            }
            else if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text) && !_isAutoClosePaused)
            {
                // If text was cleared, restart the timers only if not paused
                if (_autoCloseTimer != null)
                {
                    _autoCloseTimer.Start();
                }
                if (_countdownTimer != null)
                {
                    _countdownTimer.Start();
                }
                _remainingSeconds = AutoCloseTimeoutSeconds;
                UpdateCountdownDisplay();
            }
            
            // Use a timer to avoid excessive resizing on rapid typing
            if (_textChangedTimer == null)
            {
                _textChangedTimer = new System.Windows.Threading.DispatcherTimer();
                _textChangedTimer.Interval = TimeSpan.FromMilliseconds(100); // Reduced delay slightly
                _textChangedTimer.Tick += (s, args) =>
                {
                    if (_textChangedTimer != null) _textChangedTimer.Stop(); // Stop timer first
                    this.SizeToContent = SizeToContent.Height;
                    // Re-check scrolling after resize and delay
                    UpdateScrollIndicatorVisibility(); 
                };
            }
            
            // Reset and restart the timer when text changes
            if (_textChangedTimer != null) 
            {
                _textChangedTimer.Stop();
                _textChangedTimer.Start();
            }
        }
        
        // Helper method to update the scroll indicator visibility
        private void UpdateScrollIndicatorVisibility()
        {
            if (FeedbackTextBox == null || ScrollIndicator == null) return; // Guard clause

            try
            {
                var scrollViewer = GetScrollViewerForTextBox(FeedbackTextBox);
                if (scrollViewer != null) 
                {
                    // Check if the ScrollViewer itself is visible; otherwise, its scroll properties might not be relevant
                    if (scrollViewer.IsVisible && scrollViewer.ExtentHeight > scrollViewer.ViewportHeight)
                    {
                        ScrollIndicator.Visibility = Visibility.Visible;
                        
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
                else
                {
                    ScrollIndicator.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // Log this or handle; a crash here means something is very wrong with UI state
                Console.WriteLine($"Error in UpdateScrollIndicatorVisibility: {ex.Message}");
                if (ScrollIndicator != null) // Check if ScrollIndicator is null before accessing it
                {
                    ScrollIndicator.Visibility = Visibility.Collapsed; // Try to fail safe
                }
            }
        }
        
        // Gets the ScrollViewer associated with a TextBox
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

        // Generic recursive helper to find a visual child of a specific type
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

        private void StartAutoCloseTimer()
        {
            _autoCloseTimer = new DispatcherTimer();
            _autoCloseTimer.Interval = TimeSpan.FromSeconds(AutoCloseTimeoutSeconds);
            _autoCloseTimer.Tick += AutoCloseTimer_Tick;
            _autoCloseTimer.Start();
            
            // Reset the remaining seconds
            _remainingSeconds = AutoCloseTimeoutSeconds;
            UpdateCountdownDisplay();
        }
        
        private void StartCountdownTimer()
        {
            _countdownTimer = new DispatcherTimer();
            _countdownTimer.Interval = TimeSpan.FromSeconds(1);
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }
        
        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (_isAutoClosePaused) return;

            if (_remainingSeconds > 0 && string.IsNullOrWhiteSpace(FeedbackTextBox.Text))
            {
                _remainingSeconds--;
                UpdateCountdownDisplay();
            }
        }
        
        private void UpdateCountdownDisplay()
        {
            if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text))
            {
                CountdownTimer.Text = $"Auto-close: {_remainingSeconds}s";
                
                // Change color to red when time is running low
                if (_remainingSeconds <= 5)
                {
                    CountdownTimer.Foreground = Brushes.Red;
                    CountdownTimer.FontWeight = FontWeights.Bold;
                }
                else
                {
                    CountdownTimer.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)); // #666666
                    CountdownTimer.FontWeight = FontWeights.Normal;
                }
            }
            else
            {
                // Hide the countdown when there's text
                CountdownTimer.Text = "";
            }
        }
        
        private void AutoCloseTimer_Tick(object? sender, EventArgs e)
        {
            if (_isAutoClosePaused) return; // Do nothing if paused

            // If no feedback has been entered
            if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text))
            {
                // Set the default feedback message with no_feedback action type
                _feedbackText = "User did not provide any feedback. You are now given the free will to judge and decide. Use your best judgment to proceed safely.";
                _actionType = "no_feedback"; // Use the new action type for auto-close with no feedback
                _isSubmitSuccess = true;
                
                // Close the window
                Close();
            }
            else
            {
                // If there is feedback, stop the timer
                _autoCloseTimer?.Stop();
                _countdownTimer?.Stop();
                CountdownTimer.Text = "";
            }
        }
        
        private void ResetAutoCloseTimer(object sender, EventArgs e)
        {
            // Only reset if feedback is empty and not paused
            if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text) && !_isAutoClosePaused)
            {
                if (_autoCloseTimer != null)
                {
                    _autoCloseTimer.Stop();
                    _autoCloseTimer.Start();
                    
                    // Reset the countdown
                    _remainingSeconds = AutoCloseTimeoutSeconds;
                    UpdateCountdownDisplay();
                }
                else
                {
                    StartAutoCloseTimer();
                }
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Handle manage snippets button click
        private void ManageSnippetsButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop the auto-close timer while dialog is open
            if (_autoCloseTimer != null)
            {
                _autoCloseTimer.Stop();
            }
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
            }
            
            // Check if we have any snippets
            if (Snippets.Count == 0)
            {
                MessageBox.Show("No snippets to manage. Create a snippet first.", "No Snippets", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Restart auto-close timer after dialog is closed
                if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text) && !_isAutoClosePaused)
                {
                    StartAutoCloseTimer();
                    StartCountdownTimer();
                    _remainingSeconds = AutoCloseTimeoutSeconds;
                    UpdateCountdownDisplay();
                }
                
                return;
            }
            
            // Create a new dialog window to manage snippets
            var dialog = new Window
            {
                Title = "Manage Snippets",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            // Create the content
            var mainBorder = new Border
            {
                Margin = new Thickness(15),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1)
            };
            
            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            
            // Header text
            var headerText = new TextBlock 
            { 
                Text = "Select a snippet to edit or delete:", 
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(headerText, 0);
            
            // Snippet list
            var listBox = new ListBox 
            { 
                Margin = new Thickness(0, 0, 0, 15),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                Background = Brushes.White
            };
            Grid.SetRow(listBox, 1);
            
            // Populate the list box with snippet items
            foreach (var snippet in Snippets)
            {
                var item = new ListBoxItem { Content = snippet.Title, Tag = snippet };
                listBox.Items.Add(item);
            }
            
            // Button panel
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(buttonPanel, 2);
            
            // Create Button styles
            Style editButtonStyle = new Style(typeof(Button));
            editButtonStyle.BasedOn = Resources["ModernButton"] as Style;
            
            Style deleteButtonStyle = new Style(typeof(Button));
            deleteButtonStyle.BasedOn = Resources["RedButton"] as Style;
            
            Style closeButtonStyle = new Style(typeof(Button));
            closeButtonStyle.BasedOn = Resources["ModernButton"] as Style;
            
            // Create buttons
            var editButton = new Button 
            { 
                Content = "Edit Selected", 
                Width = 120, 
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Style = editButtonStyle,
                IsEnabled = false // Disabled until a snippet is selected
            };
            
            var deleteButton = new Button 
            { 
                Content = "Delete Selected", 
                Width = 120, 
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Style = deleteButtonStyle,
                IsEnabled = false // Disabled until a snippet is selected
            };
            
            var closeButton = new Button 
            { 
                Content = "Close", 
                Width = 80, 
                Height = 32,
                Style = closeButtonStyle,
                IsCancel = true
            };
            
            // Add buttons to panel
            buttonPanel.Children.Add(editButton);
            buttonPanel.Children.Add(deleteButton);
            buttonPanel.Children.Add(closeButton);
            
            // Enable/disable edit and delete buttons based on selection
            listBox.SelectionChanged += (s, args) => 
            {
                bool hasSelection = listBox.SelectedItem != null;
                editButton.IsEnabled = hasSelection;
                deleteButton.IsEnabled = hasSelection;
            };
            
            // Handle edit button click
            editButton.Click += (s, args) => 
            {
                if (listBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is Snippet selectedSnippet)
                {
                    // Open the edit dialog
                    if (EditSnippet(selectedSnippet))
                    {
                        // Update the list item text if edited
                        selectedItem.Content = selectedSnippet.Title;
                    }
                }
            };
            
            // Handle delete button click
            deleteButton.Click += (s, args) => 
            {
                if (listBox.SelectedItem is ListBoxItem selectedItem && selectedItem.Tag is Snippet selectedSnippet)
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to delete the snippet '{selectedSnippet.Title}'?",
                        "Confirm Delete",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                        
                    if (result == MessageBoxResult.Yes)
                    {
                        // Remove from collection
                        Snippets.Remove(selectedSnippet);
                        
                        // Remove from list box
                        listBox.Items.Remove(selectedItem);
                        
                        // Save to file
                        SaveSnippets();
                        
                        // Disable buttons if no more snippets
                        if (listBox.Items.Count == 0)
                        {
                            editButton.IsEnabled = false;
                            deleteButton.IsEnabled = false;
                        }
                    }
                }
            };
            
            // Handle close button click
            closeButton.Click += (s, args) => 
            {
                dialog.DialogResult = true;
            };
            
            // Add all controls to the grid
            grid.Children.Add(headerText);
            grid.Children.Add(listBox);
            grid.Children.Add(buttonPanel);
            
            // Set the content
            mainBorder.Child = grid;
            dialog.Content = mainBorder;
            
            // Show the dialog
            dialog.ShowDialog();
            
            // Restart auto-close timer after dialog is closed
            if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text) && !_isAutoClosePaused)
            {
                StartAutoCloseTimer();
                StartCountdownTimer();
                _remainingSeconds = AutoCloseTimeoutSeconds;
                UpdateCountdownDisplay();
            }
        }
        
        // Edit an existing snippet
        private bool EditSnippet(Snippet snippet)
        {
            // Create a dialog window for editing the snippet
            var dialog = new Window
            {
                Title = "Edit Snippet",
                Width = 450,
                Height = 280,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250))
            };

            // Create the content
            var mainBorder = new Border
            {
                Margin = new Thickness(15),
                CornerRadius = new CornerRadius(6),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1)
            };
            
            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // Spacer
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            
            // Title label and textbox
            var titleLabel = new TextBlock { 
                Text = "Title:", 
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Medium
            };
            Grid.SetRow(titleLabel, 0);
            Grid.SetColumn(titleLabel, 0);
            
            var titleBorder = new Border {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.White
            };
            Grid.SetRow(titleBorder, 0);
            Grid.SetColumn(titleBorder, 1);
            
            var titleTextBox = new TextBox { 
                Text = snippet.Title,
                Margin = new Thickness(8), 
                VerticalAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent
            };
            
            titleBorder.Child = titleTextBox;
            
            // Content label and textbox
            var contentLabel = new TextBlock { 
                Text = "Content:", 
                VerticalAlignment = VerticalAlignment.Top, 
                Margin = new Thickness(0, 5, 0, 0),
                FontWeight = FontWeights.Medium
            };
            Grid.SetRow(contentLabel, 2);
            Grid.SetColumn(contentLabel, 0);
            Grid.SetRowSpan(contentLabel, 2);
            
            var contentBorder = new Border {
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                CornerRadius = new CornerRadius(4),
                Background = Brushes.White
            };
            Grid.SetRow(contentBorder, 2);
            Grid.SetColumn(contentBorder, 1);
            Grid.SetRowSpan(contentBorder, 2);
            
            var contentTextBox = new TextBox { 
                Text = snippet.Content,
                AcceptsReturn = true, 
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Stretch,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Margin = new Thickness(8)
            };
            
            contentBorder.Child = contentTextBox;
            
            // Button panel
            var buttonPanel = new StackPanel { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            Grid.SetRow(buttonPanel, 4);
            Grid.SetColumn(buttonPanel, 0);
            Grid.SetColumnSpan(buttonPanel, 2);
            
            // Create Button styles
            Style saveButtonStyle = new Style(typeof(Button));
            saveButtonStyle.BasedOn = Resources["AccentButton"] as Style;
            
            Style cancelButtonStyle = new Style(typeof(Button));
            cancelButtonStyle.BasedOn = Resources["ModernButton"] as Style;
            
            var saveButton = new Button { 
                Content = "Save Changes", 
                Width = 120, 
                Height = 32, 
                IsDefault = true,
                Style = saveButtonStyle
            };
            
            var cancelButton = new Button { 
                Content = "Cancel", 
                Width = 80, 
                Height = 32, 
                IsCancel = true,
                Style = cancelButtonStyle
            };
            
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);
            
            // Add all controls to the grid
            grid.Children.Add(titleLabel);
            grid.Children.Add(titleBorder);
            grid.Children.Add(contentLabel);
            grid.Children.Add(contentBorder);
            grid.Children.Add(buttonPanel);
            
            // Set the content
            mainBorder.Child = grid;
            dialog.Content = mainBorder;
            
            // Wire up the save button
            saveButton.Click += (s, args) =>
            {
                if (string.IsNullOrWhiteSpace(titleTextBox.Text))
                {
                    MessageBox.Show("Please enter a title for the snippet.", "Required Field", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                if (string.IsNullOrWhiteSpace(contentTextBox.Text))
                {
                    MessageBox.Show("Please enter content for the snippet.", "Required Field", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Update the snippet
                snippet.Title = titleTextBox.Text.Trim();
                snippet.Content = contentTextBox.Text;
                
                // Save snippets to file
                SaveSnippets();
                
                // Close the dialog
                dialog.DialogResult = true;
            };
            
            // Show the dialog and return true if OK was clicked
            bool? result = dialog.ShowDialog();
            return result ?? false;
        }

        // Pause/Resume Button Click Handler
        private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _isAutoClosePaused = !_isAutoClosePaused;

            if (_isAutoClosePaused)
            {
                PauseResumeButton.Content = "Resume Timer";
                _autoCloseTimer?.Stop();
                _countdownTimer?.Stop();
            }
            else
            {
                PauseResumeButton.Content = "Pause Timer";
                if (string.IsNullOrWhiteSpace(FeedbackTextBox.Text))
                {
                    // If resuming and textbox is empty, restart timers
                    _remainingSeconds = AutoCloseTimeoutSeconds; // Reset remaining time
                    StartAutoCloseTimer(); // This will also start the countdown timer via its logic
                    StartCountdownTimer();
                    UpdateCountdownDisplay();
                }
            }
        }
    }

    // Class to store information about an image
    public class ImageItem
    {
        public string FilePath { get; set; } = "";
        public string MimeType { get; set; } = "";
        public UIElement? UiElement { get; set; }
    }

    // Class to store snippet information
    public class Snippet
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";

        public override string ToString()
        {
            return Title;
        }
    }
} 