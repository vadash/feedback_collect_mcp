using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using FeedbackApp.Models;

namespace FeedbackApp.Services
{
    /// <summary>
    /// Service responsible for managing image operations
    /// </summary>
    public class ImageService
    {
        private readonly string _tempImageDirectory;
        private const int MaxImages = 5;

        public ImageService()
        {
            _tempImageDirectory = Path.Combine(Path.GetTempPath(), "FeedbackApp_Images");
            Directory.CreateDirectory(_tempImageDirectory);
        }

        /// <summary>
        /// Gets the maximum number of images allowed
        /// </summary>
        public int MaxImageCount => MaxImages;

        /// <summary>
        /// Tries to get an image from the clipboard
        /// </summary>
        public bool TryGetImageFromClipboard(out BitmapSource? image)
        {
            image = null;
            
            try
            {
                if (Clipboard.ContainsImage())
                {
                    image = Clipboard.GetImage();
                    return image != null;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting image from clipboard: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves a clipboard image to a temporary file
        /// </summary>
        public string SaveClipboardImageToFile(BitmapSource image)
        {
            var fileName = $"clipboard_image_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.png";
            var filePath = Path.Combine(_tempImageDirectory, fileName);
            
            try
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
                
                return filePath;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving clipboard image: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates an ImageItemModel from a file path
        /// </summary>
        public ImageItemModel CreateImageItem(string filePath, bool isTemporary = false)
        {
            var fileInfo = new FileInfo(filePath);
            
            return new ImageItemModel
            {
                FilePath = filePath,
                MimeType = DetermineImageType(filePath),
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                IsTemporary = isTemporary
            };
        }

        /// <summary>
        /// Validates if the current image count allows adding more images
        /// </summary>
        public bool CanAddMoreImages(int currentCount)
        {
            return currentCount < MaxImages;
        }

        /// <summary>
        /// Gets the number of available image slots
        /// </summary>
        public int GetAvailableSlots(int currentCount)
        {
            return Math.Max(0, MaxImages - currentCount);
        }

        /// <summary>
        /// Filters a list of file paths to only include valid image files
        /// </summary>
        public List<string> FilterImageFiles(IEnumerable<string> filePaths)
        {
            var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };
            
            return filePaths
                .Where(file => validExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                .ToList();
        }

        /// <summary>
        /// Cleans up temporary image files that are no longer needed
        /// </summary>
        public void CleanupTempImages(IEnumerable<ImageItemModel> currentImages)
        {
            try
            {
                if (!Directory.Exists(_tempImageDirectory))
                    return;

                var currentImagePaths = new HashSet<string>(
                    currentImages.Select(img => img.FilePath),
                    StringComparer.OrdinalIgnoreCase);

                var tempFiles = Directory.GetFiles(_tempImageDirectory);
                
                foreach (var file in tempFiles)
                {
                    if (!currentImagePaths.Contains(file))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to delete temp file {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during temp directory cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Determines the MIME type of an image file based on its extension
        /// </summary>
        public string DetermineImageType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".bmp" => "image/bmp",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Gets the temporary image directory path
        /// </summary>
        public string GetTempImageDirectory() => _tempImageDirectory;
    }
}
