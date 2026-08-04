using System;
using System.Windows.Interop;
using System.Windows.Threading;
using NLog;

namespace EverythingToolbar.Launcher
{
    internal class TaskbarWindowHost
    {
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<TaskbarWindowHost>();
        private readonly WindowsPolicy _windowsPolicy;
        private readonly ISettings _settings;
        private readonly SearchWindowController _controller;
        private readonly SearchHost _searchHost;

        private const int AttachRetryCount = 5;
        private static readonly TimeSpan AttachRetryInterval = TimeSpan.FromSeconds(2);

        private TaskbarWindow? _taskbarWindow;
        private bool _closingTaskbarWindowIntentionally;
        private int _attachRetriesLeft = AttachRetryCount;

        public TaskbarWindowHost(
            WindowsPolicy windowsPolicy,
            ISettings settings,
            SearchWindowController controller,
            SearchHost searchHost
        )
        {
            _windowsPolicy = windowsPolicy;
            _settings = settings;
            _controller = controller;
            _searchHost = searchHost;
        }

        public bool IsRunning => _taskbarWindow != null;

        // Raised once the taskbar window is known not to be coming up, so that callers can fall back.
        // Until then a missing taskbar window means nothing: the taskbar may simply be mid-build.
        public event Action? AttachAbandoned;

        public void Create()
        {
            if (!_windowsPolicy.IsTaskbarWindowActive() || _taskbarWindow != null)
                return;

            if (!_windowsPolicy.CanEnableTaskbarWindow())
            {
                Logger.Info("Taskbar does not currently host a search box; falling back to the pinned icon.");
                Close();
                ScheduleAttachRetry();
                return;
            }

            _taskbarWindow = new TaskbarWindow(_windowsPolicy, _settings);
            _taskbarWindow.Closed += OnTaskbarWindowClosed;

            new WindowInteropHelper(_taskbarWindow).EnsureHandle();
            if (!_taskbarWindow.IsAttachedToTaskbar)
            {
                Logger.Warn("Taskbar window could not attach to the taskbar.");
                Close();
                ScheduleAttachRetry();
                return;
            }

            _taskbarWindow.Show();
            _controller.SetIconMode(false);
            _searchHost.SetPlacementTarget(_taskbarWindow.PlacementTarget);

            _attachRetriesLeft = AttachRetryCount;
        }

        public void Close()
        {
            if (_taskbarWindow != null)
            {
                _closingTaskbarWindowIntentionally = true;
                try
                {
                    _taskbarWindow.Closed -= OnTaskbarWindowClosed;
                    _taskbarWindow.Close();
                }
                catch
                {
                    // Window may already be destroyed (e.g. explorer restarted); ignore.
                }
                finally
                {
                    _taskbarWindow = null;
                    _closingTaskbarWindowIntentionally = false;
                }
            }

            _controller.SetIconMode(true);
            _searchHost.SetPlacementTarget(null);
        }

        public void HandleExplorerRestart()
        {
            if (!_windowsPolicy.IsTaskbarWindowActive())
                return;

            _attachRetriesLeft = AttachRetryCount;

            // Delay so the new taskbar's UIA tree (Widgets button / tray) is ready for positioning.
            var timer = new DispatcherTimer { Interval = AttachRetryInterval };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Close();
                Create();
            };
            timer.Start();
        }

        // A taskbar that is mid-build looks unsupported or refuses the attach, and at logon the toolbar
        // can easily win the race against explorer. TaskbarCreated only helps when it arrives after we
        // started listening, so give the taskbar a few seconds to appear before settling for the icon.
        private void ScheduleAttachRetry()
        {
            if (_attachRetriesLeft <= 0)
            {
                Logger.Info("Giving up on the taskbar search box; the pinned icon stays the way in.");
                AttachAbandoned?.Invoke();
                return;
            }

            _attachRetriesLeft--;

            var timer = new DispatcherTimer { Interval = AttachRetryInterval };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Create();
            };
            timer.Start();
        }

        private void OnTaskbarWindowClosed(object? sender, EventArgs e)
        {
            if (_closingTaskbarWindowIntentionally)
                return;

            _taskbarWindow = null;
            _controller.SetIconMode(true);
            _searchHost.SetPlacementTarget(null);
        }
    }
}
