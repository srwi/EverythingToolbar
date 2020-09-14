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

		public string FileName { get; set; }

		public string FullPathAndFileName { get; set; }

		public string DateModified
		{
			get
			{
				return File.GetLastWriteTime(FullPathAndFileName).ToString();
			}
		}
	}
}
