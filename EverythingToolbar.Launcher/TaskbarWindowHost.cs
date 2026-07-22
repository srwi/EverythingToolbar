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

        private TaskbarWindow? _taskbarWindow;
        private bool _closingTaskbarWindowIntentionally;

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

        public void Create()
        {
            if (!_windowsPolicy.IsTaskbarWindowActive() || _taskbarWindow != null)
                return;

            _taskbarWindow = new TaskbarWindow(_windowsPolicy, _settings);
            _taskbarWindow.Closed += OnTaskbarWindowClosed;

            new WindowInteropHelper(_taskbarWindow).EnsureHandle();
            if (!_taskbarWindow.IsAttachedToTaskbar)
            {
                Logger.Warn("Taskbar window could not attach to the taskbar.");
                Close();
                return;
            }

            _taskbarWindow.Show();
            _controller.SetIconMode(false);
            _searchHost.SetPlacementTarget(_taskbarWindow.PlacementTarget);
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

            // Delay so the new taskbar's UIA tree (Widgets button / tray) is ready for positioning.
            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                Close();
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
