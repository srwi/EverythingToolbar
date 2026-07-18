using System.IO;
using System.Windows.Forms;
using EverythingToolbar.Core.Platform;
using Peter;

namespace EverythingToolbar.Services
{
    public sealed class ShellDialogsAdapter : IShellDialogs
    {
        private readonly ThemeService _themeService;

        public ShellDialogsAdapter(ThemeService themeService)
        {
            _themeService = themeService;
        }

        public void OpenWith(string filePath)
        {
            ShellUtils.OpenWithDialog(filePath);
        }

        public void OpenParentFolderAndSelect(string filePath)
        {
            ShellUtils.OpenParentFolderAndSelect(filePath);
        }

        public void ShowFileProperties(string filePath)
        {
            ShellUtils.ShowFileProperties(filePath);
        }

        public void ShowWindowsContextMenu(string filePath)
        {
            var menu = new ShellContextMenu(_themeService.IsLightTheme);
            var arrFi = new FileInfo[1];
            arrFi[0] = new FileInfo(filePath);
            menu.ShowContextMenu(arrFi, Control.MousePosition);
        }

        public string? BrowseForFile(string filterLabel, string filterPattern, string? initialDirectory)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = $"{filterLabel}|{filterPattern}|All files (*.*)|*.*",
                FilterIndex = 1,
            };
            if (initialDirectory != null)
                openFileDialog.InitialDirectory = initialDirectory;

            return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : null;
        }
    }
}
