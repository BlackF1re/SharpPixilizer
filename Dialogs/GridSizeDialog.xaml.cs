using System.Windows;

namespace SharpPixilizer
{
    public partial class GridSizeDialog : Window
    {
        public int GridColumns { get; private set; }
        public int GridRows { get; private set; }

        public GridSizeDialog(int currentColumns, int currentRows)
        {
            InitializeComponent();
            ColumnsTextBox.Text = currentColumns.ToString();
            RowsTextBox.Text = currentRows.ToString();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ColumnsTextBox.Text, out int cols) && int.TryParse(RowsTextBox.Text, out int rows))
            {
                GridColumns = cols;
                GridRows = rows;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Введите корректные числа.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
