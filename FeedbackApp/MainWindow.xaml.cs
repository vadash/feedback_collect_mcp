using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FeedbackApp.Models;
using FeedbackApp.Services;
using FeedbackApp.Managers;
using FeedbackApp.Dialogs;
using FeedbackApp.Helpers;

namespace FeedbackApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Services
        private readonly FeedbackService _feedbackService;
        private readonly SnippetService _snippetService;
        private readonly ImageService _imageService;
        private readonly TimerService _timerService;
        private readonly AudioService _audioService;
        private readonly UIManager _uiManager;

        // Properties
        private string _windowTitle = "AI Feedback Collection";
        private string _promptText = "Please provide your feedback or describe your issue:";
        private string? _outputFilePath;
        private bool _isSubmitSuccess = false;
        private string? _feedbackText;
        private string _actionType = "submit";

        // Collections
        private readonly List<ImageItemModel> _images = new List<ImageItemModel>();
        private ObservableCollection<SnippetModel> _snippets = new ObservableCollection<SnippetModel>();

        // UI state
        private DispatcherTimer? _textChangedTimer;

        public ObservableCollection<SnippetModel> Snippets
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

            // Initialize services
            _feedbackService = new FeedbackService();
            _snippetService = new SnippetService();
            _imageService = new ImageService();
            _timerService = new TimerService();
            _audioService = new AudioService();

            // Initialize UI manager
            _uiManager = new UIManager(
                ImagesPanel,
                NoImagesPlaceholder,
                ImagesExpander,
                ImageCountText,
                ScrollIndicator,
                FeedbackTextBox,
                this);

            // Initialize the application
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                // Play startup sound
                _audioService.PlayStartupSound(0.5); // 50% volume

                // Load snippets
                Snippets = await _snippetService.LoadSnippetsAsync();

                // Process command line arguments
                ProcessCommandLineArguments();

                // Set up event handlers
                SetupEventHandlers();

                // Initialize UI state
                InitializeUIState();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to initialize application: {ex.Message}");
            }
        }

        private void ProcessCommandLineArguments()
        {
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
        }

        private void SetupEventHandlers()
        {
            // Window events
            this.Closing += MainWindow_Closing;
            this.PreviewMouseMove += (s, e) => ResetAutoCloseTimer();
            this.PreviewKeyDown += (s, e) => ResetAutoCloseTimer();
            this.Activated += (s, e) => ResetAutoCloseTimer();

            // Text box events
            FeedbackTextBox.TextChanged += FeedbackTextBox_TextChanged;

            // Timer events
            _timerService.AutoCloseTimerExpired += OnAutoCloseTimerExpired;
            _timerService.CountdownUpdated += OnCountdownUpdated;
        }

        private void InitializeUIState()
        {
            // Initialize image count display
            _uiManager.UpdateImageCount(_images.Count, _imageService.MaxImageCount);

            // Start the auto-close timer
            _timerService.StartTimer();
        }

        // Handle snippet selection
        private void SnippetsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SnippetsComboBox.SelectedItem is SnippetModel selectedSnippet)
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
        private async void AddSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pause timer while dialog is open
                _timerService.TogglePause();

                var dialog = new SnippetDialog(this);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    var newSnippet = dialog.GetSnippet();
                    await _snippetService.AddSnippetAsync(Snippets, newSnippet.Title, newSnippet.Content);
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to add snippet: {ex.Message}");
            }
            finally
            {
                // Resume timer if conditions are met
                if (_timerService.ShouldTimerBeActive(string.IsNullOrWhiteSpace(FeedbackTextBox.Text)))
                {
                    _timerService.TogglePause();
                }
            }
        }

        private async void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                // Only save feedback if Submit button was clicked (not if Cancel was clicked)
                if (_isSubmitSuccess)
                {
                    await SaveFeedbackAsync();
                }

                // Clean up temp images
                _imageService.CleanupTempImages(_images);

                // Dispose of services
                _timerService.Dispose();
                _audioService.Dispose();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Error during shutdown: {ex.Message}");
            }
        }

        // Handle keyboard events for the FeedbackTextBox
        private void FeedbackTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Check for Ctrl+V (paste)
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                HandleImagePaste();
            }
        }

        private void HandleImagePaste()
        {
            try
            {
                // Try to extract image from clipboard
                if (_imageService.TryGetImageFromClipboard(out BitmapSource? image))
                {
                    // Check if we can add more images
                    if (!_imageService.CanAddMoreImages(_images.Count))
                    {
                        MessageBox.Show($"You can attach a maximum of {_imageService.MaxImageCount} images.",
                            "Maximum Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Save the image and create an image item
                    string imageFilePath = _imageService.SaveClipboardImageToFile(image);
                    var imageItem = _imageService.CreateImageItem(imageFilePath, isTemporary: true);

                    // Add to our collection and UI
                    AddImageItem(imageItem);
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to paste image: {ex.Message}");
            }
        }
        
        private void AddImageItem(ImageItemModel imageItem)
        {
            // Add to our collection
            _images.Add(imageItem);

            // Add to UI
            _uiManager.AddImageToPanel(imageItem, RemoveImageItem);

            // Update UI state
            UpdateImageUIState();
        }

        private void RemoveImageItem(ImageItemModel imageItem)
        {
            try
            {
                // Remove from UI
                _uiManager.RemoveImageFromPanel(imageItem);

                // Remove from our collection
                _images.Remove(imageItem);

                // Update UI state
                UpdateImageUIState();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to remove image: {ex.Message}");
            }
        }

        private void UpdateImageUIState()
        {
            _uiManager.UpdateImageCount(_images.Count, _imageService.MaxImageCount);
            _uiManager.UpdateNoImagesPlaceholder(_images.Count > 0);
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if we can add more images
                if (!_imageService.CanAddMoreImages(_images.Count))
                {
                    MessageBox.Show($"You can attach a maximum of {_imageService.MaxImageCount} images.",
                        "Maximum Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Image",
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var imageItem = _imageService.CreateImageItem(openFileDialog.FileName);
                    AddImageItem(imageItem);
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to add image: {ex.Message}");
            }
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

        private async Task SaveFeedbackAsync()
        {
            try
            {
                var feedbackData = _feedbackService.CreateFeedbackData(_feedbackText ?? string.Empty, _actionType, _images);
                await _feedbackService.SaveFeedbackAsync(feedbackData, _outputFilePath);
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to save feedback: {ex.Message}");
            }
        }

        // Handle drag-over event for the FeedbackTextBox
        private void FeedbackTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var imageFiles = _imageService.FilterImageFiles(files);

                if (imageFiles.Count > 0)
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        // Handle drop event for the FeedbackTextBox
        private void FeedbackTextBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var imageFiles = _imageService.FilterImageFiles(files);

                    if (imageFiles.Count > 0)
                    {
                        var availableSlots = _imageService.GetAvailableSlots(_images.Count);

                        if (availableSlots <= 0)
                        {
                            MessageBox.Show($"You can attach a maximum of {_imageService.MaxImageCount} images. No more images can be added.",
                                "Maximum Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var imagesToAdd = Math.Min(availableSlots, imageFiles.Count);

                        for (int i = 0; i < imagesToAdd; i++)
                        {
                            var imageItem = _imageService.CreateImageItem(imageFiles[i]);
                            AddImageItem(imageItem);
                        }

                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to add dropped images: {ex.Message}");
            }
        }

        // Event handler for when the Images expander is expanded
        private void ImagesExpander_Expanded(object sender, RoutedEventArgs e)
        {
            _uiManager.RecalculateWindowSize();

            // Animate the window size change after a slight delay
            var timer = new DispatcherTimer();
            timer.Tick += (s, args) =>
            {
                _uiManager.RecalculateWindowSize();
                ((DispatcherTimer)s).Stop();
            };
            timer.Interval = TimeSpan.FromMilliseconds(300);
            timer.Start();
        }

        // Event handler for when the Images expander is collapsed
        private void ImagesExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            _uiManager.RecalculateWindowSize();
        }

        // Handle scroll changes in the TextBox
        private void FeedbackTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            _uiManager.UpdateScrollIndicator(e.VerticalOffset, e.ViewportHeight, e.ExtentHeight);
        }
        
        // Handler for text changes in the feedback textbox
        private void FeedbackTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Handle timer logic based on text content
            var hasText = !string.IsNullOrWhiteSpace(FeedbackTextBox.Text);

            if (hasText)
            {
                _timerService.StopTimer();
            }
            else if (_timerService.ShouldTimerBeActive(hasText))
            {
                _timerService.StartTimer();
            }

            // Use a timer to avoid excessive resizing on rapid typing
            if (_textChangedTimer == null)
            {
                _textChangedTimer = new DispatcherTimer();
                _textChangedTimer.Interval = TimeSpan.FromMilliseconds(100);
                _textChangedTimer.Tick += (s, args) =>
                {
                    _textChangedTimer?.Stop();
                    _uiManager.RecalculateWindowSize();
                };
            }

            _textChangedTimer.Stop();
            _textChangedTimer.Start();
        }

        // Timer event handlers
        private void OnAutoCloseTimerExpired(object? sender, EventArgs e)
        {
            _feedbackText = "User did not provide any feedback. You are now given the free will to judge and decide. Use your best judgment to proceed safely.";
            _actionType = "no_feedback";
            _isSubmitSuccess = true;
            Close();
        }

        private void OnCountdownUpdated(object? sender, CountdownUpdateEventArgs e)
        {
            if (e.IsActive && string.IsNullOrWhiteSpace(FeedbackTextBox.Text))
            {
                CountdownTimer.Text = $"Auto-close: {e.RemainingSeconds}s";

                // Change color to red when time is running low
                if (e.RemainingSeconds <= 5)
                {
                    CountdownTimer.Foreground = Brushes.Red;
                    CountdownTimer.FontWeight = FontWeights.Bold;
                }
                else
                {
                    CountdownTimer.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                    CountdownTimer.FontWeight = FontWeights.Normal;
                }
            }
            else
            {
                CountdownTimer.Text = "";
            }
        }

        private void ResetAutoCloseTimer()
        {
            if (_timerService.ShouldTimerBeActive(string.IsNullOrWhiteSpace(FeedbackTextBox.Text)))
            {
                _timerService.ResetTimer();
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



        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Handle manage snippets button click
        private async void ManageSnippetsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Pause timer while dialog is open
                _timerService.TogglePause();

                // Check if we have any snippets
                if (Snippets.Count == 0)
                {
                    MessageBox.Show("No snippets to manage. Create a snippet first.", "No Snippets",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // TODO: Create SnippetManagementDialog class
                // For now, show a simple message
                MessageBox.Show("Snippet management dialog will be implemented in the next phase.", "Coming Soon",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to open snippet management: {ex.Message}");
            }
            finally
            {
                // Resume timer if conditions are met
                if (_timerService.ShouldTimerBeActive(string.IsNullOrWhiteSpace(FeedbackTextBox.Text)))
                {
                    _timerService.TogglePause();
                }
            }
        }
        
        // Pause/Resume Button Click Handler
        private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            _timerService.TogglePause();

            // Update button text based on timer state
            PauseResumeButton.Content = _timerService.IsPaused ? "Resume Timer" : "Pause Timer";
        }
    }
}