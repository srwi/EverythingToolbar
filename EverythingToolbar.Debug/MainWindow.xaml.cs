using EverythingToolbar.Helpers;
using System;
using System.Windows;

namespace EverythingToolbar.Debug
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ApplicationResources.Instance.ResourceChanged += (object sender, ResourcesChangedEventArgs e) =>
            {
                Resources.MergedDictionaries.Add(e.NewResource);
            };
            ApplicationResources.Instance.LoadDefaults();
        }
    }
}
