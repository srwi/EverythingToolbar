namespace EverythingToolbar.Core.Data
{
    public enum SortBy
    {
        Name,
        Path,
        Size,
        Extension,
        TypeName,
        DateCreated,
        DateModified,
        Attributes,
        FileListFilename,
        RunCount,
        DateRecentlyChanged,
        DateAccessed,
        DateRun,
    }

    public static class SortByInfo
    {
        private static readonly string[] CliNames =
        [
            "Name",
            "Path",
            "Size",
            "Extension",
            "Type Name",
            "Date Created",
            "Date Modified",
            "Attributes",
            "File List Filename",
            "Run Count",
            "Date Recently Changed",
            "Date Accessed",
            "Date Run",
        ];

        public static string ToCliName(this SortBy sortBy) => CliNames[(int)sortBy];

        public static SortBy? FromFilterSortName(string name)
        {
            return name switch
            {
                "Name" => SortBy.Name,
                "Path" => SortBy.Path,
                "Size" => SortBy.Size,
                "Extension" => SortBy.Extension,
                "Type Name" => SortBy.TypeName,
                "Date Created" => SortBy.DateCreated,
                "Date Modified" => SortBy.DateModified,
                "Attributes" => SortBy.Attributes,
                "File List Filename" => SortBy.FileListFilename,
                "Run Count" => SortBy.RunCount,
                "Date Recently Changed" => SortBy.DateRecentlyChanged,
                "Date Accessed" => SortBy.DateAccessed,
                "Date Run" => SortBy.DateRun,
                _ => null,
            };
        }

        // SDK sort IDs are 1-based, ascending/descending interleaved (NAME_ASCENDING=1, _DESCENDING=2, PATH_ASCENDING=3, ...)
        public static uint ToEverythingSortType(this SortBy sortBy, bool descending) =>
            (uint)((int)sortBy * 2 + (descending ? 1 : 0) + 1);
    }
}
