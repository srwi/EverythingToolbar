using EverythingToolbar.Core.Data;

namespace EverythingToolbar.ViewModels
{
    public sealed class FilterSelectorViewModel
    {
        private readonly ISettings _settings;

        public FilterService FilterService { get; }

        public int MaxTabItems => _settings.MaxTabItems;

        public FilterSelectorViewModel(FilterService filterService, ISettings settings)
        {
            FilterService = filterService;
            _settings = settings;
        }

        public int IndexOf(Filter filter) => FilterService.Filters.IndexOf(filter);
    }
}
