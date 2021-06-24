 #if UNITY_EDITOR
 using UnityEditor;
 using UnityEditor.Build;
 using UnityEngine;
 using System;
 using System.IO;
using UnityEditor.Build.Reporting;

namespace BEERLabs.ProjectEsky{
    class BuildPreProcessor : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }
        /*public void OnPreprocessBuild(BuildTarget target, string path) {
        // Do the preprocessing here
            Debug.Log(path);
            string outputpath = path.Substring(0, path.LastIndexOf("/"));
            Debug.Log(outputpath);
            Copy("./OpticalCalibrations/",Path.Combine(outputpath,"OpticalCalibrations/"));
            Copy("./TrackingCalibrations/",Path.Combine(outputpath,"TrackingCalibrations/"));
            File.Copy("shaders.shader",Path.Combine(outputpath, "shaders.shader"));
            File.Copy("DisplaySettings.json",Path.Combine(outputpath, "DisplaySettings.json"));            
        }*/
        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log(report.summary.outputPath);
            string outputpath = report.summary.outputPath.Substring(0, report.summary.outputPath.LastIndexOf("/"));
            Debug.Log(outputpath);
            File.Copy("shaders.shader",Path.Combine(outputpath, "shaders.shader"));
            File.Copy("EskySettings.json",Path.Combine(outputpath, "EskySettings.json"));            
        }
    }
 }
 #endif