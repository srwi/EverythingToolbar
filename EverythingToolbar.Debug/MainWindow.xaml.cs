using System.Windows;

namespace EverythingToolbar.Debug
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ResourceManager.Instance.ResourceChanged += (sender, e) => { Resources = e.NewResource; };
            ResourceManager.Instance.AutoApplyTheme();
        }
    }
}
