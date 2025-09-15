using System.Windows;

namespace DANCustomTools.Views.Dialogs
{
    public partial class AttributeEditDialog : Window
    {
        public AttributeEditDialog()
        {
            InitializeComponent();
        }

        public string AttributeName
        {
            get { return AttributeTextBox.Text; }
            set { AttributeTextBox.Text = value; }
        }

        public string Description
        {
            get { return DescriptionTextBox.Text; }
            set { DescriptionTextBox.Text = value; }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}