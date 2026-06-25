using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using NLog;

namespace EverythingToolbar.Helpers
{
    public sealed class RegistryValueWatcher : IDisposable
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private const int REG_NOTIFY_CHANGE_LAST_SET = 0x00000004;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegNotifyChangeKeyValue(
            IntPtr hKey,
            bool bWatchSubtree,
            int dwNotifyFilter,
            IntPtr hEvent,
            bool fAsynchronous
        );

        private readonly RegistryKey? _key;
        private readonly AutoResetEvent _changedEvent = new(false);
        private readonly ManualResetEvent _stopEvent = new(false);
        private readonly Thread? _thread;

        public event Action? Changed;

        public RegistryValueWatcher(string subKey)
        {
            _key = Registry.CurrentUser.OpenSubKey(subKey);
            if (_key == null)
            {
                Logger.Warn("Could not open registry key for watching: {SubKey}", subKey);
                return;
            }

            _thread = new Thread(WatchLoop) { IsBackground = true, Name = nameof(RegistryValueWatcher) };
            _thread.Start();
        }

        private void WatchLoop()
        {
            var handles = new WaitHandle[] { _stopEvent, _changedEvent };
            while (_key != null)
            {
                int result = RegNotifyChangeKeyValue(
                    _key.Handle.DangerousGetHandle(),
                    false,
                    REG_NOTIFY_CHANGE_LAST_SET,
                    _changedEvent.SafeWaitHandle.DangerousGetHandle(),
                    true
                );
                if (result != 0)
                {
                    Logger.Warn("RegNotifyChangeKeyValue failed with code {Result}", result);
                    return;
                }

                if (WaitHandle.WaitAny(handles) == 0)
                    return;

                Changed?.Invoke();
            }
        }

        public void Dispose()
        {
            _stopEvent.Set();
            _thread?.Join(TimeSpan.FromSeconds(1));
            _key?.Dispose();
            _stopEvent.Dispose();
            _changedEvent.Dispose();
        }
    }
}