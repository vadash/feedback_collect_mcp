using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FeedbackApp.Models;

namespace FeedbackApp.Services
{
    /// <summary>
    /// Service responsible for managing text snippets
    /// </summary>
    public class SnippetService
    {
        private readonly string _snippetsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public SnippetService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "FeedbackApp");
            
            Directory.CreateDirectory(appDataPath);
            _snippetsFilePath = Path.Combine(appDataPath, "snippets.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        /// <summary>
        /// Loads all snippets from storage
        /// </summary>
        public async Task<ObservableCollection<SnippetModel>> LoadSnippetsAsync()
        {
            try
            {
                if (!File.Exists(_snippetsFilePath))
                {
                    return new ObservableCollection<SnippetModel>();
                }

                var json = await File.ReadAllTextAsync(_snippetsFilePath);
                var snippetData = JsonSerializer.Deserialize<List<SnippetData>>(json, _jsonOptions);
                
                var snippets = new ObservableCollection<SnippetModel>();
                if (snippetData != null)
                {
                    foreach (var data in snippetData)
                    {
                        snippets.Add(new SnippetModel
                        {
                            Title = data.Title,
                            Content = data.Content
                        });
                    }
                }

                return snippets;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load snippets: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves all snippets to storage
        /// </summary>
        public async Task SaveSnippetsAsync(IEnumerable<SnippetModel> snippets)
        {
            try
            {
                var snippetData = snippets.Select(s => new SnippetData
                {
                    Title = s.Title,
                    Content = s.Content
                }).ToList();

                var json = JsonSerializer.Serialize(snippetData, _jsonOptions);
                await File.WriteAllTextAsync(_snippetsFilePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save snippets: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Adds a new snippet
        /// </summary>
        public async Task<SnippetModel> AddSnippetAsync(ObservableCollection<SnippetModel> snippets, string title, string content)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty", nameof(title));
            
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Content cannot be empty", nameof(content));

            // Check for duplicate titles
            if (snippets.Any(s => s.Title.Equals(title.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A snippet with this title already exists");

            var newSnippet = new SnippetModel
            {
                Title = title.Trim(),
                Content = content
            };

            snippets.Add(newSnippet);
            await SaveSnippetsAsync(snippets);

            return newSnippet;
        }

        /// <summary>
        /// Updates an existing snippet
        /// </summary>
        public async Task UpdateSnippetAsync(ObservableCollection<SnippetModel> snippets, SnippetModel snippet, string newTitle, string newContent)
        {
            if (string.IsNullOrWhiteSpace(newTitle))
                throw new ArgumentException("Title cannot be empty", nameof(newTitle));
            
            if (string.IsNullOrWhiteSpace(newContent))
                throw new ArgumentException("Content cannot be empty", nameof(newContent));

            // Check for duplicate titles (excluding the current snippet)
            if (snippets.Any(s => s != snippet && s.Title.Equals(newTitle.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A snippet with this title already exists");

            snippet.Title = newTitle.Trim();
            snippet.Content = newContent;

            await SaveSnippetsAsync(snippets);
        }

        /// <summary>
        /// Removes a snippet
        /// </summary>
        public async Task RemoveSnippetAsync(ObservableCollection<SnippetModel> snippets, SnippetModel snippet)
        {
            snippets.Remove(snippet);
            await SaveSnippetsAsync(snippets);
        }

        /// <summary>
        /// Data transfer object for JSON serialization
        /// </summary>
        private class SnippetData
        {
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
        }
    }
}
