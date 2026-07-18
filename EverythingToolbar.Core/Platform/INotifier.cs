namespace EverythingToolbar.Core.Platform
{
    public interface INotifier
    {
        void ShowError(string messageResourceKey, string? detail = null);

        void ShowInformation(string messageResourceKey);
    }
}
