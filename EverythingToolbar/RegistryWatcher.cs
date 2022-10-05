using Microsoft.Win32;
using System;
using System.Management;
using System.Security.Principal;

namespace EverythingToolbar
{
    public delegate void RegistryChange();
    public delegate void RegistryChangeValue(object newValue);

    internal class RegistryWatcher
    {
        private ManagementEventWatcher watcher;
        private string regHive;
        private string regKeyPath;
        private string regValueName;

        public event RegistryChange OnChange;
        public event RegistryChangeValue OnChangeValue;

        public RegistryWatcher(string regHive, string regKeyPath, string regValueName)
        {
            this.regHive = regHive;
            this.regKeyPath = regKeyPath;
            this.regValueName = regValueName;

            this.watcher = CreateQuery();

            this.watcher.EventArrived += Watcher_EventArrived;

            this.watcher.Start();
            //this.watcher.Stop() // TODO: on dispose?
        }

        private static string EscapeBackticks(string unescaped)
        {
            return unescaped.Replace(@"\", @"\\");
        }

        private ManagementEventWatcher CreateQuery()
        {
            // Cannot watch HKEY_CURRENT_USER as it is synthetic.
            if (this.regHive == "HKEY_CURRENT_USER")
            {
                this.regHive = "HKEY_USERS";
                this.regKeyPath = WindowsIdentity.GetCurrent().User.Value + @"\" + this.regKeyPath;
            }

            string qu = "SELECT * FROM RegistryValueChangeEvent WHERE " +
                    $"Hive='{this.regHive}' " +
                    $"AND KeyPath='{EscapeBackticks(this.regKeyPath)}' " +
                    $"AND ValueName='{this.regValueName}'";

            WqlEventQuery query = new WqlEventQuery(qu);
            return new ManagementEventWatcher(query);
        }

        public object GetValue(object defaultValue = null)
        {
            return Registry.GetValue(this.regHive + @"\" + this.regKeyPath, this.regValueName, defaultValue);
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
