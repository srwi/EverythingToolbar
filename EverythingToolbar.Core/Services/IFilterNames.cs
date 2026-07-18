namespace EverythingToolbar.Core.Services
{
    public interface IFilterNames
    {
        string All { get; }
        string File { get; }
        string Folder { get; }
        string Audio { get; }
        string Compressed { get; }
        string Document { get; }
        string Executable { get; }
        string Picture { get; }
        string Video { get; }
    }
}
