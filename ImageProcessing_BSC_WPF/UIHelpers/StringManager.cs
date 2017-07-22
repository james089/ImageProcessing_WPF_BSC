using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing_BSC_WPF
{
    public class StringManager
    {
        public static StringManager StrMngr = new StringManager();

        public BindString GMessage { set; get; }             //Messages showing during live view applications

        /// Machine learning
        public BindString ML_rootDir { set; get; }           // Mean and map Directory
        public BindString ML_sourceImgDir { set; get; }      // Source Image Directory
        public BindString ML_resizedImgDir { set; get; }     // resized Image Directory
        public BindString ML_desWidth { set; get; }          // Resized img width
        public BindString ML_desHeight { set; get; }         // Resized img height

        public StringManager()
        {
            GMessage = new BindString();            GMessage.value = "";
            ML_rootDir = new BindString();          ML_rootDir.value = @"C:\temp";
            ML_sourceImgDir = new BindString();     ML_sourceImgDir.value = ML_rootDir.value + @"\source";
            ML_resizedImgDir = new BindString();    ML_resizedImgDir.value = ML_rootDir.value + @"\resized";
            ML_desWidth = new BindString();         ML_desWidth.value = "32";
            ML_desHeight = new BindString();        ML_desHeight.value = "32";
        }
    }
}
