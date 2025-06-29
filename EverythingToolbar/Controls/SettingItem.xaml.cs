using System.Windows;

namespace EverythingToolbar.Controls
{
    public partial class SettingItem
    {
        public SettingItem()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty SettingContentProperty =
            DependencyProperty.Register(nameof(SettingContent), typeof(object), typeof(SettingItem), new PropertyMetadata(null));

        public object SettingContent
        {
            get => GetValue(SettingContentProperty);
            set => SetValue(SettingContentProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(SettingItem), new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty HelpTextProperty =
            DependencyProperty.Register(nameof(HelpText), typeof(string), typeof(SettingItem), new PropertyMetadata(string.Empty));

        public string HelpText
        {
            get => (string)GetValue(HelpTextProperty);
            set => SetValue(HelpTextProperty, value);
        }
    }

}