using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using EverythingToolbar.Helpers;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace EverythingToolbar.Data
{
    public class SearchResult : INotifyPropertyChanged
    {
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".bmp",
            ".tiff",
            ".ico",
            ".webp",
        };

        public SearchResult()
        {
        }

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

        public long FileSize => Data.FileSize;

        public FILETIME DateModified => Data.DateModified;

        public string HumanReadableFileSize => Data.HumanReadableFileSize;

        public string HumanReadableDateModified => Data.HumanReadableDateModified;

        private ImageSource? _icon;
        private ImageSource? _previewImage;
        private const int IconSize = 16;
        private const int PreviewIconSize = 64;
        private const int PreviewThumbnailSize = 380;
        public ImageSource? Icon
        {
            get
            {
                if (_icon != null)
                    return _icon;

                // Only the cached extension icon is resolved here; this getter runs on the
                // UI thread for every realized row on every keystroke.
                var useThumbnail = ToolbarSettings.User.IsThumbnailsEnabled && IsImageFile;
                _icon = IconProvider.GetImage(FullPathAndFileName, IsFile, useThumbnail ? IconSize : 32);
                IconLoader.Enqueue(this, useThumbnail);

                return _icon;
            }
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        // Runs on an IconLoader worker thread; may block on the file system
        internal ImageSource? LoadRefinedIcon(bool useThumbnail)
        {
            if (useThumbnail)
            {
                return File.Exists(FullPathAndFileName)
                    ? ThumbnailProvider.GetImage(FullPathAndFileName, IconSize)
                    : null;
            }

            return IconProvider.GetExactImage(FullPathAndFileName, 32);
        }

        public ImageSource? PreviewImage
        {
            get
            {
                if (_previewImage != null)
                    return _previewImage;

                // Load the regular icon first, then upgrade to a thumbnail if one is needed.
                Task.Run(() =>
                {
                    var requiresThumbnail = IsImageFile && File.Exists(FullPathAndFileName);

                    ImageSource? image = IconProvider.GetImage(FullPathAndFileName, IsFile, PreviewIconSize);
                    if (image != null && _previewImage == null)
                        PreviewImage = image;

                    if (requiresThumbnail)
                    {
                        ImageSource? thumbnail = ThumbnailProvider.GetImage(
                            FullPathAndFileName,
                            PreviewThumbnailSize,
                            allowUpscaling: false
                        );
                        if (thumbnail != null)
                            PreviewImage = thumbnail;
                    }
                    else
                    {
                        ImageSource? exactIcon = IconProvider.GetExactImage(FullPathAndFileName, PreviewIconSize);
                        if (exactIcon != null)
                            PreviewImage = exactIcon;
                    }
                });

                return _previewImage;
            }
            private set
            {
                _previewImage = value;
                OnPropertyChanged();
            }
        }

        private bool IsImageFile => ImageExtensions.Contains(System.IO.Path.GetExtension(FullPathAndFileName));

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}