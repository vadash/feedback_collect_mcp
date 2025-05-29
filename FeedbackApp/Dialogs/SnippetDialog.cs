using System.Windows;
using System.Windows.Controls;
using FeedbackApp.Helpers;
using FeedbackApp.Models;

namespace FeedbackApp.Dialogs
{
    /// <summary>
    /// Dialog for adding or editing snippets
    /// </summary>
    public class SnippetDialog
    {
        private readonly Window _dialog;
        private readonly TextBox _titleTextBox;
        private readonly TextBox _contentTextBox;
        private readonly SnippetModel _snippet;
        private readonly bool _isEditMode;

        public SnippetDialog(Window? owner = null, SnippetModel? existingSnippet = null)
        {
            _isEditMode = existingSnippet != null;
            _snippet = existingSnippet ?? new SnippetModel();

            var title = _isEditMode ? "Edit Snippet" : "Add New Snippet";
            _dialog = DialogHelper.CreateStandardDialog(title, 450, 280, owner);

            var mainBorder = DialogHelper.CreateMainBorder();
            var grid = CreateDialogGrid();

            // Create title input
            var (titleLabel, titleBorder, titleTextBox) = DialogHelper.CreateLabeledTextInput(
                "Title:", 
                _snippet.Title, 
                false, 
                "Enter snippet title here");
            _titleTextBox = titleTextBox;

            // Create content input
            var (contentLabel, contentBorder, contentTextBox) = DialogHelper.CreateLabeledTextInput(
                "Content:", 
                _snippet.Content, 
                true, 
                "Type your snippet content here");
            _contentTextBox = contentTextBox;

            // Create buttons
            var buttonPanel = DialogHelper.CreateButtonPanel();
            var (saveButton, cancelButton) = CreateButtons();

            // Layout the grid
            LayoutGrid(grid, titleLabel, titleBorder, contentLabel, contentBorder, buttonPanel);

            // Add buttons to panel
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);

            // Set up the dialog
            mainBorder.Child = grid;
            _dialog.Content = mainBorder;

            // Wire up events
            saveButton.Click += OnSaveClick;
            cancelButton.Click += (s, e) => _dialog.DialogResult = false;
        }

        /// <summary>
        /// Shows the dialog and returns the result
        /// </summary>
        public bool? ShowDialog()
        {
            return _dialog.ShowDialog();
        }

        /// <summary>
        /// Gets the snippet data from the dialog
        /// </summary>
        public SnippetModel GetSnippet()
        {
            return new SnippetModel
            {
                Title = _titleTextBox.Text.Trim(),
                Content = _contentTextBox.Text
            };
        }

        private Grid CreateDialogGrid()
        {
            var grid = new Grid { Margin = new Thickness(20) };
            
            // Define rows
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) }); // Spacer
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            // Define columns
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            return grid;
        }

        private (Button saveButton, Button cancelButton) CreateButtons()
        {
            var saveButtonStyle = Application.Current.Resources["AccentButton"] as Style;
            var cancelButtonStyle = Application.Current.Resources["ModernButton"] as Style;

            var saveButton = DialogHelper.CreateStandardButton(
                _isEditMode ? "Save Changes" : "Save",
                _isEditMode ? 120 : 100,
                30,
                saveButtonStyle,
                isDefault: true);

            var cancelButton = DialogHelper.CreateStandardButton(
                "Cancel",
                100,
                30,
                cancelButtonStyle,
                isCancel: true);

            return (saveButton, cancelButton);
        }

        private static void LayoutGrid(
            Grid grid, 
            TextBlock titleLabel, 
            Border titleBorder, 
            TextBlock contentLabel, 
            Border contentBorder, 
            StackPanel buttonPanel)
        {
            // Title row
            Grid.SetRow(titleLabel, 0);
            Grid.SetColumn(titleLabel, 0);
            Grid.SetRow(titleBorder, 0);
            Grid.SetColumn(titleBorder, 1);

            // Content rows
            Grid.SetRow(contentLabel, 2);
            Grid.SetColumn(contentLabel, 0);
            Grid.SetRowSpan(contentLabel, 2);
            Grid.SetRow(contentBorder, 2);
            Grid.SetColumn(contentBorder, 1);
            Grid.SetRowSpan(contentBorder, 2);

            // Button row
            Grid.SetRow(buttonPanel, 4);
            Grid.SetColumn(buttonPanel, 0);
            Grid.SetColumnSpan(buttonPanel, 2);

            // Add to grid
            grid.Children.Add(titleLabel);
            grid.Children.Add(titleBorder);
            grid.Children.Add(contentLabel);
            grid.Children.Add(contentBorder);
            grid.Children.Add(buttonPanel);
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            var title = _titleTextBox.Text.Trim();
            var content = _contentTextBox.Text;

            if (string.IsNullOrWhiteSpace(title))
            {
                DialogHelper.ShowValidationError("Please enter a title for the snippet.", "Required Field");
                _titleTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                DialogHelper.ShowValidationError("Please enter content for the snippet.", "Required Field");
                _contentTextBox.Focus();
                return;
            }

            _dialog.DialogResult = true;
        }
    }
}
