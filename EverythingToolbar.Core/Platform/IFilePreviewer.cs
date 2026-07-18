namespace EverythingToolbar.Core.Platform
{
    public interface IFilePreviewer
    {
        void PreviewInQuickLook(string filePath);
        void PreviewInSeer(string filePath);
    }
}
