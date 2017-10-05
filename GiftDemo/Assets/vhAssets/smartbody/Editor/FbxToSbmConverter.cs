using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

public class FbxToSbmConverter : EditorWindow
{
    #region Constants
    const string SavedWindowPosXKey = "FbxToSbmConverterWindowX";
    const string SavedWindowPosYKey = "FbxToSbmConverterWindowY";
    const string SavedWindowWKey = "FbxToSbmConverterWindowW";
    const string SavedWindowHKey = "FbxToSbmConverterindowH";
    const string Precision = "f6";

    class ChannelData
    {
        public float Time;
        public Quaternion Quat;
        public List<string> parents = new List<string>();
    }

    enum OutputFormat
    {
        //sk,
        skm
    }

    //string[] m_OutputFormats = new string[1] { /*"sk (skeleton)",*/ "SmartbodyMotion" };
    #endregion

    #region Variables
    public SmartbodyManager.SkmMetaData m_SkmMetaData = new SmartbodyManager.SkmMetaData();
    static FbxToSbmConverter ThisWindow;
    //bool[] m_ChannelsAllowed = new bool[(int)SmartbodyMotion.ChannelNames.NUM_CHANNELS] { true, true, true, true };
    #endregion

    #region Functions
    [MenuItem("VH/Convert To SB Motion")]
    static void Init()
    {
        //ConvertToSBMotion();

        ThisWindow = (FbxToSbmConverter)EditorWindow.GetWindow(typeof(FbxToSbmConverter));
        ThisWindow.autoRepaintOnSceneChange = true;
        ThisWindow.position = new Rect(PlayerPrefs.GetFloat(SavedWindowPosXKey, 0),
                                       PlayerPrefs.GetFloat(SavedWindowPosYKey, 0), PlayerPrefs.GetFloat(SavedWindowWKey, 200),
                                       PlayerPrefs.GetFloat(SavedWindowHKey, 200));
    }

    void OnDestroy()
    {
        SaveLocation();
    }

    void SaveLocation()
    {
        if (ThisWindow != null)
        {
            PlayerPrefs.SetFloat(SavedWindowPosXKey, ThisWindow.position.x);
            PlayerPrefs.SetFloat(SavedWindowPosYKey, ThisWindow.position.y);
            PlayerPrefs.SetFloat(SavedWindowWKey, ThisWindow.position.width);
            PlayerPrefs.SetFloat(SavedWindowHKey, ThisWindow.position.height);
        }
    }


    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Convert Selected to Legacy"))
        {
            SBMenuItems.PrepareAnimsForSBMotionConversion();
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Convert Selected to SBMotions"))
        {
            SBMenuItems.ConvertToSBMotion();
        }
    }

    /*
    static public void ConvertToSBMotion()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            string goPath = AssetDatabase.GetAssetPath(go);
            ConvertToSBMotion(goPath);
        }
    }
    */

    static public void ConvertToSBMotion(string path)
    {
        AnimationClip animClip = (AnimationClip)AssetDatabase.LoadAssetAtPath(path, typeof(AnimationClip));
        if (animClip != null)
        {
            ConvertToSBMotion(animClip);
        }
        else
        {
            Debug.LogWarning("Failed to create a SmartbodyMotion from " + path + ". It doesn't have an animation clip");
        }
    }

    public static void ConvertAnimationType(string clipPath, ModelImporterAnimationType animType, ModelImporterAnimationCompression animationCompression)
    {
        ModelImporter modelImporter = AssetImporter.GetAtPath(clipPath) as ModelImporter;
        ModelImporterAnimationType origAnimationType = modelImporter.animationType;
        ModelImporterAnimationCompression origAnimationCompression = modelImporter.animationCompression;

        bool dirty = false;
        if (origAnimationType != animType)
            dirty = true;
        if (origAnimationCompression != animationCompression)
            dirty = true;

        // the old legacy mode has animation curves in it that I read in order to make the conversion possible
        // the new modes don't so I have to convert it
        modelImporter.animationType = animType;
        modelImporter.animationCompression = animationCompression;

        if (dirty)
        {
            AssetDatabase.ImportAsset(clipPath, ImportAssetOptions.ForceUpdate);
        }
    }

    static public void ConvertToSBMotion(AnimationClip clip)
    {
        if (clip == null)
        {
            EditorUtility.DisplayDialog("No Clip Selected", "Select an animation clip to convert", "Ok");
            return;
        }

        string m_SelectedClipPath = AssetDatabase.GetAssetPath(clip);
        GameObject motionGO = (GameObject)AssetDatabase.LoadAssetAtPath(m_SelectedClipPath, typeof(GameObject));


        // don't create the sb motion if the sb motion is newer than the animation data
        string unityModelAbsPath = string.Format("{0}/{1}", Application.dataPath, AssetDatabase.GetAssetPath(motionGO).Replace("Assets/", ""));
        string unityModelMetaAbsPath = string.Format("{0}/{1}.meta", Application.dataPath, AssetDatabase.GetAssetPath(motionGO).Replace("Assets/", ""));
        string motionsFolder = string.Format("{0}/Prefabs", Path.GetDirectoryName(unityModelAbsPath));
        string motionPrefab = string.Format("{0}/{1}.prefab", motionsFolder, motionGO.name);
        string motionDataTxt = string.Format("{0}/MotionData/{1}.bytes", motionsFolder, motionGO.name);
        if (File.Exists(motionPrefab) && File.Exists(motionDataTxt))
        {
            DateTime motionLastWriteTime = File.GetLastWriteTimeUtc(motionPrefab);
            DateTime dataLastWriteTime = File.GetLastWriteTimeUtc(motionDataTxt);
            DateTime fbxLastWriteTime = File.GetLastWriteTimeUtc(unityModelAbsPath);
            DateTime fbxMetaLastWriteTime = File.GetLastWriteTimeUtc(unityModelMetaAbsPath);
            if (motionLastWriteTime > fbxLastWriteTime &&
                dataLastWriteTime > fbxLastWriteTime &&
                motionLastWriteTime > fbxMetaLastWriteTime &&
                dataLastWriteTime > fbxMetaLastWriteTime)
            {
                // no need to import, it's up to date
                //Debug.Log(motionGO.name + " is up to date");
                return;
            }
        }

        string smartbodyMotionsFolder = Path.GetDirectoryName(m_SelectedClipPath) + "/Prefabs";
        string motionDataFolder = smartbodyMotionsFolder + "/MotionData";
        if (!Directory.Exists(smartbodyMotionsFolder))
        {
            AssetDatabase.CreateFolder(Path.GetDirectoryName(m_SelectedClipPath), "Prefabs");
        }
        if (!Directory.Exists(motionDataFolder))
        {
            AssetDatabase.CreateFolder(Path.GetDirectoryName(m_SelectedClipPath) + "Prefabs", "MotionData");
        }

        ModelImporter modelImporter = AssetImporter.GetAtPath(m_SelectedClipPath) as ModelImporter;
        ModelImporterAnimationType originalAnimationType = modelImporter.animationType;
        ModelImporterAnimationCompression originalAnimationCompression = modelImporter.animationCompression;
        ConvertAnimationType(m_SelectedClipPath, ModelImporterAnimationType.Legacy, ModelImporterAnimationCompression.Off);

        Dictionary<string, SmartbodyMotion.JointChannelFlags> jointChannelUsageMap = new Dictionary<string, SmartbodyMotion.JointChannelFlags>();
        Dictionary<string, float> syncPoints = new Dictionary<string, float>();

        // looks through the game object hiearchy for Smartbody attributes on each node
        SmartbodyAttributes[] sbAtts = motionGO.GetComponentsInChildren<SmartbodyAttributes>(true);

        if (sbAtts.Length == 0)
        {
            Debug.LogWarning("Animation " + motionGO.name + " doesn't have any SmartbodyAttributes so all channels (XPos, YPos, ZPos, and Quat) will be set for each joint in the hierarchy.");

            Transform[] children = motionGO.GetComponentsInChildren<Transform>(true);
            List<SmartbodyMotion.ChannelNames> allChannels = new List<SmartbodyMotion.ChannelNames>();
            for (int i = 0; i < (int)SmartbodyMotion.ChannelNames.NUM_CHANNELS; i++)
            {
                allChannels.Add((SmartbodyMotion.ChannelNames)i);
            }

            for (int i = 0; i < children.Length; i++)
            {
                SmartbodyMotion.JointChannelFlags flags = new SmartbodyMotion.JointChannelFlags();
                flags.m_ChannelsToUse.AddRange(allChannels);
                jointChannelUsageMap.Add(children[i].name, flags);
            }
        }
        else
        {
            foreach (SmartbodyAttributes sbAtt in sbAtts)
            {
                foreach (SmartbodyMotion.SyncPoint syncPoint in sbAtt.m_SyncPoints)
                {
                    // in this case, syncPoint.m_Time is the frame number where the sync point would occur
                    if (syncPoints.ContainsKey(syncPoint.m_Name))
                    {
                        syncPoints[syncPoint.m_Name] = syncPoint.m_Time;
                    }
                    else
                    {
                        syncPoints.Add(syncPoint.m_Name, syncPoint.m_Time);
                    }
                }

                // add channels
                SmartbodyMotion.JointChannelFlags flags = new SmartbodyMotion.JointChannelFlags();
                flags.m_ChannelsToUse.AddRange(sbAtt.m_ChannelsUsed);
                jointChannelUsageMap.Add(sbAtt.name, flags);
            }
        }


        ConvertFbxToMotion(motionGO, m_SelectedClipPath, jointChannelUsageMap, syncPoints, 1.0f);

        // forces unity to update the file without having to alt + tab in and out of unity
        //AssetDatabase.ImportAsset(m_SelectedClipPath, ImportAssetOptions.ForceUpdate);

        //Debug.Log("originalAnimationType: " + originalAnimationType);
        if (originalAnimationType != ModelImporterAnimationType.Legacy)
        {
            // preserve the original import animation type setting
            ConvertAnimationType(m_SelectedClipPath, originalAnimationType, originalAnimationCompression);
            //modelImporter.animationType = originalAnimationType;
            //AssetDatabase.ImportAsset(m_SelectedClipPath);
        }
    }

    public static void ConvertFbxToMotion(GameObject motion, string motionGOPath,
        Dictionary<string, SmartbodyMotion.JointChannelFlags> jointChannelUsageMap, Dictionary<string, float> syncPoints, float scale)
    {
        GameObject skeleton = null;
        int atSymbolIndex = motion.name.IndexOf('@');
        if (atSymbolIndex == -1)
        {
            Debug.LogError(string.Format("The name of the skeleton must be in the animation name with an @ suffix. i.e. ChrBrad@Idle01. Failed to create SmartbodyMotion {0}", motion.name));
            return;
        }

        string skeletonName = motion.name.Substring(0, atSymbolIndex);
        skeleton = GameObject.Find(skeletonName);
        if (skeleton == null)
        {
            // the character skeleton isn't in the scene, load it from the project
            string[] skeletonFbxPath = Directory.GetFiles(string.Format("{0}", Application.dataPath), skeletonName + ".fbx", SearchOption.AllDirectories);

            if (skeletonFbxPath.Length == 0 || !string.IsNullOrEmpty(skeletonFbxPath[0]))
            {
                skeletonFbxPath[0] = skeletonFbxPath[0].Replace("\\", "/");
                skeletonFbxPath[0] = skeletonFbxPath[0].Replace(Application.dataPath, "Assets");
                skeleton = (GameObject)AssetDatabase.LoadAssetAtPath(skeletonFbxPath[0], typeof(GameObject));
            }
        }

        if (skeleton == null)
        {
            Debug.LogError(string.Format("No skeleton named {0} found in the scene or folder {1}. Can't create SmartbodyMotion {2}", skeletonName, Path.GetDirectoryName(motionGOPath), motion.name));
            return;
        }

        GameObject sbMotionGO = new GameObject(string.Format("{0}", motion.name));
        SmartbodyMotion sbMotion = sbMotionGO.AddComponent<SmartbodyMotion>();

        List<float[]> frameDataTable = new List<float[]>();
        string frameDataTextAssetPath = string.Format("{0}/Prefabs/MotionData/{1}.bytes", Path.GetDirectoryName(motionGOPath), sbMotionGO.name);
        if (!Directory.Exists(Path.GetDirectoryName(frameDataTextAssetPath)))
        {
            AssetDatabase.CreateFolder(Path.GetDirectoryName(motionGOPath) + "/Prefabs", "MotionData");
        }

        AnimationClip clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(motionGOPath, typeof(AnimationClip));
        if (clip == null)
        {
            AssetDatabase.Refresh();
            clip = (AnimationClip)AssetDatabase.LoadAssetAtPath(motionGOPath, typeof(AnimationClip));
            if (clip == null)
            {
                //Debug.LogError("no animation clip found in " + motionGOPath);
                DestroyImmediate(sbMotionGO);
                return;
            }
        }

        int numFrames = (int)(clip.length / clip.frameRate * 1000.0f) /*- 1*/;
        int frameBufferSize = 0;
        //AnimationClipCurveData[] curveData = AnimationUtility.GetAllCurves(clip, true);
        EditorCurveBinding[] curveData = AnimationUtility.GetCurveBindings(clip);
        //

        // Add all the necessary channels and store timings with frame data
        List<EditorCurveBinding> holder = new List<EditorCurveBinding>(curveData);
        Dictionary<string, List<ChannelData>> channelToQuatMap = new Dictionary<string, List<ChannelData>>();
        for (int i = 0; i < curveData.Length; i++)
        {
            List<EditorCurveBinding> datas = holder.FindAll(a => Path.GetFileNameWithoutExtension(a.path) == Path.GetFileNameWithoutExtension(curveData[i].path));

            for (int j = 0; j < datas.Count; j++)
            {
                SmartbodyMotion.ChannelNames channel = SmartbodyMotion.ChannelNames.Bad_Channel;
                bool isBlendShape = false;
                //Debug.Log("datas[j].curve.keys.Length: " + datas[j].curve.keys[i].);

                if (datas[j].propertyName.Contains("m_LocalPosition.x"))
                {
                    channel = SmartbodyMotion.ChannelNames.XPos;//"XPos";
                }
                else if (datas[j].propertyName.Contains("m_LocalPosition.y"))
                {
                    channel = SmartbodyMotion.ChannelNames.YPos;//"YPos";
                }
                else if (datas[j].propertyName.Contains("m_LocalPosition.z"))
                {
                    channel = SmartbodyMotion.ChannelNames.ZPos;//"ZPos";
                }
                else if (datas[j].propertyName.Contains("m_LocalRotation"))
                {
                    channel = SmartbodyMotion.ChannelNames.Quat;//"Quat";
                }
                else if (datas[j].propertyName.Contains("blendShape"))
                {
                    channel = SmartbodyMotion.ChannelNames.XPos;//"XPos";
                    isBlendShape = true;
                }
                //else
                //{
                //    Debug.Log("datas[j].propertyName: " + datas[j].propertyName);
                //}

                if (channel == SmartbodyMotion.ChannelNames.Bad_Channel)
                {
                    continue;
                }

                string jointName = "";
                if (isBlendShape)
                {
                    // blend shape curves look like this blendShape.<ShapeName>
                    jointName = datas[j].propertyName.Split('.')[1];
                }
                else
                {
                    jointName = Path.GetFileNameWithoutExtension(datas[j].path);
                }

                if (jointChannelUsageMap != null && !isBlendShape && (!jointChannelUsageMap.ContainsKey(jointName) || !jointChannelUsageMap[jointName].m_ChannelsToUse.Contains(channel)))
                {
                    // either the joint doesn't exist or the channel isn't in use
                    continue;
                }

                string jointAndChannel = "";
                jointAndChannel = string.Format("{0} {1}", jointName, channel.ToString());

                if (!channelToQuatMap.ContainsKey(jointAndChannel))
                {
                    channelToQuatMap.Add(jointAndChannel, new List<ChannelData>());
                    sbMotion.AddChannel(jointAndChannel);
                    frameBufferSize += channel == SmartbodyMotion.ChannelNames.Quat ? 4 : 1;
                }

                ChannelData channelData = null;
                bool newKey = false;
                for (int k = 0; k < numFrames; k++)
                {
                    int frame = k;

                    if (frame < channelToQuatMap[jointAndChannel].Count)
                    {
                        // this channel is already in use
                        channelData = channelToQuatMap[jointAndChannel][frame];
                        newKey = false;
                    }
                    else
                    {
                        // new channel to add
                        channelData = new ChannelData();
                        newKey = true;
                    }

                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, datas[j]);

                    channelData.Time = ((float)(k)) / (float)30.0f;// keyframe.time;
                    if (datas[j].propertyName.Contains("m_LocalRotation.x"))
                    {
                        channelData.Quat.x = curve.Evaluate(channelData.Time);
                    }
                    else if (datas[j].propertyName.Contains("m_LocalRotation.y"))
                    {
                        channelData.Quat.y = -curve.Evaluate(channelData.Time);
                    }
                    else if (datas[j].propertyName.Contains("m_LocalRotation.z"))
                    {
                        channelData.Quat.z = -curve.Evaluate(channelData.Time);
                    }
                    else if (datas[j].propertyName.Contains("m_LocalRotation.w"))
                    {
                        channelData.Quat.w = curve.Evaluate(channelData.Time);
                    }
                    else if (datas[j].propertyName.Contains("m_LocalPosition.x"))
                    {
                        //channelData.Quat.x = (-datas[j].curve.Evaluate(channelData.Time) - datas[j].curve.Evaluate(0)) * scale;
                        channelData.Quat.x = (-curve.Evaluate(channelData.Time)) * scale;
                    }
                    else if (datas[j].propertyName.Contains("m_LocalPosition.y"))
                    {
                        //channelData.Quat.y = (datas[j].curve.Evaluate(channelData.Time) - datas[j].curve.Evaluate(0)) * scale;
                        channelData.Quat.y = (curve.Evaluate(channelData.Time)) * scale;
                    }
                    else if (datas[j].propertyName.Contains("m_LocalPosition.z"))
                    {
                        //channelData.Quat.z = (datas[j].curve.Evaluate(channelData.Time) - datas[j].curve.Evaluate(0)) * scale;
                        channelData.Quat.z = (curve.Evaluate(channelData.Time)) * scale;
                    }
                    else if (datas[j].propertyName.Contains("blendShape"))
                    {
                        channelData.Quat.x = curve.Evaluate(channelData.Time);
                    }
                    else
                    {
                        Debug.LogError(string.Format("channel {0} isn't handled", datas[j].propertyName));
                    }

                    if (newKey)
                    {
                        channelToQuatMap[jointAndChannel].Add(channelData);
                    }
                }

                holder.Remove(datas[j]);
            }
        }

        // add the frame data
        //int frameBufferIndex = 0;
        sbMotion.SetNumFrames(numFrames);
        float currFrame = 0;
        for (int currKey = 0; currKey < numFrames; currKey++)
        {
            //frameBufferIndex = 0;
            bool lineBegin = true;
            //float[] frameData = new float[frameBufferSize];
            // write out the frame time and frame data for each frame for each channel
            //float frameTime = 0;
            List<float> frameDataList = new List<float>();

            foreach (KeyValuePair<string, List<ChannelData>> kvp2 in channelToQuatMap)
            {
                if (lineBegin)
                {
                    //frameTime = kvp2.Value[currKey].Time;
                    //writer.WriteLine(frameTime.ToString("f2"));
                    lineBegin = false;
                }

                // we need to find the skeleton that this animation is based off of in order to find
                // the delta positons and rotation
                GameObject baseSkeletonJoint = VHUtils.FindChildRecursive(skeleton, kvp2.Key.Split(' ')[0]); //GameObject.Find(kvp2.Key.Split(' ')[0]);
                Vector3 basePos = Vector3.zero;
                if (baseSkeletonJoint == null)
                {
                    // blend shapes won't be able to find the joint
                    //Debug.LogError(string.Format("Couldn't find joint with name {0} in skeleton {1}. Ignore this error if {0} is a blend shape", kvp2.Key.Split(' ')[0], skeleton.name));
                }
                else
                {
                    basePos = baseSkeletonJoint.transform.localPosition;
                }

                if (kvp2.Key.Contains("XPos"))
                {
                    // switch to sb's coordinate system
                    frameDataList.Add(kvp2.Value[currKey].Quat.x - (-basePos.x));
                }
                else if (kvp2.Key.Contains("YPos"))
                {
                    frameDataList.Add(kvp2.Value[currKey].Quat.y - basePos.y);
                }
                else if (kvp2.Key.Contains("ZPos"))
                {
                    frameDataList.Add(kvp2.Value[currKey].Quat.z - basePos.z);
                }
                else if (kvp2.Key.Contains("Quat"))
                {
                    Quaternion baseQuat = baseSkeletonJoint.transform.localRotation;
                    // switch to sb's coordinate system
                    baseQuat.y *= -1;
                    baseQuat.z *= -1;

                    // unity store's the absolute local rotation in the animation curve data but Smartbody only wants the delta from the base pose (pre-rot).
                    // Because of this, we have to strip out the rotation using the inverse quat of the base skeleton joint. This gives us the delta between child and parent
                    Quaternion quat = Quaternion.Inverse(baseQuat) * kvp2.Value[currKey].Quat;
                    frameDataList.Add(quat.w);
                    frameDataList.Add(quat.x);
                    frameDataList.Add(quat.y);
                    frameDataList.Add(quat.z);
                }
            }

            // add the frame buffer
            frameDataTable.Add(frameDataList.ToArray());
            currFrame += 1;
        }

        // To serialize the hashtable and its key/value pairs,
        // you must first open a stream for writing.
        // In this case, use a file stream.
        FileStream fs = new FileStream(frameDataTextAssetPath, FileMode.Create);

        // Construct a BinaryFormatter and use it to serialize the data to the stream.
        BinaryFormatter formatter = new BinaryFormatter();
        try
        {
            formatter.Serialize(fs, frameDataTable);
        }
        catch (SerializationException e)
        {
            Debug.Log("Failed to serialize. Reason: " + e.Message);
        }
        finally
        {
            fs.Close();
        }

        // Add the sync points
        foreach (KeyValuePair<string, float> kvp in syncPoints)
        {
            sbMotion.AddSyncPoint(kvp.Key, (float)((float)kvp.Value / (float)numFrames) * clip.length);
        }

        // start and stop aren't in the fbx meta data
        sbMotion.AddSyncPoint(SmartbodyMotion.StartSyncPointName, 0);
        sbMotion.AddSyncPoint(SmartbodyMotion.StopSyncPointName, clip.length);

        // this is so unity can recognize the creation of the text file.
        // if I don't do this, then unity doesn't think the asset exists when
        // I try to load it with LoadAssetAtPath the first time the file is created.
        ConnectFrameDataToMotion(sbMotion, frameDataTextAssetPath, motionGOPath);
    }

    public static void ConnectFrameDataToMotion(SmartbodyMotion sbMotion, string textAssetPath, string motionGOPath)
    {
        AssetDatabase.Refresh();
        sbMotion.m_FrameData = (TextAsset)AssetDatabase.LoadAssetAtPath(textAssetPath, typeof(TextAsset));
        if (sbMotion.m_FrameData == null)
        {
            Debug.LogError(sbMotion.name + " has no frame data linked to it");
        }

        // this updates the inspector
        EditorUtility.SetDirty(sbMotion);

        // create the prefab in the output dir
        string prefabPath = string.Format("{0}/Prefabs/{1}.prefab", Path.GetDirectoryName(motionGOPath), sbMotion.name); // TODO: FIX HARD CODED MOTIONS
        GameObject previousMotionPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
        if (previousMotionPrefab == null)
        {
            PrefabUtility.CreatePrefab(prefabPath, sbMotion.gameObject, ReplacePrefabOptions.ReplaceNameBased);
        }
        else
        {
            PrefabUtility.ReplacePrefab(sbMotion.gameObject, previousMotionPrefab, ReplacePrefabOptions.ReplaceNameBased);
        }

        DestroyImmediate(sbMotion.gameObject);

        // update the project assets
#if UNITY_5_5_OR_NEWER
        AssetDatabase.SaveAssets();
#else
        EditorApplication.SaveAssets();
#endif
    }
    #endregion

}
