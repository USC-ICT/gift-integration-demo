using UnityEngine;
using System.Collections.Generic;

#if !UNITY_WEBPLAYER && !UNITY_IPHONE && !UNITY_ANDROID

/// <remarks>
/// VHMsgManager implements the C# VHMsg interface within Unity.  It is disabled in Web Player because
/// the Network namespace is not available in the Web Player.  See VHMsgEmulator.
/// </remarks>
public class VHMsgManager : VHMsgBase
{
    public string m_Host = "";
    public string m_Scope = "";
    public string m_Port = "";
    public string[] m_messagesToSendAtQuit;

    protected VHMsg.Client vhmsg;

    public override void AddMessageEventHandler(MessageEventHandler handler)
    {
        m_RegisteredMessageCallbacks.Add(handler);
    }

    public override void RemoveMessageEventHandler(MessageEventHandler handler)
    {
        m_RegisteredMessageCallbacks.Remove(handler);
    }

    public bool IsVHMsgNull
    {
        get { return vhmsg == null; }
    }

    override public void Awake()
    {
        OpenConnection();
    }

    void MessageEventTranslatorCallBack(object sender, VHMsg.Message args)
    {
        // convert the message from VHMsg.Message to VHMsgBase.Message
        VHMsgBase.Message baseMessage = new Message(args.s, args.properties);

        for (int i = 0; i < m_RegisteredMessageCallbacks.Count; i++)
        {
            m_RegisteredMessageCallbacks[i].Invoke(m_RegisteredMessageCallbacks[i].Target, baseMessage);
        }
    }

    override public void Update()
    {
        vhmsg.Poll();
    }

    public void OnApplicationQuit()
    {
        for (int i = 0; i < m_messagesToSendAtQuit.Length; i++)
        {
            SendVHMsg(m_messagesToSendAtQuit[i]);
        }

        CloseConnection();
    }

    void OpenConnection()
    {
        CloseConnection();

        vhmsg = new VHMsg.Client();

        if (!string.IsNullOrEmpty(m_Scope))
            vhmsg.Scope = m_Scope;

        if (string.IsNullOrEmpty(m_Host) && string.IsNullOrEmpty(m_Port))
        {
            vhmsg.OpenConnection();
        }
        else if (string.IsNullOrEmpty(m_Port))
        {
            vhmsg.OpenConnection(m_Host);
        }
        else
        {
            vhmsg.OpenConnection(m_Host, m_Port);
        }

        vhmsg.EnablePollingMethod();

        vhmsg.MessageEvent += new VHMsg.Client.MessageEventHandler(MessageEventTranslatorCallBack);
    }

    public void CloseConnection()
    {
        if (vhmsg != null)
        {
            m_RegisteredMessageCallbacks.Clear();
            vhmsg.CloseConnection();
            //vhmsg.Dispose();
            vhmsg = null;
        }
    }

    void OnEnable()
    {
        OpenConnection();
    }

    void OnDisable()
    {
        CloseConnection();
    }

    public void OnDestroy()
    {
        CloseConnection();
    }

    public override void SubscribeMessage(string req)
    {
        vhmsg.SubscribeMessage(req);
    }

    public override void SendVHMsg(string opandarg)
    {
        if (vhmsg != null)
        {
            vhmsg.SendMessage(opandarg);
        }
    }

    public override void SendVHMsg(string op, string args)
    {
        if (vhmsg != null)
        {
            vhmsg.SendMessage(op, args);
        }
    }

    public override void SendVHMsg(string op, string[] args)
    {
        if (vhmsg != null)
        {
            vhmsg.SendMessage(op, args);
        }
    }

    public override void ReceiveVHMsg(string opandarg)
    {
        Debug.LogError("ReceiveVHMsg shouldn't be getting called on VHMsg Manager");
    }
}

#else

public class VHMsgManager : VHMsgBase
{
    public string m_Host = "";
    public string m_Scope = "";
    public string m_Port = "";
    public string[] m_messagesToSendAtQuit;

    public bool IsVHMsgNull
    {
        get { return false; }
    }
    public override void AddMessageEventHandler(MessageEventHandler handler) {}
    public override void RemoveMessageEventHandler(MessageEventHandler handler) {}
    override public void Awake() {}
    override public void Update() { }
    public void OnApplicationQuit() {}
    override public void SubscribeMessage(string req) {}
    override public void SendVHMsg(string opandarg) {}
    override public void SendVHMsg(string op, string args) {}
    override public void SendVHMsg(string op, string[] args) {}
    override public void ReceiveVHMsg(string opandarg){}
}

#endif
