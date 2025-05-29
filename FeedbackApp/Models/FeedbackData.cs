using System;
using System.Collections.Generic;

namespace FeedbackApp.Models
{
    /// <summary>
    /// Represents the complete feedback data structure
    /// </summary>
    public class FeedbackData
    {
        public string Text { get; set; } = string.Empty;
        public bool HasImages { get; set; }
        public int ImageCount { get; set; }
        public List<ImageData> Images { get; set; } = new List<ImageData>();
        public string ActionType { get; set; } = "submit";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Represents image data in feedback
    /// </summary>
    public class ImageData
    {
        public string Path { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
