using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using EverythingToolbar.Helpers;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using MessageBox = Wpf.Ui.Controls.MessageBox;
using TextBlock = System.Windows.Controls.TextBlock;

namespace EverythingToolbar.Controls
{
    public static class FluentMessageBox
    {
        private static MessageBox CreateBase(string title)
        {
            var messageBox = new MessageBox()
            {
                Title = title,
                IsCloseButtonEnabled = true,
                MinWidth = 300,
            };

            var appTheme = Ioc.Default.GetRequiredService<ThemeService>().GetEffectiveTheme(ThemeFlavor.App);
            ApplicationThemeManager.Apply(
                appTheme == Theme.Light ? ApplicationTheme.Light : ApplicationTheme.Dark
            );
            ApplicationThemeManager.Apply(messageBox);

            return messageBox;
        }

        private static TextBlock CreateTextBlock(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 14,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
        }

        public static MessageBox CreateRegular(string content, string title)
        {
            var messageBox = CreateBase(title);

            messageBox.Content = CreateTextBlock(content);

            return messageBox;
        }

        public static MessageBox CreateYesNo(string content, string title)
        {
            var messageBox = CreateRegular(content, title);

            messageBox.PrimaryButtonText = "Yes";
            messageBox.SecondaryButtonText = "No";
            messageBox.IsCloseButtonEnabled = false;

            return messageBox;
        }

        public static MessageBox CreateError(string content, string title)
        {
            var messageBox = CreateBase(title);

            messageBox.IsPrimaryButtonEnabled = false;

            var symbolIcon = new SymbolIcon
            {
                Symbol = SymbolRegular.Warning28,
                FontSize = 32,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 20, 0),
            };
            Grid.SetColumn(symbolIcon, 0);

            var textBlock = new TextBlock
            {
                Text = content,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            Grid.SetColumn(textBlock, 1);

            messageBox.Content = new Grid
            {
                Margin = new Thickness(20, 0, 20, 0),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                },
                Children = { symbolIcon, textBlock },
            };
            return messageBox;
        }
    }
}