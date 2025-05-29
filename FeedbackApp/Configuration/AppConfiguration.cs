using System;

namespace FeedbackApp.Configuration
{
    /// <summary>
    /// Centralized configuration management for the application
    /// </summary>
    public class AppConfiguration
    {
        // Default values
        public const string DefaultWindowTitle = "AI Feedback Collection";
        public const string DefaultPromptText = "Please provide your feedback or describe your issue:";
        public const int DefaultAutoCloseTimeoutSeconds = 30;
        public const double DefaultAudioVolume = 0.5;
        public const int DefaultMaxImageCount = 5;

        // Timer configuration
        public const int TextChangedTimerIntervalMs = 100;
        public const int ExpanderAnimationDelayMs = 300;

        // UI configuration
        public const int ImageThumbnailWidth = 150;
        public const int ImageThumbnailHeight = 180;
        public const int ImageThumbnailMargin = 5;

        // File paths
        public const string SnippetsFileName = "snippets.json";
        public const string TempImageDirectoryName = "temp_images";
        public const string SoundsDirectoryName = "sounds";
        public const string StartupSoundFileName = "stalker_pda_sound.wav";

        // Default messages
        public static class DefaultMessages
        {
            public const string NoFeedback = "User did not provide any feedback. Please continue without human guidance. Use your best judgment to proceed safely.";
            public const string AutoCloseNoFeedback = "User did not provide any feedback. You are now given the free will to judge and decide. Use your best judgment to proceed safely.";
            public const string ApprovalMessage = "I approve. Please continue.";
            public const string RejectionMessage = "I reject. Please think of a better solution.";
            public const string AiDecideMessage = "I want you judge your own decision and decide, I give you free will to judge and decide. Please consider all implications and potential risks before proceeding.";
        }

        // Action types
        public static class ActionTypes
        {
            public const string Submit = "submit";
            public const string Approve = "approve";
            public const string Reject = "reject";
            public const string AiDecide = "ai_decide";
            public const string NoFeedback = "no_feedback";
        }

        // File filters
        public static class FileFilters
        {
            public const string ImageFiles = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
        }

        // Error messages
        public static class ErrorMessages
        {
            public const string InitializationFailed = "Failed to initialize application";
            public const string SnippetAddFailed = "Failed to add snippet";
            public const string SnippetManagementFailed = "Failed to open snippet management";
            public const string ImagePasteFailed = "Failed to paste image";
            public const string ImageAddFailed = "Failed to add image";
            public const string ImageRemoveFailed = "Failed to remove image";
            public const string ImageDropFailed = "Failed to add dropped images";
            public const string FeedbackSaveFailed = "Failed to save feedback";
            public const string ShutdownError = "Error during shutdown";
        }

        // Info messages
        public static class InfoMessages
        {
            public const string MaxImagesReached = "You can attach a maximum of {0} images.";
            public const string MaxImagesReachedNoMore = "You can attach a maximum of {0} images. No more images can be added.";
            public const string NoSnippetsToManage = "No snippets to manage. Create a snippet first.";
            public const string SnippetManagementComingSoon = "Snippet management dialog will be implemented in the next phase.";
        }

        // Window titles
        public static class WindowTitles
        {
            public const string Error = "Error";
            public const string MaximumReached = "Maximum Reached";
            public const string NoSnippets = "No Snippets";
            public const string ComingSoon = "Coming Soon";
        }

        // Properties for runtime configuration
        public string WindowTitle { get; set; } = DefaultWindowTitle;
        public string PromptText { get; set; } = DefaultPromptText;
        public string? OutputFilePath { get; set; }
        public int AutoCloseTimeoutSeconds { get; set; } = DefaultAutoCloseTimeoutSeconds;
        public double AudioVolume { get; set; } = DefaultAudioVolume;

        /// <summary>
        /// Creates a new configuration instance with default values
        /// </summary>
        public static AppConfiguration CreateDefault()
        {
            return new AppConfiguration();
        }

        /// <summary>
        /// Updates configuration from command line arguments
        /// </summary>
        public void UpdateFromCommandLineArgs(string[] args)
        {
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
                OutputFilePath = args[3];
            }
        }
    }
}
