using System;
using EverythingToolbar.Controls;
using EverythingToolbar.Platform;
using EverythingToolbar.Properties;

namespace EverythingToolbar.Helpers
{
    public sealed class NotifierAdapter : INotifier
    {
        public void ShowError(string messageResourceKey, string? detail = null)
        {
            var message = Resolve(messageResourceKey);
            if (!string.IsNullOrEmpty(detail))
                message += Environment.NewLine + Environment.NewLine + detail;

            _ = FluentMessageBox
                .CreateError(message, Resources.MessageBoxErrorTitle)
                .ShowDialogAsync();
        }

        public void ShowInformation(string messageResourceKey)
        {
            var message = Resolve(messageResourceKey);

            _ = FluentMessageBox
                .CreateRegular(message, string.Empty)
                .ShowDialogAsync();
        }

        private static string Resolve(string resourceKey)
        {
            return Resources.ResourceManager.GetString(resourceKey) ?? resourceKey;
        }
    }
}