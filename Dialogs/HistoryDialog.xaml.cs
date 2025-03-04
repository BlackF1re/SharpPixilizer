using System.Collections.Generic;
using System.Windows;

namespace SharpPixilizer
{
    public partial class HistoryDialog : Window
    {
        public HistoryDialog(List<string> history)
        {
            InitializeComponent();
            HistoryListBox.ItemsSource = history;
        }
    }
}
