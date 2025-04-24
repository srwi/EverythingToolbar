using Microsoft.Win32;
using NLog;
using System;
using System.Management;
using System.Security.Principal;

namespace EverythingToolbar.Helpers
{
    public delegate void RegistryChange();
    public delegate void RegistryChangeValue(object newValue);

    internal class RegistryEntry
    {
        public string hive;
        public string keyPath;
        public string valueName;

        public RegistryEntry(string hive, string keyPath, string valueName)
        {
            this.hive = hive;
            this.keyPath = keyPath;
            this.valueName = valueName;
        }

        public object GetValue(object defaultValue = null)
        {
            return Registry.GetValue(hive + @"\" + keyPath, valueName, defaultValue);
        }
    }

    internal class RegistryWatcher
    {
        private readonly ManagementEventWatcher watcher;
        private readonly RegistryEntry target;
        private static readonly ILogger Logger = ToolbarLogger.GetLogger<RegistryWatcher>();

        public event RegistryChange OnChange;
        public event RegistryChangeValue OnChangeValue;

        public RegistryWatcher(RegistryEntry target)
        {
            this.target = target;

            watcher = CreateWatcher();
            watcher.EventArrived += OnEventArrived;

            Start();
        }

        ~RegistryWatcher()
        {
            if (watcher != null)
                watcher.Dispose();
        }

        public void Start()
        {
            try
            {
                watcher.Start();
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Failed to initialize RegistryWatcher for target {target}.");
            }
        }

        public void Stop()
        {
            watcher.Stop();
        }

        private static string EscapeBackticks(string unescaped)
        {
            return unescaped.Replace(@"\", @"\\");
        }

        private ManagementEventWatcher CreateWatcher()
        {
            // Cannot watch HKEY_CURRENT_USER as it is synthetic.
            if (target.hive == "HKEY_CURRENT_USER")
            {
                target.hive = "HKEY_USERS";
                target.keyPath = WindowsIdentity.GetCurrent().User.Value + @"\" + target.keyPath;
            }

            var qu = "SELECT * FROM RegistryValueChangeEvent WHERE " +
                     $"Hive='{target.hive}' " +
                     $"AND KeyPath='{EscapeBackticks(target.keyPath)}' " +
                     $"AND ValueName='{target.valueName}'";

            var query = new WqlEventQuery(qu);
            return new ManagementEventWatcher(query);
        }

        public object GetValue(object defaultValue = null)
        {
            return target.GetValue(defaultValue);
        }

        private void OnEventArrived(object sender, EventArrivedEventArgs e)
        {
            OnChange?.Invoke();

            // Only read value if required
            if (OnChangeValue?.GetInvocationList().Length > 0)
            {
                OnChangeValue?.Invoke(GetValue());
            }
        }
    }
}