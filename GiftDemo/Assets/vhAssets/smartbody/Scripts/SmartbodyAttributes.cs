using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SmartbodyAttributes : MonoBehaviour
{
    #region Variables
    public List<SmartbodyMotion.ChannelNames> m_ChannelsUsed = new List<SmartbodyMotion.ChannelNames>();
    public List<SmartbodyMotion.SyncPoint> m_SyncPoints = new List<SmartbodyMotion.SyncPoint>();
    #endregion

    #region Properties
    public bool HasChannels
    {
        get { return m_ChannelsUsed.Count > 0; }
    }
    #endregion

    #region Functions
    public void AddChannel(SmartbodyMotion.ChannelNames channel)
    {
        if (!m_ChannelsUsed.Contains(channel))
        {
            m_ChannelsUsed.Add(channel);
        }
    }

    public void AddSyncPoint(string name, int frameNum)
    {
        SmartbodyMotion.SyncPoint syncPoint = m_SyncPoints.Find(sp => sp.m_Name == name);
        if (syncPoint != null)
        {
            syncPoint.m_Time = frameNum;
        }
        else
        {
            m_SyncPoints.Add(new SmartbodyMotion.SyncPoint(name, frameNum));
        }
    }

    public bool HasChannel(SmartbodyMotion.ChannelNames channel)
    {
        return m_ChannelsUsed.Contains(channel);
    }
    #endregion
}
