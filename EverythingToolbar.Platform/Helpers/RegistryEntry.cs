using Microsoft.Win32;

namespace EverythingToolbar.Platform.Helpers
{
    public class RegistryEntry(string hive, string keyPath, string valueName)
    {
        public readonly string Hive = hive;
        public readonly string KeyPath = keyPath;
        public readonly string ValueName = valueName;

        public object? GetValue(object? defaultValue = null)
        {
            return Registry.GetValue(Hive + @"\" + KeyPath, ValueName, defaultValue);
        }
    }
}
