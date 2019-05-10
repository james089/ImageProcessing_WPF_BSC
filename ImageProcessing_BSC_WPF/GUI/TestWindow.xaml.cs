
using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

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


        }

        private void chk2_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void chk2_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        DispatcherTimer t;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(1);
            t.Tick += T_Tick;
            t.Start();
        }

        bool flag = false;
        private void T_Tick(object sender, EventArgs e)
        {
            if (flag)
            {
                flag = false;
                SV.ScrollToRightEnd();
            }
            else
            {
                flag = true;
                SV.ScrollToLeftEnd();
            }
        }
    }
}
