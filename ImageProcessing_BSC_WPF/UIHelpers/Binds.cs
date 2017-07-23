using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF
{
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
}
