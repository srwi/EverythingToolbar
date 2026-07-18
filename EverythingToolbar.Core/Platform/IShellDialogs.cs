namespace EverythingToolbar.Core.Platform
{
    public interface IShellDialogs
    {
        void OpenWith(string filePath);
        void OpenParentFolderAndSelect(string filePath);
        void ShowFileProperties(string filePath);

        void ShowWindowsContextMenu(string filePath);

        string? BrowseForFile(string filterLabel, string filterPattern, string? initialDirectory);
    }
}
