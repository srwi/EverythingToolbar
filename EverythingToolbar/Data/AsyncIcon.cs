using System;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EverythingToolbar.Helpers;

namespace EverythingToolbar.Data
{
    public class AsyncIcon
    {
        private const string PlaceholderIconUri = "pack://application:,,,/EverythingToolbar;component/Icons/PlaceholderIcon.ico";
        private static readonly ImageSource PlaceholderIcon = LoadPlaceholderIcon();

        public delegate void IconLoadedEventHandler();

        private bool _isLoading;

        private ImageSource _icon;
        public ImageSource Icon
        {
            get => _isLoading ? PlaceholderIcon : _icon;
            private set => _icon = value;
        }

        public void LoadIconAsync(string fullPathAndFileName, IconLoadedEventHandler callback = null)
        {
            if (_isLoading)
                return;

            _isLoading = true;

            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    var loadedIcon = WindowsThumbnailProvider.GetThumbnail(fullPathAndFileName, 32, 32);
                    Icon = loadedIcon;
                    callback?.Invoke();
                }
                catch
                {
                    Icon = PlaceholderIcon;
                    callback?.Invoke();
                }
                finally
                {
                    _isLoading = false;
                }
            });
        }

        private static ImageSource LoadPlaceholderIcon()
        {
            try
            {
                var uri = new Uri(PlaceholderIconUri);
                return BitmapFrame.Create(uri);
            }
            catch
            {
                return null;
            }
        }
    }
}
