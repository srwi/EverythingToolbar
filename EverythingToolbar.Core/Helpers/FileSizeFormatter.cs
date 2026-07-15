namespace EverythingToolbar.Core.Helpers
{
    public static class FileSizeFormatter
    {
        public static string GetHumanReadableFileSize(long length)
        {
            string[] units = ["B", "KB", "MB", "GB", "TB", "PB", "EB"];
            double size = length;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            if (unit == 0)
                return size.ToString("0 B");

            string format = size switch
            {
                >= 100 => $"0 {units[unit]}",
                >= 10 => $"0.# {units[unit]}",
                _ => $"0.## {units[unit]}"
            };
            return size.ToString(format);
        }
    }
}
