using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FeedbackApp.Models;

namespace FeedbackApp.Services
{
    /// <summary>
    /// Service responsible for saving and loading feedback data
    /// </summary>
    public class FeedbackService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public FeedbackService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Saves feedback data to the specified file path
        /// </summary>
        /// <param name="feedbackData">The feedback data to save</param>
        /// <param name="outputFilePath">The file path to save to (optional)</param>
        /// <returns>The actual file path where the data was saved</returns>
        public async Task<string> SaveFeedbackAsync(FeedbackData feedbackData, string? outputFilePath = null)
        {
            try
            {
                var actualOutputPath = DetermineOutputPath(outputFilePath);
                
                // Ensure directory exists
                var directory = Path.GetDirectoryName(actualOutputPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize and save
                var jsonData = JsonSerializer.Serialize(feedbackData, _jsonOptions);
                await File.WriteAllTextAsync(actualOutputPath, jsonData);

                return actualOutputPath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save feedback: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Loads feedback data from the specified file path
        /// </summary>
        /// <param name="filePath">The file path to load from</param>
        /// <returns>The loaded feedback data</returns>
        public async Task<FeedbackData?> LoadFeedbackAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var jsonData = await File.ReadAllTextAsync(filePath);
                return JsonSerializer.Deserialize<FeedbackData>(jsonData, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load feedback: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a feedback data object from the provided parameters
        /// </summary>
        public FeedbackData CreateFeedbackData(string text, string actionType, System.Collections.Generic.List<ImageItemModel> images)
        {
            var imageDataList = new System.Collections.Generic.List<ImageData>();
            
            foreach (var img in images)
            {
                imageDataList.Add(new ImageData
                {
                    Path = img.FilePath,
                    Type = img.MimeType
                });
            }

            return new FeedbackData
            {
                Text = text,
                HasImages = images.Count > 0,
                ImageCount = images.Count,
                Images = imageDataList,
                ActionType = actionType,
                Timestamp = DateTime.Now
            };
        }

        private static string DetermineOutputPath(string? outputFilePath)
        {
            if (string.IsNullOrWhiteSpace(outputFilePath))
            {
                return Path.Combine(
                    Path.GetTempPath(), 
                    $"feedback_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            }
            
            return outputFilePath;
        }
    }
}
