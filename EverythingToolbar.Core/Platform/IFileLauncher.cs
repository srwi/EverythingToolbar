namespace EverythingToolbar.Platform
{
    public interface IFileLauncher
    {
        void Open(string path, string? workingDirectory = null);

        void OpenAsAdmin(string path);

        void OpenWithArguments(string path, string arguments);

        void RunCommand(string commandLine, string? workingDirectory = null);
    }
}