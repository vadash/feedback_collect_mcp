using System.ComponentModel;

namespace FeedbackApp.Models
{
    /// <summary>
    /// Enhanced model for text snippets with validation
    /// </summary>
    public class SnippetModel : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        private string _content = string.Empty;

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value?.Trim() ?? string.Empty;
                    OnPropertyChanged(nameof(Title));
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value ?? string.Empty;
                    OnPropertyChanged(nameof(Content));
                    OnPropertyChanged(nameof(IsValid));
                }
            }
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(Content);

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return Title;
        }

        /// <summary>
        /// Creates a copy of this snippet
        /// </summary>
        public SnippetModel Clone()
        {
            return new SnippetModel
            {
                Title = this.Title,
                Content = this.Content
            };
        }
    }
}
