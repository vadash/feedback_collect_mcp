using System.Windows;

namespace FeedbackApp.Models
{
    /// <summary>
    /// Enhanced model for image items with additional metadata
    /// </summary>
    public class ImageItemModel
    {
        public string FilePath { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public UIElement? UiElement { get; set; }
        public long FileSize { get; set; }
        public bool IsTemporary { get; set; }

        /// <summary>
        /// Gets the display name for the image
        /// </summary>
        public string DisplayName => System.IO.Path.GetFileName(FilePath);

        /// <summary>
        /// Gets the file size in a human-readable format
        /// </summary>
        public string FileSizeDisplay
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024:F1} KB";
                return $"{FileSize / (1024 * 1024):F1} MB";
            }
        }

        /// <summary>
        /// Determines if the file is a valid image based on extension
        /// </summary>
        public bool IsValidImage
        {
            get
            {
                var extension = System.IO.Path.GetExtension(FilePath).ToLowerInvariant();
                return extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif";
            }
        }
    }
}
