using System.ComponentModel;
using System;

namespace ImageProcessing_BSC_WPF
{
    public class BindManager
    {
        public static BindManager BindMngr = new BindManager();

        public BindString GMessage { set; get; }             //Messages showing during live view applications
        public BindInt Progress { set; get; }             
        public BindString ProgressString { set; get; }
        public BindString CmdWindowString { set; get; }

        /// Machine learning
        public BindString ML_CNTK_sourceImgDir { set; get; }      // Source Image Directory
        public BindString ML_CNTK_sourceTrainImgDir { set; get; } // Source Image Directory
        public BindString ML_CNTK_sourceTestImgDir { set; get; }  // Source Image Directory
        public BindString ML_CNTK_rootDir { set; get; }           // Mean and map Directory
        public BindString ML_CNTK_trainImgDir { set; get; }       // Resized Train Image Directory
        public BindString ML_CNTK_testImgDir { set; get; }        // Resized Test Image Directory
        public BindInt    ML_desWidth { set; get; }          // Resized img width
        public BindInt    ML_desHeight { set; get; }         // Resized img height

        public BindManager()
        {
            GMessage = new BindString();                        GMessage.value = "";
            Progress = new BindInt();                           Progress.value = 0;
            ProgressString = new BindString();                  ProgressString.value = "%";
            CmdWindowString = new BindString();                 CmdWindowString.value = "";
            ///
            ML_CNTK_sourceImgDir = new BindString();            ML_CNTK_sourceImgDir.value = @"C:\temp\source";
            ML_CNTK_sourceTrainImgDir = new BindString();       ML_CNTK_sourceTrainImgDir.value = ML_CNTK_sourceImgDir.value + @"\train";
            ML_CNTK_sourceTestImgDir = new BindString();        ML_CNTK_sourceTestImgDir.value = ML_CNTK_sourceImgDir.value + @"\test";
            ML_CNTK_rootDir = new BindString();                 ML_CNTK_rootDir.value = @"C:\temp\MLRoot";
            ML_CNTK_trainImgDir = new BindString();             ML_CNTK_trainImgDir.value = ML_CNTK_rootDir.value + @"\train";
            ML_CNTK_testImgDir = new BindString();              ML_CNTK_testImgDir.value = ML_CNTK_rootDir.value + @"\test";
            ML_desWidth = new BindInt();                        ML_desWidth.value = 32;
            ML_desHeight = new BindInt();                       ML_desHeight.value = 32;
        }

        #region Binding Settings
        public class BindString : INotifyPropertyChanged
        {
            public BindString() { }

            private string value_ = null;
            public string value
            {
                get { return value_; }
                set
                {
                    if (value_ != value)
                    {
                        value_ = value;
                        NotifyPropertyChanged("value");
                    }
                }
            }

            #region INotifyPropertyChanged Members

            private void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion
        }

        public class BindInt : INotifyPropertyChanged
        {
            public BindInt() { }

            private int value_ = 0;
            public int value
            {
                get { return value_; }
                set
                {
                    if (value_ != value)
                    {
                        value_ = value;
                        NotifyPropertyChanged("value");
                    }
                }
            }

            #region INotifyPropertyChanged Members

            private void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion
        }

        public class BindDouble : INotifyPropertyChanged
        {
            public BindDouble() { }

            private double value_ = 0;
            public double value
            {
                get { return value_; }
                set
                {
                    if (value_ != value)
                    {
                        value_ = value;
                        NotifyPropertyChanged("value");
                    }
                }
            }

            #region INotifyPropertyChanged Members

            private void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion
        }

        public class BindBool : INotifyPropertyChanged
        {
            public BindBool() { }

            private bool value_ = false;
            public bool value
            {
                get { return value_; }
                set
                {
                    if (value_ != value)
                    {
                        value_ = value;
                        NotifyPropertyChanged("value");
                    }
                }
            }

            #region INotifyPropertyChanged Members

            private void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion
        }
        public class GetUIVal
        {
            public static double getparamDouble(string vstr, string errmsg)
            {
                if (vstr == null || vstr.Length == 0)
                    throw (new Exception(errmsg));
                double val;
                if (!double.TryParse(vstr, out val))
                    throw (new Exception(errmsg));
                return val;
            }
            public static int getparamint(string vstr, string errmsg)
            {
                if (vstr == null || vstr.Length == 0)
                    throw (new Exception(errmsg));
                int val;
                if (!int.TryParse(vstr, out val))
                    throw (new Exception(errmsg));
                return val;
            }
            public static uint getparamuint(string vstr, string errmsg)
            {
                if (vstr == null || vstr.Length == 0)
                    throw (new Exception(errmsg));
                uint val;
                if (!uint.TryParse(vstr, out val))
                    throw (new Exception(errmsg));
                return val;
            }
        }
        #endregion
    }
}
