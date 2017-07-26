
using System.Windows;

namespace ImageProcessing_BSC_WPF
{
    /// <summary>
    /// Interaction logic for TestWindow.xaml
    /// </summary>
    public partial class TestWindow : Window
    {
        public TestWindow()
        {
            InitializeComponent();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //chk2.IsChecked = !chk1.IsChecked;
            chk1.Unchecked -= CheckBox_Checked;
            chk1.IsChecked = false;
            chk1.Unchecked += CheckBox_Checked;

        }

        private void chk2_Checked(object sender, RoutedEventArgs e)
        {
            chk2.Unchecked -= CheckBox_Checked;
            chk2.IsChecked = false;
            chk2.Unchecked += CheckBox_Checked;
        }

        private void chk2_Unchecked(object sender, RoutedEventArgs e)
        {

        }

    }
}
