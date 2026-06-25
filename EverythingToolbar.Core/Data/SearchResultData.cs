using System;
using System.IO;
using System.Runtime.InteropServices;
using EverythingToolbar.Helpers;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EverythingToolbar.Data
{
    public sealed record SearchResultData(
        string HighlightedPath,
        string HighlightedFileName,
        string FullPathAndFileName,
        bool IsFile,
        long FileSize,
        FILETIME DateModified
    )
    {
        public string Path => System.IO.Path.GetDirectoryName(FullPathAndFileName) ?? "";

        public string FileName => System.IO.Path.GetFileName(FullPathAndFileName);

        public string HumanReadableFileSize
        {
            get
            {
                if (!IsFile || FileSize < 0)
                    return string.Empty;

                return FileSizeFormatter.GetHumanReadableFileSize(FileSize);
            }
        }

        public string HumanReadableDateModified
        {
            get
            {
                long dateModified = ((long)DateModified.dwHighDateTime << 32) | (uint)DateModified.dwLowDateTime;
                return DateTime.FromFileTime(dateModified).ToString("g");
            }
        }
    }
}