using System.IO;
using System.Windows.Forms;
using EverythingToolbar.Platform;
using Peter;

namespace EverythingToolbar.Helpers
{
    public sealed class ShellDialogsAdapter : IShellDialogs
    {
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
            var menu = new ShellContextMenu();
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