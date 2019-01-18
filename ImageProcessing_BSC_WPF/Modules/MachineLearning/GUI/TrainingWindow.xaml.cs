using ImageProcessing_BSC_WPF.UIHelpers;
using mUserControl_BSC_dll.UserControls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI.ImageLabelTool_Yolo;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.GUI
{
    /// <summary>
    /// Interaction logic for TrainingWindow.xaml
    /// </summary>
    public partial class TrainingWindow : Window
    {
        public static TrainingWindow mTrainingWindow = null;

        ProcessStartInfo processInfo;
        Process process;
        string lineContent;
        public TrainingWindow()
        {
            mTrainingWindow = this;
            InitializeComponent();
        }

        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible)
            {
                string darknetCMD = mLabelTool.DarknetTrainCmd();

                darknetCMD = "/c " + darknetCMD;
                processInfo = new ProcessStartInfo("cmd.exe", darknetCMD);
                processInfo.CreateNoWindow = true;
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;

                trainModelRoutine.RunWorkerAsync();
            }
        }


        private BackgroundWorker trainModelRoutine = new BackgroundWorker();
        public void TrainModelRoutineSetup()
        {
            trainModelRoutine.DoWork += new DoWorkEventHandler(trainModelRoutine_doWork);
            trainModelRoutine.ProgressChanged += new ProgressChangedEventHandler(trainModelRoutine_ProgressChanged);
            trainModelRoutine.RunWorkerCompleted += new RunWorkerCompletedEventHandler(trainModelRoutine_WorkerCompleted);
            trainModelRoutine.WorkerReportsProgress = true;
            trainModelRoutine.WorkerSupportsCancellation = true;
        }

        private void trainModelRoutine_doWork(object sender, DoWorkEventArgs e)
        {
            process = Process.Start(processInfo);

            TextOutputter outputter = new TextOutputter(mTrainingWindow.TB_info);
            Console.SetOut(outputter);

            process.OutputDataReceived += Process_OutputDataReceived;
            process.BeginOutputReadLine();
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.BeginErrorReadLine();

            process.WaitForExit();

            process.Close();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            lineContent = e.Data;
            trainModelRoutine.ReportProgress(0);
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            //lineContent = e.Data;
            //trainModelRoutine.ReportProgress(0);
        }

        private void trainModelRoutine_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Console.WriteLine(lineContent);
        }

        private void trainModelRoutine_WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }


        private void lbl_title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Btn_close_Click(object sender, RoutedEventArgs e)
        {
            if (mMessageBox.ShowConfirmation("Terminate?") == mDialogResult.yes)
            {
                Close();
            }
        }

    }
}
