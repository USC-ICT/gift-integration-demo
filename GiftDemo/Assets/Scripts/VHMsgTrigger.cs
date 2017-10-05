using UnityEngine;
using System.Collections;

public class VHMsgTrigger : MonoBehaviour
{
    #region Variables   
    public string[] m_OnEnterMessages;
    public string[] m_OnExitMessages;
    public string[] m_OnStayMessages;
    #endregion

    #region Functions
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("VHMsgTrigger::OnTriggerEnter");
        foreach (string msg in m_OnEnterMessages)
        {
            VHMsgBase.Get().SendVHMsg(msg);
        }
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("VHMsgTrigger::OnTriggerExit");
        foreach (string msg in m_OnExitMessages)
        {
            VHMsgBase.Get().SendVHMsg(msg);
        }
    }

    void OnTriggerStay(Collider other)
    {
        foreach (string msg in m_OnStayMessages)
        {
            VHMsgBase.Get().SendVHMsg(msg);
        }
    }
    #endregion
}
