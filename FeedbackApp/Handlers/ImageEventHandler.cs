using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using FeedbackApp.Models;
using FeedbackApp.Services;
using FeedbackApp.Managers;
using FeedbackApp.Helpers;

namespace FeedbackApp.Handlers
{
    /// <summary>
    /// Handles all image-related events and operations
    /// </summary>
    public class ImageEventHandler
    {
        private readonly ImageService _imageService;
        private readonly UIManager _uiManager;
        private readonly List<ImageItemModel> _images;

        public ImageEventHandler(ImageService imageService, UIManager uiManager, List<ImageItemModel> images)
        {
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _uiManager = uiManager ?? throw new ArgumentNullException(nameof(uiManager));
            _images = images ?? throw new ArgumentNullException(nameof(images));
        }

        /// <summary>
        /// Handles image paste from clipboard
        /// </summary>
        public void HandleImagePaste()
        {
            try
            {
                // Try to extract image from clipboard
                if (_imageService.TryGetImageFromClipboard(out BitmapSource? image) && image != null)
                {
                    // Check if we can add more images
                    if (!_imageService.CanAddMoreImages(_images.Count))
                    {
                        MessageBox.Show($"You can attach a maximum of {_imageService.MaxImageCount} images.",
                            "Maximum Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Save the image and create an image item
                    string imageFilePath = _imageService.SaveClipboardImageToFile(image);
                    var imageItem = _imageService.CreateImageItem(imageFilePath, isTemporary: true);

                    // Add to our collection and UI
                    AddImageItem(imageItem);
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to paste image: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles adding image from file dialog
        /// </summary>
        public void HandleAddImageFromFile()
        {
            try
            {
                // Check if we can add more images
                if (!_imageService.CanAddMoreImages(_images.Count))
                {
                    MessageBox.Show($"You can attach a maximum of {_imageService.MaxImageCount} images.",
                        "Maximum Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select Image",
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var imageItem = _imageService.CreateImageItem(openFileDialog.FileName);
                    AddImageItem(imageItem);
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to add image: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles drag and drop of image files
        /// </summary>
        public void HandleImageDrop(DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    var imageFiles = _imageService.FilterImageFiles(files);

                    if (imageFiles.Count > 0)
                    {
                        var availableSlots = _imageService.GetAvailableSlots(_images.Count);

                        if (availableSlots <= 0)
                        {
                            MessageBox.Show($"You can attach a maximum of {_imageService.MaxImageCount} images. No more images can be added.",
                                "Maximum Reached", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        var imagesToAdd = Math.Min(availableSlots, imageFiles.Count);

                        for (int i = 0; i < imagesToAdd; i++)
                        {
                            var imageItem = _imageService.CreateImageItem(imageFiles[i]);
                            AddImageItem(imageItem);
                        }

                        e.Handled = true;
                    }
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to add dropped images: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles drag over validation for image files
        /// </summary>
        public void HandleImageDragOver(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var imageFiles = _imageService.FilterImageFiles(files);

                if (imageFiles.Count > 0)
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// Adds an image item to the collection and UI
        /// </summary>
        private void AddImageItem(ImageItemModel imageItem)
        {
            // Add to our collection
            _images.Add(imageItem);

            // Add to UI
            _uiManager.AddImageToPanel(imageItem, RemoveImageItem);

            // Update UI state
            UpdateImageUIState();
        }

        /// <summary>
        /// Removes an image item from the collection and UI
        /// </summary>
        public void RemoveImageItem(ImageItemModel imageItem)
        {
            try
            {
                // Remove from UI
                _uiManager.RemoveImageFromPanel(imageItem);

                // Remove from our collection
                _images.Remove(imageItem);

                // Update UI state
                UpdateImageUIState();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Failed to remove image: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the image-related UI state
        /// </summary>
        private void UpdateImageUIState()
        {
            _uiManager.UpdateImageCount(_images.Count, _imageService.MaxImageCount);
            _uiManager.UpdateNoImagesPlaceholder(_images.Count > 0);
        }
    }
}
