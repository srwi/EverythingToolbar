using EverythingToolbar.Properties;
using System.Collections.Generic;
using System.ComponentModel;

namespace EverythingToolbar.Settings
{
    public partial class UserInterface
    {
        public UserInterface()
        {
            InitializeComponent();
            DataContext = new UserInterfaceViewModel();
        }
    }

    public class UserInterfaceViewModel : INotifyPropertyChanged
    {
        public List<KeyValuePair<string, string>> ItemTemplates { get; } =
        [
            new(Resources.ItemTemplateCompact, "Compact"),
            new(Resources.ItemTemplateCompactDetailed, "CompactDetailed"),
            new(Resources.ItemTemplateNormal, "Normal"),
            new(Resources.ItemTemplateNormalDetailed, "NormalDetailed")
        ];

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}