﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ImageProcessing_BSC_WPF.Modules.MachineLearning.YOLO
{
    public class ModelPath
    {
        public string ConfigPath { get; private set; }
        public string WeightsPath { get; private set; }
        public string NamesPath { get; private set; }
        public float FixedAspectRatio { get; private set; } = -1f;
        public bool Found { get; private set; } = true;

        public ModelPath(string modelPath)
        {
            getModelFiles(modelPath);
        }

        public static string GetWeightFile(string path)
        {
            if (new DirectoryInfo(path).EnumerateFiles("*.weights").Count() != 0)
            {
                string fileName = (new DirectoryInfo(path).EnumerateFiles("*.weights")
                   .OrderByDescending(f => f.CreationTime)
                   .FirstOrDefault()).Name;
                if (File.Exists(fileName))
                    return fileName;
                else
                    return "No Weight File";
            }
            else
            {
                return "No Weight File";
            }
           
        }

        private void getModelFiles(string modelPath)
        {
            try
            {
                ConfigPath = getLatestFile(modelPath, "*.cfg");
                WeightsPath = getLatestFile(modelPath, "*.weights");
                NamesPath = getLatestFile(modelPath, "*.names");

                string aspect = Path.Combine(modelPath, "fixed.aspect");
                float result;
                if (File.Exists(aspect))
                {
                    string data = File.ReadAllText(aspect).Trim();
                    if (float.TryParse(data, out result))
                    {
                        FixedAspectRatio = result;
                    }
                }
            }
            catch
            {
                Found = false;
            }
        }
        private string getLatestFile(string path, string ext)
        {
            return Path.Combine(path, (new DirectoryInfo(path).EnumerateFiles(ext)
                    .OrderByDescending(f => f.CreationTime)
                    .FirstOrDefault()).Name);
        }
    }
}
