using EverythingToolbar.Helpers;
using EverythingToolbar.Properties;

namespace EverythingToolbar.Services
{
    public sealed class FilterNamesService : IFilterNames
    {
        public string All => Resources.DefaultFilterAll;
        public string File => Resources.DefaultFilterFile;
        public string Folder => Resources.DefaultFilterFolder;
        public string Audio => Resources.UserFilterAudio;
        public string Compressed => Resources.UserFilterCompressed;
        public string Document => Resources.UserFilterDocument;
        public string Executable => Resources.UserFilterExecutable;
        public string Picture => Resources.UserFilterPicture;
        public string Video => Resources.UserFilterVideo;
    }
}