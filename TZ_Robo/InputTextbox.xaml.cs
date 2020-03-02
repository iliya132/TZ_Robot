using System.Windows;

namespace TZ_Robo
{
    /// <summary>
    /// Interaction logic for InputTextbox.xaml
    /// </summary>
    public partial class InputTextbox : Window
    {
        public InputTextbox() : this(string.Empty) { }
        public InputTextbox(string input)
        {
            InitializeComponent();
            inputTextBox.Text = input;
        }
        private void OK_Btn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
