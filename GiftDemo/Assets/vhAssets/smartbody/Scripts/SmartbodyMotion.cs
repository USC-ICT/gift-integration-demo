using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;


public class SmartbodyMotion : MonoBehaviour
{
    #region Constants
    public enum ChannelNames
    {
        XPos,
        YPos,
        ZPos,
        Quat,

        NUM_CHANNELS,
        Bad_Channel
    }

    enum LoadState
    {
        NotLoaded,
        Loading,
        Loaded,
        Error
    }

    public const string StartSyncPointName = "start";
    public const string StopSyncPointName = "stop";

    public class JointChannelFlags
    {
        public List<ChannelNames> m_ChannelsToUse = new List<ChannelNames>();
    }

    /// <summary>
    /// Stores data for an individual frame of the animation
    /// </summary>
    [Serializable]
    public class Frame
    {
        // time of the animations
        public float m_Time;

        // all joint data in the correct order for the frame
        public float[] m_Data;

        public Frame(float _time, float[] _data)
        {
            m_Time = _time;
            //Array.Copy(_data, m_Data, _data.Length);
            m_Data = _data;
        }
    }

    [Serializable]
    public class SyncPoint
    {
        public string m_Name;
        public float m_Time;

        public SyncPoint(string _name, float _time)
        {
            m_Name = _name;
            m_Time = _time;
        }
    }

    [Serializable]
    public class MotionData
    {
        public List<string> m_Channels = new List<string>();

        //public List<Frame> m_Frames = new List<Frame>();

        public int m_NumFrames = 0;

        public List<SyncPoint> m_SyncPoints = new List<SyncPoint>();
    }

    #endregion

    #region Properties
    List<string> Channels
    {
        get { return m_MotionData.m_Channels; }
    }

    List<SyncPoint> SyncPoints
    {
        get { return m_MotionData.m_SyncPoints; }
    }

    //List<Frame> Frames
    //{
    //    get { return m_MotionData.m_Frames; }
    //}

    public int NumFrames
    {
        get { return m_MotionData.m_NumFrames;/*m_MotionData.m_Frames.Count;*/ }
    }

    public float MotionLength
    {
        get { return GetSyncPointTime(StopSyncPointName); }
    }

    public string MotionName
    {
        get { return name; }
    }

    public bool IsLoaded
    {
        get { return m_LoadState == LoadState.Loaded; }
    }

    public bool IsLoadError
    {
        get { return m_LoadState == LoadState.Error; }
    }

    public int NumChannels
    {
        get { return Channels.Count; }
    }
    #endregion

    #region Variables
    public MotionData m_MotionData = new MotionData();
    public SmartbodyCharacterInit m_MotionFinishedLoadingReceiver;
    public string m_MotionFinishedLoadingCallback = "";
    public bool m_LogLoadingTime = false;
    public TextAsset m_FrameData;
    LoadState m_LoadState = LoadState.NotLoaded;
    public bool m_SaveAsSkm;
    public bool m_IsPosture = false;
    #endregion

    #region Functions
    void Start()
    {

    }

    public void AddChannel(string channel)
    {
        if (!Channels.Contains(channel))
        {
            Channels.Add(channel);
        }
        else
        {
            Debug.LogError(string.Format("SmartbodyMotion {0} already contains channel {1}", name, channel));
        }
    }

    public bool IsQuatChannel(int channelIndex)
    {
        return Channels.Count > channelIndex && channelIndex >= 0 && Channels[channelIndex].IndexOf("Quat") != -1;
    }

    public void SetNumFrames(int numFrames)
    {
        m_MotionData.m_NumFrames = numFrames;
    }

    public void AddSyncPoint(string syncPointName, float time)
    {
        SyncPoint syncPoint = SyncPoints.Find(sp => sp.m_Name == syncPointName);
        if (syncPoint == null)
        {
            SyncPoints.Add(new SyncPoint(syncPointName, time));
        }
        else
        {
            syncPoint.m_Time = time;
            Debug.LogError(string.Format("SmartbodyMotion {0} already contains sync point {1}", name, syncPointName));
        }
    }

    //public void AddFrame(float frameTime, float[] frameData)
    //{
    //    if (NumFrames > 0 && Frames[NumFrames - 1].m_Time > frameTime)
    //    {
    //        Debug.LogError(string.Format("SmartbodyMotion {0} needs frames to be added in order based on frameTime. stored frametime {1} incoming frameTime {2}", name, Frames[NumFrames - 1].m_Time, frameTime));
    //        return;
    //    }
    //    Frames.Add(new Frame(frameTime, frameData));
    //}

    public void Load(string skeletonName, string skeletonMap)
    {
        StartCoroutine(LoadMotionCoroutine(skeletonName, skeletonMap));
    }

    public IEnumerator LoadStreaming(string skeletonName, string skeletonMap)
    {
        yield return StartCoroutine(LoadMotionCoroutine(skeletonName, skeletonMap));
    }

    IEnumerator LoadMotionCoroutine(string skeletonName, string skeletonMap)
    {
        SmartbodyManager sbm = SmartbodyManager.Get();
        if (sbm == null)
        {
            Debug.LogError(string.Format("Failed to load motion {0} because there is not SmartBodyManager in the scene", MotionName));
            m_LoadState = LoadState.Error;
            yield break;
        }

        while (m_LoadState == LoadState.Loading)
        {
            // the motion is already in the process of being loaded.
            // wait for the loading to finish and then continue
            yield return new WaitForEndOfFrame();
        }

        if (m_LoadState == LoadState.NotLoaded)
        {
            //float timeSpentInThisFunction = Time.timeSinceLevelLoad;
            m_LoadState = LoadState.Loading;
            DateTime originalStartTime = DateTime.Now;
            DateTime startTime = DateTime.Now;

            if (!sbm.CreateMotion(MotionName))
            {
                // motion already loaded or failed to load
                Debug.LogWarning(string.Format("Failed to load motion {0} because it has already been loaded", MotionName));
                m_LoadState = LoadState.Error;
                yield break;
            }

            if (m_LogLoadingTime)
            {
                Debug.Log(string.Format("Started loading motion {0} in {1} seconds", MotionName, (DateTime.Now - startTime).TotalSeconds.ToString("f3")));
                startTime = DateTime.Now;
            }

            // add channels
            sbm.AddMotionChannels(MotionName, Channels, skeletonMap);

            //if (m_LogLoadingTime)
            //{
            //    Debug.LogWarning(string.Format("Finished Adding motion channels {0} in {1} seconds", MotionName, (DateTime.Now - startTime).TotalSeconds.ToString("f3")));
            //    startTime = DateTime.Now;
            //}

            //if (m_LogLoadingTime)
            //{
            //    Debug.LogWarning(string.Format("PART1 Finished reading frame data {0} in {1} seconds", MotionName, (DateTime.Now - startTime).TotalSeconds.ToString("f3")));
            //    startTime = DateTime.Now;
            //}

            List<float[]> frameVals = VHUtils.DeserializeBytes<List<float[]>>(m_FrameData.bytes);

            const float oneOverThirty = 1.0f / 30.0f;

            for (int i = 0; i < frameVals.Count; i++)
            {
                sbm.AddMotionFrame(MotionName, (float)i * oneOverThirty, frameVals[i]);
            }

            //if (m_LogLoadingTime)
            //{
            //    Debug.LogWarning(string.Format("Finished adding frame data {0} in {1} seconds", MotionName, (DateTime.Now - startTime).TotalSeconds.ToString("f3")));
            //    startTime = DateTime.Now;
            //}

            // add sync points
            for (int i = 0; i < SyncPoints.Count; i++)
            {
                sbm.AddMotionSyncPoint(MotionName, SyncPoints[i].m_Name, SyncPoints[i].m_Time);
            }

            if (m_LogLoadingTime)
            {
                Debug.Log(string.Format("Finished loading motion {0} in {1} seconds", MotionName, (DateTime.Now - originalStartTime).TotalSeconds.ToString("f3")));
            }
        }

        // for re-targetting
        sbm.SetMotionSkeleton(MotionName, skeletonName);

        sbm.ApplyMotion(MotionName, skeletonMap);

        if (m_SaveAsSkm)
        {
            sbm.SaveMotionToSkm(MotionName);
        }

        if (m_MotionFinishedLoadingReceiver != null && !string.IsNullOrEmpty(m_MotionFinishedLoadingCallback))
        {
            m_MotionFinishedLoadingReceiver.SendMessage(m_MotionFinishedLoadingCallback, this);
        }

        m_LoadState = LoadState.Loaded;
    }

    public float GetSyncPointTime(string syncPointName)
    {
        SyncPoint syncPoint = SyncPoints.Find(sp => sp.m_Name == syncPointName);
        float time = 0;
        if (syncPoint != null)
        {
            time = syncPoint.m_Time;
        }
        return time;
    }

    public void ResetLoadFlag()
    {
        //Debug.Log("ResetLoadFlag() - " + MotionName);

        m_LoadState = LoadState.NotLoaded;
    }
    #endregion
}
