
namespace EverythingToolbar.ViewModels
{
    public sealed class FilterSelectorViewModel
    {
        public FilterService FilterService { get; }
        public ISettings Settings { get; }

        public FilterSelectorViewModel(FilterService filterService, ISettings settings)
        {
            FilterService = filterService;
            Settings = settings;
        }
    }
}
