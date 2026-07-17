using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace EverythingToolbar.App
{
    public class SettingsProxy : DispatchProxy
    {
        private static readonly Dictionary<string, PropertyInfo> StoreProperties = typeof(IToolbarSettings)
            .GetProperties()
            .ToDictionary(property => property.Name);

        private IToolbarSettings _store = null!;
        private PropertyChangedEventHandler? _propertyChanged;

        public static ISettings Create(IToolbarSettings store)
        {
            var proxy = Create<ISettings, SettingsProxy>();
            ((SettingsProxy)proxy)._store = store;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var name = targetMethod?.Name;
            switch (name)
            {
                case "add_PropertyChanged":
                    _propertyChanged += (PropertyChangedEventHandler?)args?[0];
                    return null;
                case "remove_PropertyChanged":
                    _propertyChanged -= (PropertyChangedEventHandler?)args?[0];
                    return null;
                case not null when name.StartsWith("get_"):
                    return StoreProperty(name.Substring(4)).GetValue(_store);
                case not null when name.StartsWith("set_"):
                    Set(name.Substring(4), args?[0]);
                    return null;
                default:
                    return null;
            }
        }

        private void Set(string propertyName, object? newValue)
        {
            var storeProperty = StoreProperty(propertyName);
            var oldValue = storeProperty.GetValue(_store);
            if (Equals(oldValue, newValue))
                return;

            storeProperty.SetValue(_store, newValue);
            _propertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static PropertyInfo StoreProperty(string settingsPropertyName)
        {
            return StoreProperties[settingsPropertyName];
        }
    }
}
