using System;
using System.Windows;
using System.Windows.Controls;

namespace RemoteAbandon.Views
{
    public partial class RemoteAbandonControl : UserControl
    {
        private Plugin Plugin { get; }

        public RemoteAbandonControl()
        {
            InitializeComponent();
        }

        public RemoteAbandonControl(Plugin plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin;
        }

        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Plugin?.SaveConfig();
                MessageBox.Show("Remote Grid Abandon configuration saved successfully!", "Remote Grid Abandon", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetStatsButton_OnClick(object sender, RoutedEventArgs e)
        {
            Plugin?.Statistics?.Reset();
        }
    }
}

