using Microsoft.Win32;

namespace EverythingToolbar.Helpers
{
    internal class RegistryEntry(string hive, string keyPath, string valueName)
    {
        public string Hive = hive;
        public string KeyPath = keyPath;
        public string ValueName = valueName;

        public object? GetValue(object? defaultValue = null)
        {
            return Registry.GetValue(Hive + @"\" + KeyPath, ValueName, defaultValue);
        }
    }
}
