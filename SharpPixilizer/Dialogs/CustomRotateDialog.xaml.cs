using System.Windows;

namespace SharpPixilizer
{
    public partial class CustomRotateDialog : Window
    {
        public int RotationAngle { get; private set; }

        public CustomRotateDialog()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(AngleTextBox.Text, out int angle))
            {
                RotationAngle = angle;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Введите корректное число.");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
