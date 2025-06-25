using System;
using System.Reflection;

namespace EverythingToolbar.Settings
{
    public partial class About
    {
        public About()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            VersionTextBlock.Text = Properties.Resources.AboutVersion + " " +
                                    (version.Revision == 0
                                        ? $"{version.Major}.{version.Minor}.{version.Build}"
                                        : $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}");
        }
    }
}