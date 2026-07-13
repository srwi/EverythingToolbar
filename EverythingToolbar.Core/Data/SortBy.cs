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
        {
            "Name",
            "Path",
            "Size",
            "Extension",
            "Type name",
            "Date created",
            "Date modified",
            "Attributes",
            "File list filename",
            "Run count",
            "Date recently changed",
            "Date accessed",
            "Date run",
        };

        public static string ToCliName(this SortBy sortBy) => CliNames[(int)sortBy];

        // SDK sort IDs are 1-based, ascending/descending interleaved (NAME_ASCENDING=1, _DESCENDING=2, PATH_ASCENDING=3, ...).
        public static uint ToEverythingSortType(this SortBy sortBy, bool descending) =>
            (uint)((int)sortBy * 2 + (descending ? 1 : 0) + 1);
    }
}