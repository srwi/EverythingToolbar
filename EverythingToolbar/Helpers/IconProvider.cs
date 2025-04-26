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
    public static class ThumbnailProvider
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory shellItem);

        [ComImport]
        [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage(
                [In] Size size,
                [In] int flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Size
        {
            public int cx;
            public int cy;
        }

        private const int SiigbfResizetofit = 0x00;

        public static ImageSource GetImage(string filePath)
        {
            try
            {
                Guid shellItemImageFactoryGuid = new Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B");
                SHCreateItemFromParsingName(filePath, IntPtr.Zero, shellItemImageFactoryGuid, out IShellItemImageFactory imageFactory);

                Size size = new Size { cx = 32, cy = 32 };
                imageFactory.GetImage(size, SiigbfResizetofit, out IntPtr hBitmap);

                try
                {
                    var imageSource = Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    imageSource.Freeze();
                    return imageSource;
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
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }

    public static class IconProvider
    {
        private static readonly ConcurrentDictionary<int, ImageSource> IconIndexCache = new ConcurrentDictionary<int, ImageSource>();
        private static readonly ConcurrentDictionary<string, ImageSource> ExtensionCache = new ConcurrentDictionary<string, ImageSource>();

        [StructLayout(LayoutKind.Sequential)]
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
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
            ref Shfileinfo psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint ShgfiIcon = 0x000000100;
        private const uint ShgfiSmallicon = 0x000000001;
        private const uint ShgfiSysiconindex = 0x000004000;
        private const uint ShgfiUsefileattributes = 0x000000010;
        private const uint FileAttributeNormal = 0x00000080;

        private static int _fallbackIconIndex;

        public static ImageSource GetImage(string path, Action<ImageSource> onUpdated = null)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                extension = Path.GetFileName(path).ToLowerInvariant();

            if (!ExtensionCache.TryGetValue(extension, out var iconByExtension))
            {
                iconByExtension = GetIconByPath(path);
                if (iconByExtension != null)
                {
                    ExtensionCache.TryAdd(extension, iconByExtension);
                }
            }

            if (onUpdated == null)
                return iconByExtension;

            Task.Run(() =>
            {
                int iconIndex = GetIconIndex(path, false);
                if (iconIndex == 0)
                    iconIndex = GetFallbackIconIndex();

                if (IconIndexCache.TryGetValue(iconIndex, out var cachedIcon))
                {
                    onUpdated.Invoke(cachedIcon);
                    return;
                }

                var exactIcon = GetIconByPath(path);
                IconIndexCache.TryAdd(iconIndex, exactIcon);

                if (exactIcon != null)
                    onUpdated.Invoke(exactIcon);
            });

            return iconByExtension;
        }

        private static ImageSource GetIconByPath(string path)
        {
            Shfileinfo shfi = new Shfileinfo();
            const uint flags = ShgfiIcon | ShgfiSmallicon;
            SHGetFileInfo(path, 0, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            if (shfi.hIcon == IntPtr.Zero)
                return null;

            try
            {
                var imageSource = Imaging.CreateBitmapSourceFromHIcon(
                    shfi.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                imageSource.Freeze();
                return imageSource;
            }
            finally
            {
                DestroyIcon(shfi.hIcon);
            }
        }

        private static int GetFallbackIconIndex()
        {
            if (_fallbackIconIndex == 0)
                _fallbackIconIndex = GetIconIndex("", true);

            return _fallbackIconIndex;
        }

        private static int GetIconIndex(string path, bool useFilename)
        {
            Shfileinfo shfi = new Shfileinfo();
            uint flags = ShgfiSysiconindex | ShgfiSmallicon;
            uint fileAttributes = 0;
            if (useFilename)
            {
                fileAttributes = FileAttributeNormal;
                flags |= ShgfiUsefileattributes;
            }
            SHGetFileInfo(path, fileAttributes, ref shfi, (uint)Marshal.SizeOf(shfi), flags);
            return shfi.iIcon;
        }
    }
}