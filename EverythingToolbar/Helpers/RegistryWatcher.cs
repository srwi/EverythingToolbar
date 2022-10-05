using Microsoft.Win32;
using System;
using System.Management;
using System.Security.Principal;

namespace EverythingToolbar
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
            return Registry.GetValue(this.hive + @"\" + this.keyPath, this.valueName, defaultValue);
        }
    }

    internal class RegistryWatcher
    {
        private ManagementEventWatcher watcher;
        private RegistryEntry target;

        public event RegistryChange OnChange;
        public event RegistryChangeValue OnChangeValue;

        public RegistryWatcher(string regHive, string regKeyPath, string regValueName) 
            : this(new RegistryEntry(regHive, regKeyPath, regValueName))
        {
        }
        public RegistryWatcher(RegistryEntry target)
        {
            this.target = target;
            this.watcher = CreateQuery();

            this.watcher.EventArrived += Watcher_EventArrived;

            this.Start();
        }

        public void Start()
        {
            this.watcher.Start();
        }

        public void Stop()
        {
            this.watcher.Stop();
        }

        private static string EscapeBackticks(string unescaped)
        {
            return unescaped.Replace(@"\", @"\\");
        }

        private ManagementEventWatcher CreateQuery()
        {
            // Cannot watch HKEY_CURRENT_USER as it is synthetic.
            if (this.target.hive == "HKEY_CURRENT_USER")
            {
                this.target.hive = "HKEY_USERS";
                this.target.keyPath = WindowsIdentity.GetCurrent().User.Value + @"\" + this.target.keyPath;
            }

            string qu = "SELECT * FROM RegistryValueChangeEvent WHERE " +
                    $"Hive='{this.target.hive}' " +
                    $"AND KeyPath='{EscapeBackticks(this.target.keyPath)}' " +
                    $"AND ValueName='{this.target.valueName}'";

            WqlEventQuery query = new WqlEventQuery(qu);
            return new ManagementEventWatcher(query);
        }

        public object GetValue(object defaultValue = null)
        {
            return target.GetValue(defaultValue);
        }

        private void Watcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            this.OnChange?.Invoke();

            // No need to read value when nobody wants it
            if (this.OnChangeValue?.GetInvocationList().Length > 0)
            { 
                this.OnChangeValue?.Invoke(this.GetValue());
            }
        }
    }
}
