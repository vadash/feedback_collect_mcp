using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FeedbackApp.Models;
using FeedbackApp.Services;
using FeedbackApp.Dialogs;
using FeedbackApp.Helpers;

namespace FeedbackApp.Handlers
{
    /// <summary>
    /// Handles all snippet-related events and operations
    /// </summary>
    public class SnippetEventHandler
    {
        private readonly SnippetService _snippetService;
        private readonly TimerService _timerService;
        private readonly TextBox _feedbackTextBox;
        private readonly ComboBox _snippetsComboBox;
        private readonly Window _parentWindow;

        public SnippetEventHandler(
            SnippetService snippetService, 
            TimerService timerService,
            TextBox feedbackTextBox,
            ComboBox snippetsComboBox,
            Window parentWindow)
        {
            _snippetService = snippetService ?? throw new ArgumentNullException(nameof(snippetService));
            _timerService = timerService ?? throw new ArgumentNullException(nameof(timerService));
            _feedbackTextBox = feedbackTextBox ?? throw new ArgumentNullException(nameof(feedbackTextBox));
            _snippetsComboBox = snippetsComboBox ?? throw new ArgumentNullException(nameof(snippetsComboBox));
            _parentWindow = parentWindow ?? throw new ArgumentNullException(nameof(parentWindow));
        }

        /// <summary>
        /// Handles snippet selection from the combo box
        /// </summary>
        public void HandleSnippetSelection(SnippetModel selectedSnippet)
        {
            if (selectedSnippet == null) return;

            string currentText = _feedbackTextBox.Text;
            string snippetContent = selectedSnippet.Content;

            if (!string.IsNullOrEmpty(currentText) && !currentText.EndsWith(Environment.NewLine))
            {
                _feedbackTextBox.AppendText(Environment.NewLine);
            }
            _feedbackTextBox.AppendText(snippetContent);
            _feedbackTextBox.CaretIndex = _feedbackTextBox.Text.Length;
            _feedbackTextBox.ScrollToEnd();

            // Reset the combo box selection
            _snippetsComboBox.SelectedIndex = -1;

            // Set focus back to the text box
            _feedbackTextBox.Focus();
        }

        /// <summary>
        /// Handles adding a new snippet
        /// </summary>
        public async Task HandleAddSnippet(ObservableCollection<SnippetModel> snippets)
        {
            try
            {
                // Pause timer while dialog is open
                _timerService.TogglePause();

                var dialog = new SnippetDialog(_parentWindow);
                var result = dialog.ShowDialog();

                if (result == true)
                {
                    var newSnippet = dialog.GetSnippet();
                    await _snippetService.AddSnippetAsync(snippets, newSnippet.Title, newSnippet.Content);
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to add snippet: {ex.Message}");
            }
            finally
            {
                // Resume timer if conditions are met
                if (_timerService.ShouldTimerBeActive(string.IsNullOrWhiteSpace(_feedbackTextBox.Text)))
                {
                    _timerService.TogglePause();
                }
            }
        }

        /// <summary>
        /// Handles managing existing snippets
        /// </summary>
        public void HandleManageSnippets(ObservableCollection<SnippetModel> snippets)
        {
            try
            {
                // Pause timer while dialog is open
                _timerService.TogglePause();

                // Check if we have any snippets
                if (snippets.Count == 0)
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
                if (_timerService.ShouldTimerBeActive(string.IsNullOrWhiteSpace(_feedbackTextBox.Text)))
                {
                    _timerService.TogglePause();
                }
            }
        }
    }
}
