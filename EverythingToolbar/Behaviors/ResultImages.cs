using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Behaviors
{
    public sealed class ResultImages : ObservableObject
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

        private static readonly ISettings Settings = Ioc.Default.GetRequiredService<ISettings>();

        private const int IconSize = 16;
        private const int PreviewIconSize = 64;
        private const int PreviewThumbnailSize = 380;

        private static readonly PropertyChangedEventArgs IconChangedArgs = new(nameof(Icon));
        private static readonly PropertyChangedEventArgs PreviewImageChangedArgs = new(nameof(PreviewImage));

        private readonly SearchResult _result;
        private ImageSource? _icon;
        private ImageSource? _previewImage;
        private bool _iconLoadStarted;
        private bool _previewLoadStarted;

        internal ResultImages(SearchResult result)
        {
            _result = result;
        }

        public ImageSource? Icon
        {
            get => _icon;
            private set
            {
                _icon = value;
                OnPropertyChanged(IconChangedArgs);
            }
        }

        public ImageSource? PreviewImage
        {
            get => _previewImage;
            private set
            {
                _previewImage = value;
                OnPropertyChanged(PreviewImageChangedArgs);
            }
        }

        public void EnsureIconLoading()
        {
            if (_iconLoadStarted)
                return;
            _iconLoadStarted = true;

            var useThumbnail = Settings.IsThumbnailsEnabled && IsImageFile;
            Icon = IconProvider.GetImage(_result.FullPathAndFileName, _result.IsFile, useThumbnail ? IconSize : 32);
            IconLoader.Enqueue(this, useThumbnail);
        }

        public void EnsurePreviewLoading()
        {
            if (_previewLoadStarted)
                return;
            _previewLoadStarted = true;

            // Load the regular icon first, then upgrade to a thumbnail if one is needed.
            Task.Run(() =>
            {
                var path = _result.FullPathAndFileName;
                var requiresThumbnail = IsImageFile && File.Exists(path);

                ImageSource? image = IconProvider.GetImage(path, _result.IsFile, PreviewIconSize);
                if (image != null && _previewImage == null)
                    PreviewImage = image;

                if (requiresThumbnail)
                {
                    ImageSource? thumbnail = ThumbnailProvider.GetImage(path, PreviewThumbnailSize, allowUpscaling: false);
                    if (thumbnail != null)
                        PreviewImage = thumbnail;
                }
                else
                {
                    ImageSource? exactIcon = IconProvider.GetExactImage(path, PreviewIconSize);
                    if (exactIcon != null)
                        PreviewImage = exactIcon;
                }
            });
        }

        public void SetFixedIcon(ImageSource icon)
        {
            _iconLoadStarted = true;
            Icon = icon;
        }

        // Runs on an IconLoader worker thread; may block on the file system.
        internal ImageSource? LoadRefinedIcon(bool useThumbnail)
        {
            if (useThumbnail)
            {
                return File.Exists(_result.FullPathAndFileName)
                    ? ThumbnailProvider.GetImage(_result.FullPathAndFileName, IconSize)
                    : null;
            }

            return IconProvider.GetExactImage(_result.FullPathAndFileName, 32);
        }

        // Applied by IconLoader on the dispatcher once the refined icon is ready.
        internal void ApplyRefinedIcon(ImageSource icon)
        {
            Icon = icon;
        }

        private bool IsImageFile => ImageExtensions.Contains(Path.GetExtension(_result.FullPathAndFileName));
    }

    internal static class ResultImageCache
    {
        private static readonly ConditionalWeakTable<SearchResult, ResultImages> Table = new();

        public static ResultImages Get(SearchResult result) =>
            Table.GetValue(result, static r => new ResultImages(r));
    }
}
