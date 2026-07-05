using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using EverythingToolbar.Platform;

namespace EverythingToolbar.Helpers
{
    public sealed class ClipboardAdapter : IClipboard
    {
        public void SetFileDropList(IEnumerable<string> filePaths)
        {
            var dataObj = new DataObject();
            var collection = new StringCollection();
            foreach (var path in filePaths)
                collection.Add(path);
            dataObj.SetFileDropList(collection);
            Clipboard.SetDataObject(dataObj, copy: false); // Fixes #362
        }

        public void SetText(string text)
        {
            var dataObj = new DataObject();
            dataObj.SetText(text);
            Clipboard.SetDataObject(dataObj, copy: false); // Fixes #362
        }
    }
}