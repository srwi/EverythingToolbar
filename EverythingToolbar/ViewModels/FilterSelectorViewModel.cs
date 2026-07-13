using EverythingToolbar.Core.Data;

namespace EverythingToolbar.ViewModels
{
    public sealed class FilterSelectorViewModel
    {
        private readonly ISettings _settings;

        public FilterProvider FilterProvider { get; }

        public int MaxTabItems => _settings.MaxTabItems;

        public FilterSelectorViewModel(FilterProvider filterProvider, ISettings settings)
        {
            FilterProvider = filterProvider;
            _settings = settings;
        }

        public int IndexOf(Filter filter) => FilterProvider.Filters.IndexOf(filter);
    }
}
