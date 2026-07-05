namespace EverythingToolbar.Data
{
    public class SearchResult
    {
        public SearchResult(SearchResultData data)
        {
            Data = data;
        }

        public SearchResultData Data { get; init; } = null!;

        public bool IsFile => Data.IsFile;

        public string FullPathAndFileName => Data.FullPathAndFileName;

        public string Path => Data.Path;

        public string HighlightedPath => Data.HighlightedPath;

        public string FileName => Data.FileName;

        public string HighlightedFileName => Data.HighlightedFileName;

        public string HumanReadableFileSize => Data.HumanReadableFileSize;

        public string HumanReadableDateModified => Data.HumanReadableDateModified;
    }
}