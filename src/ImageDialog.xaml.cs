using System.Windows;

namespace NativeMDView
{
    public partial class ImageDialog : Window
    {
        public string AltText => AltTextBox.Text;
        public string ImageUrl => ImageUrlBox.Text;

        public ImageDialog()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ImageUrlBox.Text))
            {
                MessageBox.Show("Please enter an image URL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
