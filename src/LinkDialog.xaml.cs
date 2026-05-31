using System.Windows;

namespace NativeMDView
{
    public partial class LinkDialog : Window
    {
        public string LinkText => LinkTextBox.Text;
        public string LinkUrl => LinkUrlBox.Text;

        public LinkDialog()
        {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LinkTextBox.Text) || string.IsNullOrWhiteSpace(LinkUrlBox.Text))
            {
                MessageBox.Show("Please fill in both fields.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
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
