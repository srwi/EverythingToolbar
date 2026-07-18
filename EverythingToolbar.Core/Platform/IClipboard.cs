using System.Collections.Generic;

namespace EverythingToolbar.Core.Platform
{
    public interface IClipboard
    {
        void SetFileDropList(IEnumerable<string> filePaths);
        void SetText(string text);
    }
}
