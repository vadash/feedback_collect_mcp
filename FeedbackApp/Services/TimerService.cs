using System;
using System.Windows.Threading;

namespace FeedbackApp.Services
{
    /// <summary>
    /// Service responsible for managing auto-close timer functionality
    /// </summary>
    public class TimerService
    {
        private DispatcherTimer? _autoCloseTimer;
        private DispatcherTimer? _countdownTimer;
        private int _remainingSeconds;
        private bool _isPaused;

        public const int DefaultTimeoutSeconds = 15;

        /// <summary>
        /// Event fired when the auto-close timer expires
        /// </summary>
        public event EventHandler? AutoCloseTimerExpired;

        /// <summary>
        /// Event fired when the countdown updates
        /// </summary>
        public event EventHandler<CountdownUpdateEventArgs>? CountdownUpdated;

        /// <summary>
        /// Gets whether the timer is currently paused
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Gets the remaining seconds
        /// </summary>
        public int RemainingSeconds => _remainingSeconds;

        /// <summary>
        /// Starts the auto-close timer with the specified timeout
        /// </summary>
        public void StartTimer(int timeoutSeconds = DefaultTimeoutSeconds)
        {
            StopTimer();

            _remainingSeconds = timeoutSeconds;
            _isPaused = false;

            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(timeoutSeconds)
            };
            _autoCloseTimer.Tick += OnAutoCloseTimerTick;
            _autoCloseTimer.Start();

            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += OnCountdownTimerTick;
            _countdownTimer.Start();

            UpdateCountdown();
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        public void StopTimer()
        {
            _autoCloseTimer?.Stop();
            _autoCloseTimer = null;

            _countdownTimer?.Stop();
            _countdownTimer = null;

            _remainingSeconds = 0;
            UpdateCountdown();
        }

        /// <summary>
        /// Resets the timer to the full timeout duration
        /// </summary>
        public void ResetTimer(int timeoutSeconds = DefaultTimeoutSeconds)
        {
            if (_autoCloseTimer != null && !_isPaused)
            {
                _autoCloseTimer.Stop();
                _autoCloseTimer.Interval = TimeSpan.FromSeconds(timeoutSeconds);
                _autoCloseTimer.Start();

                _remainingSeconds = timeoutSeconds;
                UpdateCountdown();
            }
        }

        /// <summary>
        /// Pauses or resumes the timer
        /// </summary>
        public void TogglePause()
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                _autoCloseTimer?.Stop();
                _countdownTimer?.Stop();
            }
            else
            {
                if (_autoCloseTimer != null && _remainingSeconds > 0)
                {
                    _autoCloseTimer.Interval = TimeSpan.FromSeconds(_remainingSeconds);
                    _autoCloseTimer.Start();
                }
                _countdownTimer?.Start();
            }

            UpdateCountdown();
        }

        /// <summary>
        /// Checks if the timer should be active based on conditions
        /// </summary>
        public bool ShouldTimerBeActive(bool hasText)
        {
            return !hasText && !_isPaused;
        }

        private void OnAutoCloseTimerTick(object? sender, EventArgs e)
        {
            if (!_isPaused)
            {
                StopTimer();
                AutoCloseTimerExpired?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnCountdownTimerTick(object? sender, EventArgs e)
        {
            if (!_isPaused && _remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateCountdown();
            }
        }

        private void UpdateCountdown()
        {
            var args = new CountdownUpdateEventArgs
            {
                RemainingSeconds = _remainingSeconds,
                IsPaused = _isPaused,
                IsActive = _autoCloseTimer != null
            };

            CountdownUpdated?.Invoke(this, args);
        }

        /// <summary>
        /// Disposes of the timer resources
        /// </summary>
        public void Dispose()
        {
            StopTimer();
        }
    }

    /// <summary>
    /// Event arguments for countdown updates
    /// </summary>
    public class CountdownUpdateEventArgs : EventArgs
    {
        public int RemainingSeconds { get; set; }
        public bool IsPaused { get; set; }
        public bool IsActive { get; set; }
    }
}
