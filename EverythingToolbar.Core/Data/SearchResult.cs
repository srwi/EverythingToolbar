using System;
using EverythingToolbar.Core.Helpers;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EverythingToolbar.Core.Data
{
    public sealed record SearchResult(
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
                ulong fileTime = ((ulong)(uint)DateModified.dwHighDateTime << 32) | (uint)DateModified.dwLowDateTime;
                if (fileTime == 0 || fileTime > (ulong)DateTime.MaxValue.ToFileTimeUtc())
                    return string.Empty;

                try
                {
                    return DateTime.FromFileTime((long)fileTime).ToString("g");
                }
                catch (ArgumentException)
                {
                    return string.Empty;
                }
            }
        }
    }
}
