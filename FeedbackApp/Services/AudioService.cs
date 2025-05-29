using System;
using System.IO;
using System.Windows.Media;

namespace FeedbackApp.Services
{
    /// <summary>
    /// Service responsible for managing audio playback functionality
    /// </summary>
    public class AudioService : IDisposable
    {
        private MediaPlayer? _mediaPlayer;
        private bool _disposed = false;

        public AudioService()
        {
            _mediaPlayer = new MediaPlayer();
        }

        /// <summary>
        /// Plays the startup sound at the specified volume
        /// </summary>
        /// <param name="volume">Volume level (0.0 to 1.0)</param>
        public void PlayStartupSound(double volume = 0.5)
        {
            try
            {
                if (_mediaPlayer == null)
                {
                    System.Diagnostics.Debug.WriteLine("AudioService: MediaPlayer is null");
                    return;
                }

                // Get the path to the sound file
                string soundFilePath = GetSoundFilePath("stalker_pda_sound.wav");
                
                if (!File.Exists(soundFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"AudioService: Sound file not found at {soundFilePath}");
                    return;
                }

                // Set volume (clamp between 0.0 and 1.0)
                _mediaPlayer.Volume = Math.Max(0.0, Math.Min(1.0, volume));
                
                // Load and play the sound
                _mediaPlayer.Open(new Uri(soundFilePath, UriKind.Absolute));
                _mediaPlayer.Play();
                
                System.Diagnostics.Debug.WriteLine($"AudioService: Playing startup sound at {volume * 100}% volume");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AudioService: Error playing startup sound: {ex.Message}");
                // Don't throw - audio failure shouldn't crash the app
            }
        }

        /// <summary>
        /// Plays a sound file at the specified volume
        /// </summary>
        /// <param name="fileName">Name of the sound file in the sounds directory</param>
        /// <param name="volume">Volume level (0.0 to 1.0)</param>
        public void PlaySound(string fileName, double volume = 0.5)
        {
            try
            {
                if (_mediaPlayer == null)
                {
                    System.Diagnostics.Debug.WriteLine("AudioService: MediaPlayer is null");
                    return;
                }

                string soundFilePath = GetSoundFilePath(fileName);
                
                if (!File.Exists(soundFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"AudioService: Sound file not found at {soundFilePath}");
                    return;
                }

                // Set volume (clamp between 0.0 and 1.0)
                _mediaPlayer.Volume = Math.Max(0.0, Math.Min(1.0, volume));
                
                // Load and play the sound
                _mediaPlayer.Open(new Uri(soundFilePath, UriKind.Absolute));
                _mediaPlayer.Play();
                
                System.Diagnostics.Debug.WriteLine($"AudioService: Playing {fileName} at {volume * 100}% volume");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AudioService: Error playing sound {fileName}: {ex.Message}");
                // Don't throw - audio failure shouldn't crash the app
            }
        }

        /// <summary>
        /// Stops any currently playing audio
        /// </summary>
        public void Stop()
        {
            try
            {
                _mediaPlayer?.Stop();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AudioService: Error stopping audio: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the full path to a sound file in the sounds directory
        /// </summary>
        /// <param name="fileName">Name of the sound file</param>
        /// <returns>Full path to the sound file</returns>
        private string GetSoundFilePath(string fileName)
        {
            // Get the directory where the executable is located
            string? exeDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            if (string.IsNullOrEmpty(exeDirectory))
            {
                // Fallback to current directory
                exeDirectory = Directory.GetCurrentDirectory();
            }

            // Look for sounds directory relative to executable
            string soundsDirectory = Path.Combine(exeDirectory, "sounds");
            
            // If not found, try relative to the project root (for development)
            if (!Directory.Exists(soundsDirectory))
            {
                // Go up directories to find the project root
                string? projectRoot = FindProjectRoot(exeDirectory);
                if (!string.IsNullOrEmpty(projectRoot))
                {
                    soundsDirectory = Path.Combine(projectRoot, "FeedbackApp", "sounds");
                }
            }

            return Path.Combine(soundsDirectory, fileName);
        }

        /// <summary>
        /// Finds the project root directory by looking for characteristic files
        /// </summary>
        /// <param name="startDirectory">Directory to start searching from</param>
        /// <returns>Project root directory or null if not found</returns>
        private string? FindProjectRoot(string startDirectory)
        {
            string? currentDir = startDirectory;
            
            while (!string.IsNullOrEmpty(currentDir))
            {
                // Look for characteristic files that indicate project root
                if (File.Exists(Path.Combine(currentDir, "FeedbackApp.sln")) ||
                    File.Exists(Path.Combine(currentDir, "package.json")) ||
                    Directory.Exists(Path.Combine(currentDir, "FeedbackApp")))
                {
                    return currentDir;
                }
                
                currentDir = Path.GetDirectoryName(currentDir);
            }
            
            return null;
        }

        /// <summary>
        /// Disposes of the AudioService and releases resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    try
                    {
                        _mediaPlayer?.Stop();
                        _mediaPlayer?.Close();
                        _mediaPlayer = null;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"AudioService: Error during disposal: {ex.Message}");
                    }
                }
                _disposed = true;
            }
        }
    }
}
