using UnityEngine;
using System.Collections.Generic;

public class SpeechBox : MonoBehaviour
{
    #region Constants
    // ------> Constants
    public const string SpeechTextFieldName = "Speech Text Field";
    #endregion

    #region Variables
    // ------> Variables
    public VHMsgManager vhmsg;
    public DebugConsole m_Console;
    public FreeMouseLook m_FreeMouseLook;

    string m_SpeechText = "Type here to talk to Brad. Press the M key to toggle between microphone mode and mouse cursor. Press the up arrow to see sample questions.";
    int m_SpeechUserID = 1;
    Rect m_SpeechTextFieldPos = new Rect(0, 0.95f, 0.9f, 0.05f);
    Rect m_SpeechSayButtonPos = new Rect(0.9f, 0.95f, 0.1f, 0.05f);
    List<string> m_SavedSpeech = new List<string>();
    int m_nPreviousSpeechIndex = 0;
    bool m_bShow = true;

    #endregion

    #region Properties
    public bool IsSpeechTextBoxInFocus
    {
        get { return SpeechTextFieldName == GUI.GetNameOfFocusedControl(); }
    }

    public bool Show { get { return m_bShow; } set { m_bShow = value; } }

    #endregion

    #region Functions
    void Start()
    {
        if (m_FreeMouseLook == null)
        {
            m_FreeMouseLook = (FreeMouseLook)Camera.main.GetComponent(typeof(FreeMouseLook));
        }
        //m_SpeechTextFieldPos = new Rect(0, (float)Screen.height * 0.95f, (float)Screen.width * 0.9f, (float)Screen.height * 0.05f);
        //m_SpeechSayButtonPos = new Rect(m_SpeechTextFieldPos.x + m_SpeechTextFieldPos.width,
           // m_SpeechTextFieldPos.y, (float)Screen.width * 0.1f, m_SpeechTextFieldPos.height);

        // add in some default speech text that you can say to brad
        m_SavedSpeech.AddRange(ToolkitText.QuestionsToBrad);
        m_nPreviousSpeechIndex = m_SavedSpeech.Count;
    }

    void Update()
    {
        // speech box not in focus
        if (Input.GetKeyDown(KeyCode.L))
        {
            m_bShow = !m_bShow;
        }
    }

    void HandleInput()
    {
        if (Event.current == null || Event.current.type != EventType.KeyDown)
        {
            return;
        }

        // they are typing in the speech box
        if (IsSpeechTextBoxInFocus)
        {
            if (Event.current.character == '\n')
            {
                SendSpeechMessage(m_SpeechText);
            }
            else if (Event.current.keyCode == KeyCode.UpArrow)
            {
                if (m_nPreviousSpeechIndex > 0)
                {
                    m_SpeechText = m_SavedSpeech[--m_nPreviousSpeechIndex];
                }
            }
            else if (Event.current.keyCode == KeyCode.DownArrow)
            {
                if (m_nPreviousSpeechIndex < m_SavedSpeech.Count - 1)
                {
                    m_SpeechText = m_SavedSpeech[++m_nPreviousSpeechIndex];
                }
            }
        }
    }

    void OnGUI()
    {
        if (!m_bShow)
        {
            return;
        }

        HandleInput();

        // talk to brad and ask him questions
        if (!m_Console.DrawConsole)
        {
            string currentControlName = GUI.GetNameOfFocusedControl();
            GUI.SetNextControlName(SpeechTextFieldName);
            m_SpeechText = VHIMGUI.TextField(m_SpeechTextFieldPos, m_SpeechText);
            if (VHIMGUI.Button(m_SpeechSayButtonPos, "Say"))
            {
                SendSpeechMessage(m_SpeechText);
                GUI.FocusControl(SpeechTextFieldName);

                if (GUI.GetNameOfFocusedControl() == SpeechTextFieldName)
                {
                    HighlightText();
                }
            }

            if ((currentControlName != SpeechTextFieldName && GUI.GetNameOfFocusedControl() == SpeechTextFieldName) 
                || (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                // it wasn't selected before, but now it is so highlight the text
                HighlightText();
            }
            m_FreeMouseLook.enabled = !IsSpeechTextBoxInFocus;
        }
    }

    public void SendSpeechMessage(string message)
    {
        message = message.Replace("\n", "");

        vhmsg.SendVHMsg(string.Format("vrSpeech start user{0} user", m_SpeechUserID));
        vhmsg.SendVHMsg(string.Format("vrSpeech finished-speaking user{0}", m_SpeechUserID));
        vhmsg.SendVHMsg(string.Format("vrSpeech interp user{0} 1 1.0 normal {1}", m_SpeechUserID, message));
        vhmsg.SendVHMsg(string.Format("vrSpeech emotion user{0} 1 1.0 normal neutral", m_SpeechUserID));
        vhmsg.SendVHMsg(string.Format("vrSpeech tone user{0} 1 1.0 normal flat", m_SpeechUserID));
        vhmsg.SendVHMsg(string.Format("vrSpeech asr-complete user{0}", m_SpeechUserID));
        ++m_SpeechUserID;

        if (string.Compare(m_SavedSpeech[m_SavedSpeech.Count - 1], message) != 0)
        {
            m_SavedSpeech.Add(message);
        }

        m_nPreviousSpeechIndex = m_SavedSpeech.Count;
    }

    void HighlightText()
    {
        TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
        textEditor.SelectAll();
    }
    #endregion
}
