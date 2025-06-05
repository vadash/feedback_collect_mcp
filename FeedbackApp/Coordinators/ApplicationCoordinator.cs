using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FeedbackApp.Models;
using FeedbackApp.Services;
using FeedbackApp.Handlers;
using FeedbackApp.Infrastructure;
using FeedbackApp.Configuration;
using FeedbackApp.Helpers;
using FeedbackApp.Managers;

namespace FeedbackApp.Coordinators
{
    /// <summary>
    /// Coordinates application initialization and lifecycle management
    /// </summary>
    public class ApplicationCoordinator : IDisposable
    {
        private readonly ServiceContainer _serviceContainer;
        private readonly AppConfiguration _configuration;
        private readonly Window _mainWindow;
        
        // UI Elements
        private readonly TextBox _feedbackTextBox;
        private readonly ComboBox _snippetsComboBox;
        
        // Collections
        private readonly List<ImageItemModel> _images;
        private ObservableCollection<SnippetModel> _snippets = new();

        // UI state
        private DispatcherTimer? _textChangedTimer;

        public ObservableCollection<SnippetModel> Snippets
        {
            get => _snippets;
            set => _snippets = value ?? new ObservableCollection<SnippetModel>();
        }

        public ApplicationCoordinator(
            ServiceContainer serviceContainer,
            AppConfiguration configuration,
            Window mainWindow,
            TextBox feedbackTextBox,
            ComboBox snippetsComboBox,
            List<ImageItemModel> images)
        {
            _serviceContainer = serviceContainer ?? throw new ArgumentNullException(nameof(serviceContainer));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            _feedbackTextBox = feedbackTextBox ?? throw new ArgumentNullException(nameof(feedbackTextBox));
            _snippetsComboBox = snippetsComboBox ?? throw new ArgumentNullException(nameof(snippetsComboBox));
            _images = images ?? throw new ArgumentNullException(nameof(images));
        }

        /// <summary>
        /// Initializes the application asynchronously
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // Play startup sound
                var audioService = _serviceContainer.GetService<AudioService>();
                audioService.PlayStartupSound(_configuration.AudioVolume);

                // Load snippets
                var snippetService = _serviceContainer.GetService<SnippetService>();
                Snippets = await snippetService.LoadSnippetsAsync();

                // Process command line arguments
                ProcessCommandLineArguments();

                // Set up event handlers
                SetupEventHandlers();

                // Initialize UI state
                InitializeUIState();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"{AppConfiguration.ErrorMessages.InitializationFailed}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes command line arguments
        /// </summary>
        private void ProcessCommandLineArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            _configuration.UpdateFromCommandLineArgs(args);
        }

        /// <summary>
        /// Sets up all event handlers
        /// </summary>
        private void SetupEventHandlers()
        {
            // Window events
            _mainWindow.Closing += MainWindow_Closing;
            _mainWindow.PreviewMouseMove += (s, e) => ResetAutoCloseTimer();
            _mainWindow.PreviewKeyDown += (s, e) => ResetAutoCloseTimer();
            _mainWindow.Activated += (s, e) => ResetAutoCloseTimer();

            // Text box events - Note: PreviewKeyDown, PreviewDragOver, and Drop are handled via XAML to avoid duplicates
            _feedbackTextBox.TextChanged += FeedbackTextBox_TextChanged;

            // Snippet combo box events
            _snippetsComboBox.SelectionChanged += SnippetsComboBox_SelectionChanged;

            // Timer events
            var timerService = _serviceContainer.GetService<TimerService>();
            timerService.AutoCloseTimerExpired += OnAutoCloseTimerExpired;
            timerService.CountdownUpdated += OnCountdownUpdated;
        }

        /// <summary>
        /// Initializes UI state
        /// </summary>
        private void InitializeUIState()
        {
            var imageService = _serviceContainer.GetService<ImageService>();
            var uiManager = _serviceContainer.GetService<UIManager>();
            
            // Initialize image count display
            uiManager.UpdateImageCount(_images.Count, imageService.MaxImageCount);

            // Start the auto-close timer
            var timerService = _serviceContainer.GetService<TimerService>();
            timerService.StartTimer(_configuration.AutoCloseTimeoutSeconds);
        }

        /// <summary>
        /// Handles application shutdown
        /// </summary>
        private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                var feedbackActionHandler = _serviceContainer.GetService<FeedbackActionHandler>();
                
                // Only save feedback if Submit button was clicked (not if Cancel was clicked)
                if (feedbackActionHandler.IsSubmitSuccess)
                {
                    await SaveFeedbackAsync(feedbackActionHandler);
                }

                // Clean up temp images
                var imageService = _serviceContainer.GetService<ImageService>();
                imageService.CleanupTempImages(_images);

                // Dispose of services through container
                _serviceContainer.Dispose();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"{AppConfiguration.ErrorMessages.ShutdownError}: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves feedback data
        /// </summary>
        private async Task SaveFeedbackAsync(FeedbackActionHandler feedbackActionHandler)
        {
            try
            {
                var feedbackService = _serviceContainer.GetService<FeedbackService>();
                var feedbackData = feedbackService.CreateFeedbackData(
                    feedbackActionHandler.FeedbackText ?? string.Empty, 
                    feedbackActionHandler.ActionType, 
                    _images);
                await feedbackService.SaveFeedbackAsync(feedbackData, _configuration.OutputFilePath);
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"{AppConfiguration.ErrorMessages.FeedbackSaveFailed}: {ex.Message}");
            }
        }

        // Event handlers that delegate to appropriate handlers
        private void FeedbackTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Check for Ctrl+V (paste)
            if (e.Key == System.Windows.Input.Key.V && 
                (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                var imageHandler = _serviceContainer.GetService<ImageEventHandler>();
                imageHandler.HandleImagePaste();
            }
        }

        private void FeedbackTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            var imageHandler = _serviceContainer.GetService<ImageEventHandler>();
            imageHandler.HandleImageDragOver(e);
        }

        private void FeedbackTextBox_Drop(object sender, DragEventArgs e)
        {
            var imageHandler = _serviceContainer.GetService<ImageEventHandler>();
            imageHandler.HandleImageDrop(e);
        }

        private void FeedbackTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollHandler = _serviceContainer.GetService<ScrollIndicatorHandler>();
            scrollHandler.UpdateScrollIndicator(e.VerticalOffset, e.ViewportHeight, e.ExtentHeight);
        }

        private void FeedbackTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var timerService = _serviceContainer.GetService<TimerService>();
            var uiManager = _serviceContainer.GetService<UIManager>();
            
            // Handle timer logic based on text content
            var hasText = !string.IsNullOrWhiteSpace(_feedbackTextBox.Text);

            if (hasText)
            {
                timerService.StopTimer();
            }
            else if (timerService.ShouldTimerBeActive(hasText))
            {
                timerService.StartTimer();
            }

            // Use a timer to avoid excessive resizing on rapid typing
            if (_textChangedTimer == null)
            {
                _textChangedTimer = new DispatcherTimer();
                _textChangedTimer.Interval = TimeSpan.FromMilliseconds(AppConfiguration.TextChangedTimerIntervalMs);
                _textChangedTimer.Tick += (s, args) =>
                {
                    _textChangedTimer?.Stop();
                    uiManager.RecalculateWindowSize();
                };
            }

            _textChangedTimer.Stop();
            _textChangedTimer.Start();
        }

        private void SnippetsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_snippetsComboBox.SelectedItem is SnippetModel selectedSnippet)
            {
                var snippetHandler = _serviceContainer.GetService<SnippetEventHandler>();
                snippetHandler.HandleSnippetSelection(selectedSnippet);
            }
        }

        private void OnAutoCloseTimerExpired(object? sender, EventArgs e)
        {
            var feedbackActionHandler = _serviceContainer.GetService<FeedbackActionHandler>();
            // Set the feedback data for auto-close scenario
            feedbackActionHandler.GetType().GetProperty("FeedbackText")?.SetValue(feedbackActionHandler, AppConfiguration.DefaultMessages.AutoCloseNoFeedback);
            feedbackActionHandler.GetType().GetProperty("ActionType")?.SetValue(feedbackActionHandler, AppConfiguration.ActionTypes.NoFeedback);
            feedbackActionHandler.GetType().GetProperty("IsSubmitSuccess")?.SetValue(feedbackActionHandler, true);
            _mainWindow.Close();
        }

        private void OnCountdownUpdated(object? sender, CountdownUpdateEventArgs e)
        {
            // This would need to be handled by a countdown display handler
            // For now, keeping the existing logic in MainWindow
        }

        private void ResetAutoCloseTimer()
        {
            var timerService = _serviceContainer.GetService<TimerService>();
            timerService.ResetTimer();
        }

        public void Dispose()
        {
            _textChangedTimer?.Stop();
            _textChangedTimer = null;
            _serviceContainer?.Dispose();
        }
    }
}
