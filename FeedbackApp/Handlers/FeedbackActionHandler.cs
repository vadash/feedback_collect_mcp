using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FeedbackApp.Services;
using FeedbackApp.Helpers;

namespace FeedbackApp.Handlers
{
    /// <summary>
    /// Handles all feedback action button events (Submit, Approve, Reject, AI Decide, Cancel)
    /// </summary>
    public class FeedbackActionHandler
    {
        private readonly FeedbackService _feedbackService;
        private readonly Window _window;
        private readonly TextBox _feedbackTextBox;

        // Properties to store feedback state
        public string? FeedbackText { get; private set; }
        public string ActionType { get; private set; } = "submit";
        public bool IsSubmitSuccess { get; private set; } = false;

        public FeedbackActionHandler(FeedbackService feedbackService, Window window, TextBox feedbackTextBox)
        {
            _feedbackService = feedbackService ?? throw new ArgumentNullException(nameof(feedbackService));
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _feedbackTextBox = feedbackTextBox ?? throw new ArgumentNullException(nameof(feedbackTextBox));
        }

        /// <summary>
        /// Handles the Submit button click
        /// </summary>
        public void HandleSubmit()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_feedbackTextBox.Text))
                {
                    // Instead of showing a message box, use a special action type for empty submissions
                    FeedbackText = "User did not provide any feedback. Please continue without human guidance. Use your best judgment to proceed safely.";
                    ActionType = "no_feedback"; // New action type for empty submissions
                    IsSubmitSuccess = true;
                    _window.Close();
                    return;
                }

                // Store feedback data to be saved when window closes
                FeedbackText = _feedbackTextBox.Text;
                ActionType = "submit"; // Set action type to submit
                IsSubmitSuccess = true;
                
                // Close the window - feedback will be saved in the Closing event handler
                _window.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing feedback: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                IsSubmitSuccess = false;
            }
        }

        /// <summary>
        /// Handles the Approve button click
        /// </summary>
        public void HandleApprove()
        {
            try
            {
                // Set default approval message if text box is empty
                FeedbackText = string.IsNullOrWhiteSpace(_feedbackTextBox.Text) ? 
                    "I approve. Please continue." : _feedbackTextBox.Text;
                
                ActionType = "approve"; // Set action type to approve
                IsSubmitSuccess = true;
                
                // Close the window - feedback will be saved in the Closing event handler
                _window.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing approval: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                IsSubmitSuccess = false;
            }
        }

        /// <summary>
        /// Handles the Reject button click
        /// </summary>
        public void HandleReject()
        {
            try
            {
                // Set default rejection message if text box is empty
                FeedbackText = string.IsNullOrWhiteSpace(_feedbackTextBox.Text) ? 
                    "I reject. Please think of a better solution." : _feedbackTextBox.Text;
                
                ActionType = "reject"; // Set action type to reject
                IsSubmitSuccess = true;
                
                // Close the window - feedback will be saved in the Closing event handler
                _window.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing rejection: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                IsSubmitSuccess = false;
            }
        }

        /// <summary>
        /// Handles the AI Decide button click
        /// </summary>
        public void HandleAiDecide()
        {
            try
            {
                // Set default message if text box is empty
                FeedbackText = string.IsNullOrWhiteSpace(_feedbackTextBox.Text) ? 
                    "I want you judge your own decision and decide, I give you free will to judge and decide. Please consider all implications and potential risks before proceeding." : _feedbackTextBox.Text;
                
                ActionType = "ai_decide"; // Set action type to ai_decide
                IsSubmitSuccess = true;
                
                // Close the window - feedback will be saved in the Closing event handler
                _window.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing AI decision request: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                IsSubmitSuccess = false;
            }
        }

        /// <summary>
        /// Handles the Cancel button click
        /// </summary>
        public void HandleCancel()
        {
            IsSubmitSuccess = false;
            _window.Close();
        }

        /// <summary>
        /// Resets the handler state
        /// </summary>
        public void Reset()
        {
            FeedbackText = null;
            ActionType = "submit";
            IsSubmitSuccess = false;
        }
    }
}
