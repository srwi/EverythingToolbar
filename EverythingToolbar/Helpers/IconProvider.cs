using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EverythingToolbar.Helpers
{
    internal static class ImageScalingHelper
    {
        public static int GetScaledSize(int logicalSize)
        {
            double dpi = GetDpiForSystem();
            if (dpi < 96)
                dpi = 96;
            return (int)Math.Ceiling(logicalSize * dpi / 96.0);
        }

        public static BitmapSource SetLogicalSize(BitmapSource source, int logicalSize, bool downOnly = false)
        {
            double targetLogicalSize = logicalSize;
            if (downOnly)
            {
                double systemDpi = GetDpiForSystem();
                if (systemDpi < 96)
                    systemDpi = 96;

                double nativeLogicalSize = source.PixelWidth * 96.0 / systemDpi;
                targetLogicalSize = Math.Min(logicalSize, nativeLogicalSize);
            }

            if (targetLogicalSize <= 0)
                return source;

            double targetDpi = source.PixelWidth * 96.0 / targetLogicalSize;
            if (Math.Abs(source.DpiX - targetDpi) < 0.1)
                return source;

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            var format = source.Format;
            int stride = (width * format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[stride * height];
            source.CopyPixels(pixels, stride, 0);
            var result = BitmapSource.Create(
                width,
                height,
                targetDpi,
                targetDpi,
                format,
                source.Palette,
                pixels,
                stride
            );
            result.Freeze();
            return result;
        }

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();
    }

    public static class ThumbnailProvider
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory shellItem
        );

        [ComImport]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage([In] Size size, [In] int flags, [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Size
        {
            public int cx;
            public int cy;
        }

        private const int SiigbfResizetofit = 0x00;

        public static ImageSource? GetImage(string filePath, int imageSize, bool allowUpscaling = true)
        {
            IShellItemImageFactory? imageFactory = null;
            try
            {
                int scaledSize = ImageScalingHelper.GetScaledSize(imageSize);

                Guid shellItemImageFactoryGuid = new("BCC18B79-BA16-442F-80C4-8A59C30C463B");
                SHCreateItemFromParsingName(filePath, IntPtr.Zero, shellItemImageFactoryGuid, out imageFactory);

                Size size = new() { cx = scaledSize, cy = scaledSize };
                imageFactory.GetImage(size, SiigbfResizetofit, out IntPtr hBitmap);

                try
                {
                    var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()
                    );
                    imageSource.Freeze();
                    return ImageScalingHelper.SetLogicalSize(imageSource, imageSize, downOnly: !allowUpscaling);
                }
                finally
                {
                    DeleteObject(hBitmap);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (imageFactory != null && Marshal.IsComObject(imageFactory))
                    Marshal.ReleaseComObject(imageFactory);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }

    public static class IconProvider
    {
        private static readonly ConcurrentDictionary<string, ImageSource> IconByIndexAndScaleCache = new();
        private static readonly ConcurrentDictionary<string, int> ExtensionToIndexMap = new();

        private static int _fallbackDirectoryIconIndex;

        static IconProvider()
        {
            _fallbackDirectoryIconIndex = GetIconIndex("asdf1234", IconIndexType.DirectoryName);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct Shfileinfo
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            ref Shfileinfo psfi,
            uint cbSizeFileInfo,
            uint uFlags
        );

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint ShgfiSmallicon = 0x000000001;
        private const uint ShgfiSysiconindex = 0x000004000;
        private const uint ShgfiUsefileattributes = 0x000000010;
        private const uint FileAttributeNormal = 0x00000080;
        private const uint FileAttributeDirectory = 0x00000010;

        public static ImageSource? GetImage(
            string path,
            bool isFile,
            int iconSize,
            Action<ImageSource>? onUpdated = null
        )
        {
            int iconIndexByExt;
            if (isFile)
            {
                var extension = Path.GetExtension(path);
                if (!ExtensionToIndexMap.TryGetValue(extension, out iconIndexByExt))
                {
                    iconIndexByExt = GetIconIndex($"asdf1234.{extension}", IconIndexType.ByFileName);
                    ExtensionToIndexMap.TryAdd(extension, iconIndexByExt);
                }
            }
            else
            {
                iconIndexByExt = _fallbackDirectoryIconIndex;
            }

            var iconByIndexAndScaleCacheKey = iconIndexByExt + "_" + iconSize;
            if (!IconByIndexAndScaleCache.TryGetValue(iconByIndexAndScaleCacheKey, out var iconByExtAndScale))
            {
                iconByExtAndScale = GetIconFromSystemImageList(iconIndexByExt, iconSize);
                if (iconByExtAndScale != null)
                {
                    IconByIndexAndScaleCache.TryAdd(iconByIndexAndScaleCacheKey, iconByExtAndScale);
                }
            }

            if (onUpdated != null)
            {
                Task.Run(() =>
                {
                    ImageSource? exactIcon = GetExactImage(path, iconSize);
                    if (exactIcon != null)
                        onUpdated.Invoke(exactIcon);
                });
            }

            return iconByExtAndScale;
        }

        // Can block on network paths (SHGetFileInfo touches the file system) - do not call on the UI thread.
        public static ImageSource? GetExactImage(string path, int iconSize)
        {
            int exactIconIndex = GetIconIndex(path, IconIndexType.ByFilePath);
            var exactIconCacheKey = exactIconIndex + "_" + iconSize;
            if (IconByIndexAndScaleCache.TryGetValue(exactIconCacheKey, out var cachedExactIcon))
                return cachedExactIcon;

            ImageSource? exactIcon = GetIconFromSystemImageList(exactIconIndex, iconSize);
            if (exactIcon != null)
                IconByIndexAndScaleCache.TryAdd(exactIconCacheKey, exactIcon);

            return exactIcon;
        }

        private static int GetIconIndex(string path, IconIndexType indexType)
        {
            Shfileinfo shfi = new();
            uint flags = ShgfiSysiconindex | ShgfiSmallicon;
            uint fileAttributes = 0;
            if (indexType == IconIndexType.ByFileName)
            {
                fileAttributes = FileAttributeNormal;
                flags |= ShgfiUsefileattributes;
            }
            else if (indexType == IconIndexType.DirectoryName)
            {
                fileAttributes = FileAttributeDirectory;
                flags |= ShgfiUsefileattributes;
            }
            SHGetFileInfo(path, fileAttributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
            return shfi.iIcon;
        }

        enum IconIndexType
        {
            ByFileName,
            ByFilePath,
            DirectoryName,
        }

        private const int IldTransparent = 0x00000001;
        private const int ShilLarge = 0;
        private const int ShilSmall = 1;
        private const int ShilExtralarge = 2;
        private const int ShilJumbo = 4;

        private static ImageSource? GetIconFromSystemImageList(int iconIndex, int iconSize)
        {
            int scaledSize = ImageScalingHelper.GetScaledSize(iconSize);

            IImageList? imageList = null;
            try
            {
                int imageListType = GetImageListType(scaledSize);
                Guid iImageListGuid = new("46EB5926-582E-4017-9FDF-E8998DAA0950");
                int hr = SHGetImageList(imageListType, iImageListGuid, out imageList);
                if (hr != 0)
                    return null;

                hr = imageList.GetIcon(iconIndex, IldTransparent, out IntPtr hIcon);
                if (hr != 0 || hIcon == IntPtr.Zero)
                    return null;

                try
                {
                    var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions()
                    );
                    imageSource.Freeze();
                    return ImageScalingHelper.SetLogicalSize(imageSource, iconSize);
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                if (imageList != null && Marshal.IsComObject(imageList))
                    Marshal.ReleaseComObject(imageList);
            }
        }

        private static int GetImageListType(int iconSize)
        {
            if (iconSize <= 16)
                return ShilSmall;
            if (iconSize <= 32)
                return ShilLarge;
            if (iconSize <= 48)
                return ShilExtralarge;
            return ShilJumbo;
        }

        [DllImport("shell32.dll", PreserveSig = true)]
        private static extern int SHGetImageList(
            int iImageList,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IImageList ppv
        );

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig]
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

            [PreserveSig]
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);

            [PreserveSig]
            int SetOverlayImage(int iImage, int iOverlay);

            [PreserveSig]
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

            [PreserveSig]
            int Draw(IntPtr pimldp);

            [PreserveSig]
            int Remove(int i);

            [PreserveSig]
            int GetIcon(int i, int flags, out IntPtr picon);
        }
    }
}
