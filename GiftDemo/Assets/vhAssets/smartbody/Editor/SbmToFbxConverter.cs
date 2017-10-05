using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

public class SbmToFbxConverter : EditorWindow
{
    #region Constants
    const string SavedWindowPosXKey = "SbmToFbxConverterWindowX";
    const string SavedWindowPosYKey = "SbmToFbxConverterWindowY";
    const string SavedWindowWKey = "SbmToFbxConverterWindowW";
    const string SavedWindowHKey = "SbmToFbxConverterindowH";
    const string Precision = "f6";
    #endregion

    #region Functions
    //[MenuItem("VH/Skm to SBMotion")]
    static void Init()
    {
        SbmToFbxConverter window = (SbmToFbxConverter)EditorWindow.GetWindow(typeof(SbmToFbxConverter));
        window.autoRepaintOnSceneChange = true;
        window.position = new Rect(PlayerPrefs.GetFloat(SavedWindowPosXKey, 0),
            PlayerPrefs.GetFloat(SavedWindowPosYKey, 0), PlayerPrefs.GetFloat(SavedWindowWKey, 812),
            PlayerPrefs.GetFloat(SavedWindowHKey, 236));
#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0
        window.title = "FbxToSBMotion";
#else
        window.titleContent.text = "FbxToSBMotion";
#endif

        window.Setup();
        window.Show();
    }

    public void Setup()
    {

    }

    void OnDestroy()
    {
        SaveLocation();
    }

    void SaveLocation()
    {
        PlayerPrefs.SetFloat(SavedWindowPosXKey, position.x);
        PlayerPrefs.SetFloat(SavedWindowPosYKey, position.y);
        PlayerPrefs.SetFloat(SavedWindowWKey, position.width);
        PlayerPrefs.SetFloat(SavedWindowHKey, position.height);
    }

    public static bool CreateMotionFromSkm(string file)
    {
        // create a txt file that stores key times and values in the correct channel order
        string fileName = Path.GetFileNameWithoutExtension(file);
        string motionDataFolder = string.Format("{0}/Prefabs/MotionData", Path.GetDirectoryName(file));
        if (!Directory.Exists(motionDataFolder))
        {
            Directory.CreateDirectory(motionDataFolder);
        }

        string motionsFolder = string.Format("{0}/Prefabs", Path.GetDirectoryName(file));
        if (!Directory.Exists(motionsFolder))
        {
            Directory.CreateDirectory(motionsFolder);
        }

        List<float[]> frameDataTable = new List<float[]>();
        string frameDataPath = string.Format("{0}/{1}.bytes", motionDataFolder, Path.GetFileNameWithoutExtension(file));

        // don't create the sb motion if the sb motion is newer than the animation data
        string unitySkmAbsPath = string.Format("{0}/{1}", Application.dataPath, file.Replace("Assets/", ""));
        string motionPrefab = string.Format("{0}/{1}.prefab", motionsFolder, fileName);
        if (File.Exists(motionPrefab) && File.Exists(frameDataPath))
        {
            System.DateTime motionLastWriteTime = File.GetLastWriteTimeUtc(motionPrefab);
            System.DateTime dataLastWriteTime = File.GetLastWriteTimeUtc(frameDataPath);
            System.DateTime fbxLastWriteTime = File.GetLastWriteTimeUtc(unitySkmAbsPath);
            if (motionLastWriteTime > fbxLastWriteTime &&
                dataLastWriteTime > fbxLastWriteTime)
            {
                // no need to import, it's up to date
                //Debug.Log(fileName + " is up to date");
                return false;
            }
        }

        GameObject sbMotionGO = new GameObject(fileName);
        SmartbodyMotion sbMotion = sbMotionGO.AddComponent<SmartbodyMotion>();

        string[] fileLines = File.ReadAllLines(file);// motionData.text.Split('\n');
        string line = "";
        bool readingChannels = false;
        bool readingFrames = false;
        string keyTimeStr = "";
        int currFrame = 0;

        for (int i = 0; i < fileLines.Length; i++)
        {
            line = fileLines[i];
            line = line.Trim();

            if (!readingChannels && !readingFrames && line.Contains("channels"))
            {
                readingChannels = true;
            }
            else if (!readingChannels && !readingFrames && line.Contains("frames"))
            {
                readingChannels = false;
                readingFrames = true;
            }
            else if (readingChannels)
            {
                if (string.IsNullOrEmpty(line) || (line.IndexOf("Pos") == -1  && line.IndexOf("Quat") == -1))
                {
                    readingChannels = false;
                    if (line.Contains("frames"))
                    {
                        readingFrames = true;
                    }
                }
                else
                {
                    sbMotion.AddChannel(line.Trim());
                }
            }
            else if (readingFrames)
            {
                int index = line.IndexOf("fr");
                if (index == -1 || string.IsNullOrEmpty(line))
                {
                    readingFrames = false;
                }
                else
                {
                    string frameHeaderInfo = line.Substring(0, index + 1); // kt [time] fr
                    sbMotion.SetNumFrames(sbMotion.NumFrames + 1);

                    keyTimeStr = frameHeaderInfo.Split(' ')[1]; //[time]
                    //Debug.Log("keyTimeStr: " + keyTimeStr);

                    //writer.WriteLine(keyTimeStr);

                    line = line.Remove(0, index + 3);
                    string[] frameData = line.Split(' ');

                    List<float> frameDataList = new List<float>();

                    int frameDataIndex = 0;
                    int channelIndex = 0;
                    Vector3 axisAngle = new Vector3();
                    while (frameDataIndex < frameData.Length)
                    {
                        if (sbMotion.IsQuatChannel(channelIndex))
                        {
                            // convert the axis angle to a quat
                            //axisAngle.Set(float.Parse(frameData[frameDataIndex]), float.Parse(frameData[frameDataIndex + 1]), float.Parse(frameData[frameDataIndex + 2]));
                            if (!float.TryParse(frameData[frameDataIndex], out axisAngle.x))
                            {
                                Debug.LogError("Failed parsing " + frameData[frameDataIndex] + " in skm " + file);
                            }

                            if (!float.TryParse(frameData[frameDataIndex + 1], out axisAngle.y))
                            {
                                Debug.LogError("Failed parsing " + frameData[frameDataIndex + 1] + " in skm " + file);
                            }

                            if (!float.TryParse(frameData[frameDataIndex + 2], out axisAngle.z))
                            {
                                Debug.LogError("Failed parsing " + frameData[frameDataIndex + 2] + " in skm " + file);
                            }
                            Quaternion rot = VHMath.AxisAngleToQuat(axisAngle);

                            frameDataList.Add(rot.w);
                            frameDataList.Add(rot.x);
                            frameDataList.Add(rot.y);
                            frameDataList.Add(rot.z);

                            frameDataIndex += 3;
                        }
                        else
                        {
                            float currData = 0;
                            if (!float.TryParse(frameData[frameDataIndex], out currData))
                            {
                                Debug.LogError("Failed parsing " + frameData[frameDataIndex] + " in skm " + file);
                            }
                            frameDataList.Add(currData);
                            frameDataIndex += 1;
                        }

                        channelIndex++;
                    }

                    frameDataTable.Add(frameDataList.ToArray());
                    currFrame += 1;
                }
            }
            else if (!readingChannels && !readingFrames && line.Contains("time"))
            {
                string[] syncNameAndTime = line.Split(':');
                sbMotion.AddSyncPoint(ConvertSkmSyncNameToFbxSyncName(syncNameAndTime[0].Trim()), float.Parse(syncNameAndTime[1].Trim()));
            }
        }

        sbMotion.AddSyncPoint("start", 0);
        sbMotion.AddSyncPoint("stop", float.Parse(keyTimeStr.Trim()));

        //writer.Close();

        // To serialize the hashtable and its key/value pairs,
        // you must first open a stream for writing.
        // In this case, use a file stream.
        FileStream fs = new FileStream(frameDataPath, FileMode.Create);

        // Construct a BinaryFormatter and use it to serialize the data to the stream.
        BinaryFormatter formatter = new BinaryFormatter();
        try
        {
            formatter.Serialize(fs, frameDataTable);
        }
        catch (SerializationException e)
        {
            Debug.Log("Failed to serialize. Reason: " + e.Message);
            throw;
        }
        finally
        {
            fs.Close();
        }

        FbxToSbmConverter.ConnectFrameDataToMotion(sbMotion, frameDataPath, Path.GetDirectoryName(file) + "/");

        return true;
    }

    static string ConvertSkmSyncNameToFbxSyncName(string skmSyncPointName)
    {
        string retVal = skmSyncPointName;
        if (skmSyncPointName == "emphasis time")
        {
            retVal = "emphasisTime";
        }
        else if (skmSyncPointName == "ready time")
        {
            retVal = "readyTime";
        }
        else if (skmSyncPointName == "relax time")
        {
            retVal = "relaxTime";
        }
        else if (skmSyncPointName == "strokeStart time")
        {
            retVal = "strokeStartTime";
        }
        else if (skmSyncPointName == "stroke time")
        {
            retVal = "strokeTime";
        }
        else
        {
            Debug.LogError("Failed to convert skmSyncPointName: " + skmSyncPointName);
        }

        return retVal;
    }
    #endregion
}
