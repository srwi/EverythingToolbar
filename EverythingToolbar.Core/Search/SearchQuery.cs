using EverythingToolbar.Core.Data;

namespace EverythingToolbar.Core.Search
{
    public sealed record SearchQuery(
        string SearchText,
        SortBy SortBy,
        bool SortDescending,
        bool MatchCase,
        bool MatchPath,
        bool MatchWholeWord,
        bool UseRegex
    );
}