using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Linq;

/// <summary>
/// The purpose of this class is to add menu items to the unity menu toolbar.
/// Clicking these menu items will perform some functionality describe in this file.
/// Non-EditorWindow MenuItems should go in this class to keep them in a common place
/// EditorWindow MenuItems go in their own specific classes (like SBMWindow.cs)
/// </summary>
public class SBMenuItems : MonoBehaviour
{
    public static List<string> GetAnimsToConvert()
    {
        List<string> filesToConvert = new List<string>();

        UnityEngine.Object[] selectedObjects = Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets);
        foreach (UnityEngine.Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (Path.GetExtension(path) == "")
            {
                // they have a folder selected
                filesToConvert.AddRange(Directory.GetFiles(path, "*.fbx", SearchOption.AllDirectories));
                filesToConvert.AddRange(Directory.GetFiles(path, "*.obj", SearchOption.AllDirectories));
                filesToConvert.AddRange(Directory.GetFiles(path, "*.dae", SearchOption.AllDirectories));
                filesToConvert.AddRange(Directory.GetFiles(path, "*.skm", SearchOption.AllDirectories));
            }
            else
            {
                filesToConvert.Add(path);
            }
        }

        return filesToConvert;
    }

    ///[MenuItem("VH/Convert To SB Motion")] // TODO: PUT THIS BACK IN AND REMOVE IT FROM FbxToSbmConverter when a window is no longer needed!
    public static void ConvertToSBMotion()
    {
        List<string> filesToConvert = GetAnimsToConvert();

        if (filesToConvert.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Select a folder or an fbx, obj, dae, skm in order to convert", "Ok");
        }
        else
        {
            filesToConvert.ForEach(f => f = f.Replace(Application.dataPath, "Assets"));
            filesToConvert.ForEach(f => f = f.Replace("\\", "/"));
            ConvertFilesToSBMotions(filesToConvert);
        }
    }

    public static void PrepareAnimsForSBMotionConversion()
    {
        List<string> filesToConvert = GetAnimsToConvert();

        if (filesToConvert.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Select a folder or an fbx, obj, dae, skm in order to convert", "Ok");
        }
        else
        {
            filesToConvert.ForEach(f => f = f.Replace(Application.dataPath, "Assets"));
            filesToConvert.ForEach(f => f = f.Replace("\\", "/"));
            PrepareFilesForSBMotionConversion(filesToConvert);
        }
    }

    static void ConvertFilesToSBMotions(List<string> files)
    {
        Debug.Log("--- ConvertFilesToSBMotions() started -------------");

        for (int i = 0; i < files.Count; i++)
        {
            //Debug.Log("Converting " + files[i]);

            //Debug.Log(files[i]);
            if (Path.GetExtension(files[i]) == ".skm")
            {
                SbmToFbxConverter.CreateMotionFromSkm(files[i]);
            }
            else
            {
                FbxToSbmConverter.ConvertToSBMotion(files[i]);
            }
        }

        Debug.Log("--- ConvertFilesToSBMotions() ended -------------");
    }

    static void PrepareFilesForSBMotionConversion(List<string> files)
    {
        Debug.Log("--- PrepareFilesForSBMotionConversion() started -------------");

        for (int i = 0; i < files.Count; i++)
        {
            if (Path.GetExtension(files[i]) == ".fbx")
                FbxToSbmConverter.ConvertAnimationType(files[i], ModelImporterAnimationType.Legacy, ModelImporterAnimationCompression.Off);
        }

        Debug.Log("--- PrepareFilesForSBMotionConversion() ended -------------");
    }

}
