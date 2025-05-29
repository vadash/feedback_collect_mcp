using System;
using System.Collections.Generic;
using FeedbackApp.Services;
using FeedbackApp.Managers;
using FeedbackApp.Handlers;
using FeedbackApp.Configuration;

namespace FeedbackApp.Infrastructure
{
    /// <summary>
    /// Simple dependency injection container for the application
    /// </summary>
    public class ServiceContainer : IDisposable
    {
        private readonly Dictionary<Type, object> _services = new();
        private readonly List<IDisposable> _disposableServices = new();
        private bool _disposed = false;

        /// <summary>
        /// Registers a service instance
        /// </summary>
        public void RegisterSingleton<T>(T instance) where T : class
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            
            _services[typeof(T)] = instance;
            
            if (instance is IDisposable disposable)
            {
                _disposableServices.Add(disposable);
            }
        }

        /// <summary>
        /// Registers a service factory
        /// </summary>
        public void RegisterSingleton<T>(Func<ServiceContainer, T> factory) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            
            var instance = factory(this);
            RegisterSingleton(instance);
        }

        /// <summary>
        /// Gets a service instance
        /// </summary>
        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered");
        }

        /// <summary>
        /// Tries to get a service instance
        /// </summary>
        public T? TryGetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            
            return null;
        }

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        public bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Configures all application services
        /// </summary>
        public static ServiceContainer ConfigureServices(AppConfiguration configuration)
        {
            var container = new ServiceContainer();

            // Register configuration
            container.RegisterSingleton(configuration);

            // Register core services
            container.RegisterSingleton(new FeedbackService());
            container.RegisterSingleton(new SnippetService());
            container.RegisterSingleton(new ImageService());
            container.RegisterSingleton(new TimerService());
            container.RegisterSingleton(new AudioService());

            return container;
        }

        /// <summary>
        /// Configures UI-related services that require UI elements
        /// </summary>
        public void ConfigureUIServices(
            System.Windows.Controls.WrapPanel imagesPanel,
            System.Windows.Controls.Border noImagesPlaceholder,
            System.Windows.Controls.Expander imagesExpander,
            System.Windows.Controls.TextBlock imageCountText,
            System.Windows.Controls.TextBlock scrollIndicator,
            System.Windows.Controls.TextBox feedbackTextBox,
            System.Windows.Controls.ComboBox snippetsComboBox,
            System.Windows.Window mainWindow)
        {
            // Register UI Manager
            var uiManager = new UIManager(
                imagesPanel,
                noImagesPlaceholder,
                imagesExpander,
                imageCountText,
                scrollIndicator,
                feedbackTextBox,
                mainWindow);
            RegisterSingleton(uiManager);

            // Register handlers
            var imageService = GetService<ImageService>();
            var images = new System.Collections.Generic.List<Models.ImageItemModel>();
            var imageHandler = new ImageEventHandler(imageService, uiManager, images);
            RegisterSingleton(imageHandler);

            var feedbackService = GetService<FeedbackService>();
            var feedbackActionHandler = new FeedbackActionHandler(feedbackService, mainWindow, feedbackTextBox);
            RegisterSingleton(feedbackActionHandler);

            var snippetService = GetService<SnippetService>();
            var timerService = GetService<TimerService>();
            var snippetHandler = new SnippetEventHandler(snippetService, timerService, feedbackTextBox, snippetsComboBox, mainWindow);
            RegisterSingleton(snippetHandler);

            var scrollHandler = new ScrollIndicatorHandler(scrollIndicator);
            RegisterSingleton(scrollHandler);
        }

        /// <summary>
        /// Disposes all disposable services
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var disposable in _disposableServices)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error disposing service: {ex.Message}");
                    }
                }
                
                _disposableServices.Clear();
                _services.Clear();
                _disposed = true;
            }
        }
    }
}
