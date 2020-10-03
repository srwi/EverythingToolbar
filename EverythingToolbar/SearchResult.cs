using System.IO;
using System.Windows.Media;

namespace EverythingToolbar
{
	public class SearchResult
	{
		public ImageSource Icon
		{
			get
			{
				ImageSource image = WindowsThumbnailProvider.GetThumbnail(FullPathAndFileName, 16, 16);
				image.Freeze();
				return image;
			}
		}

        public bool IsFile { get; set; }

		public string FileName { get; set; }

		public string Path { get; set; }

		public string FullPathAndFileName { get; set; }

		public string FileSize
		{
			get
			{
                if (!IsFile)
                    return GetBytesReadable(0);

                try
                {
                    FileInfo fi = new FileInfo(FullPathAndFileName);
                    return GetBytesReadable(fi.Length);
                }
                catch
				{
                    return "";
                }
			}
		}

		public string DateModified
		{
			get
			{
				return File.GetLastWriteTime(FullPathAndFileName).ToString();
			}
		}

        // Taken from: https://stackoverflow.com/a/11124118/1477251
        static string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);

            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }

            // Divide by 1024 to get fractional value
            readable = (readable / 1024);

            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }
    }
}
