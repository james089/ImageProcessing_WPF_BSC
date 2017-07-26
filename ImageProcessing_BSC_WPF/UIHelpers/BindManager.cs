
namespace ImageProcessing_BSC_WPF
{
    public class BindManager
    {
        public static BindManager BindMngr = new BindManager();

        public BindString GMessage { set; get; }             //Messages showing during live view applications
        public BindInt Progress { set; get; }             
        public BindString ProgressString { set; get; }             

        /// Machine learning
        public BindString ML_sourceImgDir { set; get; }      // Source Image Directory
        public BindString ML_sourceTrainImgDir { set; get; } // Source Image Directory
        public BindString ML_sourceTestImgDir { set; get; }  // Source Image Directory
        public BindString ML_rootDir { set; get; }           // Mean and map Directory
        public BindString ML_trainImgDir { set; get; }       // Resized Train Image Directory
        public BindString ML_testImgDir { set; get; }        // Resized Test Image Directory
        public BindInt ML_desWidth { set; get; }             // Resized img width
        public BindInt ML_desHeight { set; get; }            // Resized img height
        
        public BindManager()
        {
            GMessage = new BindString();                GMessage.value = "";
            Progress = new BindInt();                   Progress.value = 0;
            ProgressString = new BindString();          ProgressString.value = "%";
            ///
            ML_sourceImgDir = new BindString();         ML_sourceImgDir.value = @"C:\temp\source";
            ML_sourceTrainImgDir = new BindString();    ML_sourceTrainImgDir.value = ML_sourceImgDir.value + @"\train";
            ML_sourceTestImgDir = new BindString();     ML_sourceTestImgDir.value = ML_sourceImgDir.value + @"\test";
            ML_rootDir = new BindString();              ML_rootDir.value = @"C:\temp\MLRoot";
            ML_trainImgDir = new BindString();          ML_trainImgDir.value = ML_rootDir.value + @"\train";
            ML_testImgDir = new BindString();           ML_testImgDir.value = ML_rootDir.value + @"\test";
            ML_desWidth = new BindInt();                ML_desWidth.value = 32;
            ML_desHeight = new BindInt();               ML_desHeight.value = 32;
        }
    }
}
