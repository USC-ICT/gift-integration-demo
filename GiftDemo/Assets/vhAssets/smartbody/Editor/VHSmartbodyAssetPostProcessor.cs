using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// Custom import class.  These functions get called on asset import or reimport
/// </summary>
public class VHSmartbodyAssetPostProcessor : AssetPostprocessor
{
    #region Constants
    const string SbodyPosXPropName = "SbodyPosX";
    const string SbodyPosYPropName = "SbodyPosY";
    const string SbodyPosZPropName = "SbodyPosZ";
    const string SbodyQuatPropName = "SbodyQuat";
    #endregion


    // key is a joint name, value is the channels that are used for the sb motion
    Dictionary<string, SmartbodyMotion.JointChannelFlags> m_JointChannelUsageMap = new Dictionary<string, SmartbodyMotion.JointChannelFlags>();
    Dictionary<string, float> m_SyncPoints = new Dictionary<string, float>();


    /// <summary>
    /// Reads Maya User Properties. This function is called after OnAssignMaterialModel and before OnPostprocessModel
    /// The fbx gameobject hierachy has not been created and connected at this point
    /// </summary>
    /// <param name="go"></param>
    /// <param name="propNames"></param>
    /// <param name="values"></param>
    void OnPostprocessGameObjectWithUserProperties(GameObject go, string[] propNames, object[] values)
    {
        int i = 0;
        try
        {
            // Go through the properties one by one
            for (i = 0; i < propNames.Length; i++)
            {
                if ("CreateSBMotion" == propNames[i])
                {
                    if (go.name.Contains("@"))
                    {
                        //m_bCreateSBMotion = (bool)values[i];
                    }
                }
                else if ("readyTime" == propNames[i]
                    || "strokeStartTime" == propNames[i]
                    || "emphasisTime" == propNames[i]
                    || "strokeTime" == propNames[i]
                    || "relaxTime" == propNames[i])
                {
                    //m_bCreateSBMotion = true;
                    m_SyncPoints.Add(propNames[i], (int)values[i]);
                    SmartbodyAttributes attributes = go.GetComponent<SmartbodyAttributes>();
                    if (attributes == null)
                    {
                        attributes = go.AddComponent<SmartbodyAttributes>();
                    }
                    attributes.AddSyncPoint(propNames[i], (int)values[i]);

                }
                else if (SbodyPosXPropName == propNames[i]
                    || SbodyPosYPropName == propNames[i]
                    || SbodyPosZPropName == propNames[i]
                    || SbodyQuatPropName == propNames[i])
                {
                    //m_bCreateSBMotion = true;
                    if ((bool)values[i])
                    {
                        AddJointChannel(go, propNames[i]);
                    }
                }
            }

            //if (m_OriginalAnimType == ModelImporterAnimationType.Human)
            //{
            //    m_bCreateSBMotion = false;
            //}
        }
        catch (System.Exception e)
        {
            Debug.LogError("OnPostprocessGameObjectWithUserProperties caught an error on propName: "
                + propNames[i] + ". Exception: " + e.Message);
        }
    }

    SmartbodyMotion.JointChannelFlags GetJointChannelFlags(string jointName)
    {
        if (!m_JointChannelUsageMap.ContainsKey(jointName))
        {
            m_JointChannelUsageMap.Add(jointName, new SmartbodyMotion.JointChannelFlags());
        }

        return m_JointChannelUsageMap[jointName];
    }

    void AddJointChannel(GameObject go, string flagName)
    {
        SmartbodyMotion.JointChannelFlags flags = GetJointChannelFlags(go.name);
        SmartbodyAttributes attributes = go.GetComponent<SmartbodyAttributes>();
        if (attributes == null)
        {
            attributes = go.AddComponent<SmartbodyAttributes>();
        }

        switch (flagName)
        {
        case SbodyPosXPropName:
            attributes.AddChannel(SmartbodyMotion.ChannelNames.XPos);
            flags.m_ChannelsToUse.Add(SmartbodyMotion.ChannelNames.XPos);
            break;

        case SbodyPosYPropName:
            attributes.AddChannel(SmartbodyMotion.ChannelNames.YPos);
            flags.m_ChannelsToUse.Add(SmartbodyMotion.ChannelNames.YPos);
            break;

        case SbodyPosZPropName:
            attributes.AddChannel(SmartbodyMotion.ChannelNames.ZPos);
            flags.m_ChannelsToUse.Add(SmartbodyMotion.ChannelNames.ZPos);
            break;

        case SbodyQuatPropName:
            attributes.AddChannel(SmartbodyMotion.ChannelNames.Quat);
            flags.m_ChannelsToUse.Add(SmartbodyMotion.ChannelNames.Quat);
            break;

        default:
            Debug.LogError(string.Format("Couldn't set joint channel flag for flag {0} on joint {1}", flagName, go.name));
            break;
        }
    }

    void CreateSBMotion(GameObject unityModel)
    {
        if (!unityModel.name.Contains("@"))
        {
            //m_bCreateSBMotion = false;
            return;
        }

        // this is the unity assets folder relative path to the unityModel
        //string unityModelRelPath = AssetDatabase.GetAssetPath(unityModel);
        // this is the absolute path to the unity model file
        string unityModelAbsPath = string.Format("{0}/{1}", Application.dataPath, assetImporter.assetPath.Replace("Assets/", ""));

        // create the output folder for which the motions will be placed
        string motionsFolder = string.Format("{0}/Prefabs", Path.GetDirectoryName(unityModelAbsPath));

        string motionPrefab = string.Format("{0}/{1}.prefab", motionsFolder, unityModel.name);
        string motionDataTxt = string.Format("{0}/MotionData/{1}.bytes", motionsFolder, unityModel.name);
        if (File.Exists(motionPrefab) && File.Exists(motionDataTxt))
        {
            DateTime motionLastWriteTime = File.GetLastWriteTimeUtc(motionPrefab);
            DateTime fbxLastWriteTime = File.GetLastWriteTimeUtc(unityModelAbsPath);
            if (motionLastWriteTime > fbxLastWriteTime)
            {
                // no need to import, it's up to date
                //Debug.Log(Path.GetFileNameWithoutExtension(unityModelAbsPath) + " is up-to-date");
                ResetMotionData();
                return;
            }
        }
        if (!Directory.Exists(motionsFolder))
        {
            AssetDatabase.CreateFolder(Path.GetDirectoryName(assetImporter.assetPath), "Prefabs");
        }

        ModelImporter modelImporter = assetImporter as ModelImporter;

        if (modelImporter.animationType == ModelImporterAnimationType.Legacy)
        {
            // this is the right animationtype for being able to read the animation curves, do the conversion
            // convert the unityModel into a smartbody readable format
            FbxToSbmConverter.ConvertFbxToMotion(unityModel, assetImporter.assetPath, m_JointChannelUsageMap, m_SyncPoints, 1.0f);
            //m_bConvertingBack = true;
            modelImporter.animationType = ModelImporterAnimationType.Generic;
        }
        else
        {
            // we're not in the right animation type yet, switch the type and re-import
            modelImporter.animationType = ModelImporterAnimationType.Legacy;
            AssetDatabase.ImportAsset(modelImporter.assetPath);
        }

        // reset the data for the next run through
        ResetMotionData();
    }

    void ResetMotionData()
    {
        m_JointChannelUsageMap.Clear();
        m_SyncPoints.Clear();
        //m_bCreateSBMotion = false;
    }

    private static void PostProcessSkm(string file)
    {
        SbmToFbxConverter.CreateMotionFromSkm(file);
    }

    private static void PostProcessBml(string file)
    {
        string unityModelAbsPath = string.Format("{0}/{1}", Application.dataPath.Replace("Assets/", ""), file);
        string prefabsFolder = string.Format("{0}/Prefabs", Path.GetDirectoryName(unityModelAbsPath));
        if (!Directory.Exists(prefabsFolder))
        {
            // create the output folder for which the prefabs will be placed
            AssetDatabase.CreateFolder(Path.GetDirectoryName(file), "Prefabs");
        }
        BMLConverter.Convert(file);
    }
}
