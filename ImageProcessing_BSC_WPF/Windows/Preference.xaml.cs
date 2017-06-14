using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ImageProcessing_BSC_WPF
{
    /// <summary>
    /// Interaction logic for Preference.xaml
    /// </summary>
    public partial class Preference : Window
    {
        public event EventHandler preferenceUpdated;
        bool _settingApplied;

        public Preference()
        {
            InitializeComponent();
            loadProgramSetting();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            applyProgramSetting();
            Radio_FPS_high.IsChecked = true;
        }

        private void loadProgramSetting()
        {
            GV._camConnectAtStartup = Properties.Settings.Default.camConnect;
            GV._previewFPS = (previewFPS) Properties.Settings.Default.previewFPS;
        }

        private void applyProgramSetting()
        {
            Chk_connectAtStartUp.IsChecked = GV._camConnectAtStartup;

            switch(GV._previewFPS)
            {
                case previewFPS.LOW :
                    Radio_FPS_low.IsChecked = true;
                    TB_previewFPS.Text = "" + (int)previewFPS.LOW;break;
                case previewFPS.MEDIUM:
                    Radio_FPS_medium.IsChecked = true;
                    TB_previewFPS.Text = "" + (int)previewFPS.MEDIUM;break;
                case previewFPS.HIGH:
                    Radio_FPS_high.IsChecked = true; 
                    TB_previewFPS.Text = "" + (int)previewFPS.HIGH;break;
            }

            _settingApplied = true;
        }

        private void saveProgramSetting()
        {
            Properties.Settings.Default.camConnect = GV._camConnectAtStartup;
            Properties.Settings.Default.previewFPS = (int) GV._previewFPS;

            Properties.Settings.Default.Save();
        }

        private void Chk_connectAtStartUp_Checked(object sender, RoutedEventArgs e)
        {
            GV._camConnectAtStartup = true;                      //set to connect the camera at the program startup
            saveProgramSetting();
        }


        private void Chk_connectAtStartUp_Unchecked(object sender, RoutedEventArgs e)
        {
            GV._camConnectAtStartup = false;                      //set to connect the camera at the program startup
            saveProgramSetting();
        }

        private void Radio_FPS_low_Checked(object sender, RoutedEventArgs e)
        {
            if (_settingApplied)
            {
                GV._previewFPS = previewFPS.LOW;
                TB_previewFPS.Text = "" + (int)previewFPS.LOW;
                saveProgramSetting();
            }
        }

        private void Radio_FPS_medium_Checked(object sender, RoutedEventArgs e)
        {
            if (_settingApplied)
            {
                GV._previewFPS = previewFPS.MEDIUM;
                TB_previewFPS.Text = "" + (int)previewFPS.MEDIUM;
                saveProgramSetting();
            }
        }

        private void Radio_FPS_high_Checked(object sender, RoutedEventArgs e)
        {
            if (_settingApplied)
            {
                GV._previewFPS = previewFPS.HIGH;
                TB_previewFPS.Text = "" + (int)previewFPS.HIGH;
                saveProgramSetting();
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (this.preferenceUpdated != null)
                preferenceUpdated(new object(), new EventArgs());
            this.Close();
        }
    }
}
