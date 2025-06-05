using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using FeedbackApp.Models;
using FeedbackApp.Configuration;
using FeedbackApp.Infrastructure;
using FeedbackApp.Coordinators;
using FeedbackApp.Handlers;
using FeedbackApp.Services;
using FeedbackApp.Managers;

namespace FeedbackApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Core dependencies
        private readonly ServiceContainer _serviceContainer;
        private readonly AppConfiguration _configuration;
        private readonly ApplicationCoordinator _coordinator;

        // Properties for data binding
        private string _windowTitle;
        private string _promptText;

        public ObservableCollection<SnippetModel> Snippets => _coordinator?.Snippets ?? new ObservableCollection<SnippetModel>();

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

            // Initialize configuration
            _configuration = AppConfiguration.CreateDefault();
            _windowTitle = _configuration.WindowTitle;
            _promptText = _configuration.PromptText;

            // Initialize service container
            _serviceContainer = ServiceContainer.ConfigureServices(_configuration);

            // Create shared images collection
            var images = new System.Collections.Generic.List<Models.ImageItemModel>();

            // Configure UI services with shared images collection
            _serviceContainer.ConfigureUIServices(
                ImagesPanel,
                NoImagesPlaceholder,
                ImagesExpander,
                ImageCountText,
                ScrollIndicator,
                FeedbackTextBox,
                SnippetsComboBox,
                this,
                images);

            // Initialize coordinator with shared images collection
            _coordinator = new ApplicationCoordinator(
                _serviceContainer,
                _configuration,
                this,
                FeedbackTextBox,
                SnippetsComboBox,
                images);

            // Initialize the application
            InitializeAsync();

            // Set up countdown event subscription
            SetupCountdownEventSubscription();
        }

        private async void InitializeAsync()
        {
            await _coordinator.InitializeAsync();

            // Update properties from configuration after command line arguments are processed
            _windowTitle = _configuration.WindowTitle;
            _promptText = _configuration.PromptText;

            // Update UI bindings after initialization
            OnPropertyChanged(nameof(Snippets));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(PromptText));
        }

        private void SetupCountdownEventSubscription()
        {
            var timerService = _serviceContainer.GetService<TimerService>();
            timerService.CountdownUpdated += OnCountdownUpdated;
        }

        // Event handlers that delegate to handlers
        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = _serviceContainer.GetService<FeedbackActionHandler>();
            handler.HandleSubmit();
        }

        private void ApproveButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = _serviceContainer.GetService<FeedbackActionHandler>();
            handler.HandleApprove();
        }

        private void RejectButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = _serviceContainer.GetService<FeedbackActionHandler>();
            handler.HandleReject();
        }

        private void AiDecideButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = _serviceContainer.GetService<FeedbackActionHandler>();
            handler.HandleAiDecide();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = _serviceContainer.GetService<FeedbackActionHandler>();
            handler.HandleCancel();
        }

        private async void AddSnippetButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = _serviceContainer.GetService<SnippetEventHandler>();
            await handler.HandleAddSnippet(Snippets);
            OnPropertyChanged(nameof(Snippets));
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = _serviceContainer.GetService<ImageEventHandler>();
            handler.HandleAddImageFromFile();
        }

        private void ManageSnippetsButton_Click(object sender, RoutedEventArgs e)
        {
            var handler = _serviceContainer.GetService<SnippetEventHandler>();
            handler.HandleManageSnippets(Snippets);
        }

        private void PauseResumeButton_Click(object sender, RoutedEventArgs e)
        {
            var timerService = _serviceContainer.GetService<TimerService>();
            timerService.TogglePause();

            // Update button text based on timer state
            PauseResumeButton.Content = timerService.IsPaused ? "Resume Timer" : "Pause Timer";
        }

        private void ImagesExpander_Expanded(object sender, RoutedEventArgs e)
        {
            var uiManager = _serviceContainer.GetService<UIManager>();
            uiManager.RecalculateWindowSize();

            // Animate the window size change after a slight delay
            var timer = new DispatcherTimer();
            timer.Tick += (s, args) =>
            {
                uiManager.RecalculateWindowSize();
                if (s is DispatcherTimer dispatcherTimer)
                {
                    dispatcherTimer.Stop();
                }
            };
            timer.Interval = TimeSpan.FromMilliseconds(AppConfiguration.ExpanderAnimationDelayMs);
            timer.Start();
        }

        private void ImagesExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            var uiManager = _serviceContainer.GetService<UIManager>();
            uiManager.RecalculateWindowSize();
        }

        // Countdown timer display handler
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

        // Event handlers that delegate to coordinator
        private void SnippetsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Delegate to coordinator
            _coordinator?.GetType().GetMethod("SnippetsComboBox_SelectionChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_coordinator, new object[] { sender, e });
        }

        private void FeedbackTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Delegate to coordinator
            _coordinator?.GetType().GetMethod("FeedbackTextBox_PreviewKeyDown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_coordinator, new object[] { sender, e });
        }

        private void FeedbackTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Delegate to coordinator
            _coordinator?.GetType().GetMethod("FeedbackTextBox_PreviewDragOver",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_coordinator, new object[] { sender, e });
        }

        private void FeedbackTextBox_Drop(object sender, DragEventArgs e)
        {
            // Delegate to coordinator
            _coordinator?.GetType().GetMethod("FeedbackTextBox_Drop",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_coordinator, new object[] { sender, e });
        }

        private void FeedbackTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Delegate to coordinator
            _coordinator?.GetType().GetMethod("FeedbackTextBox_TextChanged",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_coordinator, new object[] { sender, e });
        }

        private void FeedbackTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollHandler = _serviceContainer.GetService<ScrollIndicatorHandler>();
            scrollHandler.UpdateScrollIndicator(e.VerticalOffset, e.ViewportHeight, e.ExtentHeight);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}