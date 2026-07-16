using EverythingToolbar.Data;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.ViewModels
{
    public sealed class FilterSelectorViewModel
    {
        public FilterLoader FilterLoader { get; }
        public ISettings Settings { get; }

        public FilterSelectorViewModel(FilterLoader filterLoader, ISettings settings)
        {
            FilterLoader = filterLoader;
            Settings = settings;
        }
    }
}
