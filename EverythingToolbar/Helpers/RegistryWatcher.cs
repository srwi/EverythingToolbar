using System.Management;
using System.Security.Principal;
using Microsoft.Win32;

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

        public event RegistryChange OnChange;
        public event RegistryChangeValue OnChangeValue;

        public RegistryWatcher(RegistryEntry target)
        {
            this.target = target;

            watcher = CreateWatcher();
            watcher.EventArrived += OnEventArrived;

            Start();
        }

        public void Start()
        {
            watcher.Start();
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

            string qu = "SELECT * FROM RegistryValueChangeEvent WHERE " +
                    $"Hive='{target.hive}' " +
                    $"AND KeyPath='{EscapeBackticks(target.keyPath)}' " +
                    $"AND ValueName='{target.valueName}'";

            WqlEventQuery query = new WqlEventQuery(qu);
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
