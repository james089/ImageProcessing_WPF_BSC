using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ImageProcessing_BSC_WPF.GUI.Panels
{
    /// <summary>
    /// Interaction logic for LabelledTextBox.xaml
    /// </summary>
    public partial class LabelledTextBox : UserControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
           .Register("Label",
                   typeof(string),
                   typeof(LabelledTextBox),
                   new FrameworkPropertyMetadata("LabelledTextBox1"));

        public static readonly DependencyProperty LabelWidthProperty = DependencyProperty
            .Register("LabelWidth",
                    typeof(string),
                    typeof(LabelledTextBox),
                    new FrameworkPropertyMetadata("LabelledTextBox2"));

        public static readonly DependencyProperty TextProperty = DependencyProperty
            .Register("Text",
                    typeof(string),
                    typeof(LabelledTextBox),
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty TextWidthProperty = DependencyProperty
            .Register("TextWidth",
                    typeof(string),
                    typeof(LabelledTextBox),
                    new FrameworkPropertyMetadata("LabelledTextBox3"));

        public static readonly DependencyProperty UnitProperty = DependencyProperty.Register("Unit", typeof(string),
    typeof(LabelledTextBox), new PropertyMetadata(""));

        public LabelledTextBox()
        {
            InitializeComponent();
            Root.DataContext = this;
        }

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }
        public string LabelWidth
        {
            get { return (string)GetValue(LabelWidthProperty); }
            set { SetValue(LabelWidthProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public string TextWidth
        {
            get { return (string)GetValue(TextWidthProperty); }
            set { SetValue(TextWidthProperty, value); }
        }

        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }
    }
}
