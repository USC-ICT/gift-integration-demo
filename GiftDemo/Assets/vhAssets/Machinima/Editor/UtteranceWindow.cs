using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class UtteranceWindow : EditorWindow
{
    #region Constants
    const string SavedWindowPosXKey = "UtteranceWindowX";
    const string SavedWindowPosYKey = "UtteranceWindowY";
    const string SavedWindowWKey = "UtteranceWindowW";
    const string SavedWindowHKey = "UtteranceWindowH";
    const string LastFolderPathKey = "LastUtteranceFolder";
    const float ButtonSize = 200;
    readonly string[] Blank = new string[] { "" };

    class UtteranceData
    {
        public string name;
        public string text;

        public UtteranceData(string _name, string _text)
        {
            name = _name;
            text = _text;
        }
    }

    #endregion

    #region Variables
    public string m_CharacterName = "";
    public string m_Target = "user";
    public string m_Label = "";
    public string m_vrSpeak = "";
    public bool m_CreateCutscenes = true;
    List<UtteranceData> m_Utterances = new List<UtteranceData>();
    string[] m_UtteranceNames;
    int m_Id;
    int m_SelectedUtterance;
    //bool m_ShouldBeClosed;
    bool m_ShowResponse;
    public CutsceneEditor m_CutsceneEditorWindow;
    bool m_BatchMode = false;
    #endregion

    #region Functions
    public static void Init()
    {
        UtteranceWindow window = (UtteranceWindow)EditorWindow.GetWindow(typeof(UtteranceWindow));
        window.Setup(0, 0);
        window.ShowPopup();
    }

    public static void Init(Rect windowPos, CutsceneEditor cutsceneEditorWindow)
    {
        UtteranceWindow window = (UtteranceWindow)EditorWindow.GetWindow(typeof(UtteranceWindow));
        window.Setup(windowPos.x, windowPos.y);
        window.ShowPopup();
        window.m_CutsceneEditorWindow = cutsceneEditorWindow;
        cutsceneEditorWindow.SequencerIO.AddOnCreatedCutsceneCallback(window.OnCreatedCutscene);
        //cutsceneEditorWindow.ListenToNVBG(true);
    }

   public void Setup(float xPos, float yPos)
    {
        position = new Rect(PlayerPrefs.GetFloat(SavedWindowPosXKey, 0),
           PlayerPrefs.GetFloat(SavedWindowPosYKey, 0), PlayerPrefs.GetFloat(SavedWindowWKey, 422),
           PlayerPrefs.GetFloat(SavedWindowHKey, 350));

        //if (Application.isPlaying)
        {
            if (VHMsgBase.Get() != null)
            {
                VHMsgBase.Get().Awake();
                VHMsgBase.Get().SubscribeMessage("vrSpeak");
                VHMsgBase.Get().AddMessageEventHandler(VHMsg_MessageEventHandler);
            }
            else
            {
                EditorUtility.DisplayDialog("Missing VHMsgManager", "Place the VHsgManager Prefab in the scene", "Ok");
            }
        }
        
    }

    void OnCreatedCutscene(Cutscene cutscene)
    {
        m_CutsceneEditorWindow.ChangeCutsceneName(m_CutsceneEditorWindow.GetSelectedCutscene().CutsceneName, m_Utterances[m_SelectedUtterance].name);
        m_CutsceneEditorWindow.Repaint();

        if (m_BatchMode)
        {
            m_SelectedUtterance += 1;
            if (m_SelectedUtterance >= m_UtteranceNames.Length)
            {
                // we're finished
                m_SelectedUtterance = m_UtteranceNames.Length - 1;
                m_BatchMode = false;
            }
            else
            {
                Send();
            }
        }
    }

    void OnDestroy()
    {
        SaveLocation();

        if (Application.isPlaying)
        {
            VHMsgBase.Get().RemoveMessageEventHandler(VHMsg_MessageEventHandler);
        }  
    }

    void SaveLocation()
    {
        PlayerPrefs.SetFloat(SavedWindowPosXKey, position.x);
        PlayerPrefs.SetFloat(SavedWindowPosYKey, position.y);
        PlayerPrefs.SetFloat(SavedWindowWKey, position.width);
        PlayerPrefs.SetFloat(SavedWindowHKey, position.height);
        //Debug.Log(position);
    }

    void Update()
    {
        if (VHMsgBase.Get() != null)
        {
            VHMsgBase.Get().Update();
        }
    }

    void DrawUtteranceInfo(bool disabled)
    {
        EditorGUI.BeginDisabledGroup(disabled);
        if (!disabled)
        {
            UtteranceData utt = m_Utterances[m_SelectedUtterance];
            m_SelectedUtterance = EditorGUILayout.Popup("Utterance", m_SelectedUtterance, m_UtteranceNames);

            GUILayout.Label("Utterance Text");
            utt.text = EditorGUILayout.TextArea(utt.text, GUILayout.Height(100));
        }
        else
        {
            m_SelectedUtterance = EditorGUILayout.Popup("Utterance", 0, Blank);

            GUILayout.Label("Utterance Text");
            EditorGUILayout.TextArea("Load an utterance", GUILayout.Height(100));
        }

        EditorGUI.EndDisabledGroup();
    }

    void OnGUI()
    {
        GUILayout.Label(m_Label);
        m_CharacterName = EditorGUILayout.TextField("Character Name", m_CharacterName);
        m_Target = EditorGUILayout.TextField("Target", m_Target);

        bool utterancesLoaded = m_UtteranceNames != null && m_UtteranceNames.Length > 0 && m_Utterances.Count > 0;
        DrawUtteranceInfo(!utterancesLoaded);

        m_CutsceneEditorWindow.SequencerIO.BmlSubDir = EditorGUILayout.TextField("Bml Sub Directory", m_CutsceneEditorWindow.SequencerIO.BmlSubDir);
        /*EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        bool temp = m_CreateCutscenes;
        m_CreateCutscenes = EditorGUILayout.Toggle("Create Cutscene", m_CreateCutscenes);
        if (temp != m_CreateCutscenes)
        {
            m_CutsceneEditorWindow.ListenToNVBG(m_CreateCutscenes);
        }
        EditorGUI.EndDisabledGroup();*/


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Transcript File", GUILayout.Width(ButtonSize)))
        {
            RequestFileOpenUtteranceText(false);
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Open Audio Speech Prefab", GUILayout.Width(ButtonSize)))
        {
            RequestFileOpenAudioSpeechFile();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Master Transcript File", GUILayout.Width(ButtonSize)))
        {
            RequestFileOpenUtteranceText(true);
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Open Audio Speech Prefab Batch", GUILayout.Width(ButtonSize)))
        {
            RequestBatchFileOpenAudioSpeechFile();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Cutscene", GUILayout.Width(ButtonSize)))
        {
            Send();
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Clear Utterances", GUILayout.Width(ButtonSize)))
        {
            ClearUtterances();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Generate Cutscene Batch", GUILayout.Width(ButtonSize)))
        {
            SendBatch();
        }

        /*GUILayout.Label("Response");
        m_ShowResponse = EditorGUILayout.Foldout(m_ShowResponse, m_vrSpeak);
        if (m_ShowResponse)
        {
            EditorGUILayout.TextField(m_vrSpeak, GUILayout.Height(300));
        }*/
        
    }

    void RequestFileOpenUtteranceText(bool isMasterList)
    {
        string filepath = EditorUtility.OpenFilePanel("Open Utterance Text File", string.Format("{0}", PlayerPrefs.GetString(LastFolderPathKey, Application.dataPath)), "txt");
        if (!string.IsNullOrEmpty(filepath))
        {
            if (!isMasterList)
            {
                FileOpenUtteranceText(filepath);
            }
            else
            {
                FileOpenMasterUtteranceText(filepath);
            }
        }
    }

    void RequestFileOpenAudioSpeechFile()
    {
        string filepath = EditorUtility.OpenFilePanel("Open Audio Speech Prefab", string.Format("{0}", PlayerPrefs.GetString(LastFolderPathKey, Application.dataPath)), "prefab");
        if (!string.IsNullOrEmpty(filepath))
        {
            AudioSpeechFile asf = m_CutsceneEditorWindow.FileLoadAudioSpeechFile(filepath);
            FileOpenAudioSpeechFile(asf);
        }
    }

    void RequestBatchFileOpenAudioSpeechFile()
    {
        string folderName = EditorUtility.OpenFolderPanel("Batch Open Audio Speech Files", string.Format("{0}", PlayerPrefs.GetString(LastFolderPathKey, Application.dataPath)), "");
        if (!string.IsNullOrEmpty(folderName))
        {
            string[] filepaths = Directory.GetFiles(folderName, "*.prefab");
            foreach (string filepath in filepaths)
            {
                AudioSpeechFile asf = m_CutsceneEditorWindow.FileLoadAudioSpeechFile(filepath);
                FileOpenAudioSpeechFile(asf);
            }
        }
    }

    void AddUtterance(string uttName, string uttText, bool updateUttList)
    {
       int index = m_Utterances.FindIndex(u => u.name == uttName);
        if (index != -1)
        {
            m_Utterances[index].text = uttText;
            m_SelectedUtterance = index;
        }
        else
        {
            m_Utterances.Add(new UtteranceData(uttName, uttText));
            m_SelectedUtterance = m_Utterances.Count - 1;
        }

        if (updateUttList)
        {
            UpdateUtteranceList();
        }
    }

    void UpdateUtteranceList()
    {
        m_UtteranceNames = m_Utterances.Select(utt => utt.name).ToArray();
    }

    void ClearUtterances()
    {
        Debug.Log("ClearUtterances");
        m_Utterances.Clear();
    }

    void FileOpenUtteranceText(string filePathAndName)
    {
        string utteranceText = File.ReadAllText(filePathAndName);
        AddUtterance(Path.GetFileNameWithoutExtension(filePathAndName), utteranceText, true);
        UpdateUtteranceList();
    }

    void FileOpenMasterUtteranceText(string filePathAndName)
    {
        string[] lines = File.ReadAllLines(filePathAndName);
        if (lines == null || lines.Length ==0)
        {
            EditorUtility.DisplayDialog("Couldn't read file", "The file is malformed", "Ok");
            return;
        }

        foreach (string line in lines)
        {
            char splitStr = '\n';
            string[] lineParts = line.Split(splitStr);

            if (lineParts.Length >= 2)
            {
                string uttName = lineParts[0];
                string transcript = lineParts[1];
                AddUtterance(uttName, transcript, false);
            }
            else
            {
                Debug.LogError("Problem reading line: " + line + ". No tab delimeter found");
            }
        }

        UpdateUtteranceList();
    }

    void FileOpenAudioSpeechFile(AudioSpeechFile asf)
    {
        if (asf != null)
        {
            if (asf.m_UtteranceText != null)
            {
                AddUtterance(asf.name, asf.UtteranceText, true);
            }
            else
            {
                EditorUtility.DisplayDialog("Missing Utterance File", "There is no utterance file associated with " + asf.name, "Ok");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Couldn't read file", "The prefab that you opened doesn't have an AudioSpeechFile componenet attached", "Ok");
        }
    }

    void Send()
    {
        Send(m_Utterances[m_SelectedUtterance].name, m_Utterances[m_SelectedUtterance].text);
    }

    void Send(string uttName, string uttText)
    {
        if (string.IsNullOrEmpty(m_CharacterName))
        {
            EditorUtility.DisplayDialog("Missing Character Name", string.Format("Enter the name of the character (example: BradPrefab)"), "Ok");
        }
        else if (string.IsNullOrEmpty(uttName))
        {
            EditorUtility.DisplayDialog("Missing Utterance Name", string.Format("Enter an utterance name (example: \"brad_hello\""), "Ok");
        }
        else if (string.IsNullOrEmpty(uttText))
        {
            EditorUtility.DisplayDialog("Missing Utterance Text", string.Format("Enter some utterance text (example: \"Hello world!\")"), "Ok");
        }
        else if (string.IsNullOrEmpty(m_Target))
        {
            EditorUtility.DisplayDialog("Missing Target", string.Format("Enter a target name (example: \"user\")"), "Ok");
        }
        else
        {
            if (/*Application.isPlaying &&*/ VHMsgBase.Get() != null)
            {
                Express(m_CharacterName, uttName, m_Id.ToString(), uttText, m_Target);
                m_Id += 1;
            }
            else
            {
                EditorUtility.DisplayDialog("Not running", string.Format("The application must be playing and you must have VHMsgManager in the scene"), "Ok");
            }
        }
    }


    void SendBatch()
    {
        m_BatchMode = true;
        Send();
    }


    public void Express(string character, string uttID, string expressId, string text, string target)
    {
        
        string uttIDModified = uttID;
        if (string.IsNullOrEmpty(uttID))
            uttIDModified = DateTime.Now.ToString("yyyyMMddHHmmssffff");

        string expressIdModified = expressId;
        if (string.IsNullOrEmpty(expressId))
            expressIdModified = DateTime.Now.ToString("yyyyMMddHHmmssffff");

        string message = string.Format("vrExpress {0} {4} {2} <?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>"
            + "<act><participant id=\"{0}\" role=\"actor\" /><fml><turn start=\"take\" end=\"give\" /><affect type=\"neutral\" "
            + "target=\"addressee\"></affect><culture type=\"neutral\"></culture><personality type=\"neutral\"></personality></fml>"
            + "<bml><speech id=\"sp1\" ref=\"{1}\" type=\"application/ssml+xml\">{3}</speech></bml></act>", character, uttIDModified, expressIdModified, text, target);
        VHMsgBase.Get().SendVHMsg(message);
    }

    void VHMsg_MessageEventHandler(object sender, VHMsgBase.Message message)
    {
        string[] splitargs = message.s.Split(" ".ToCharArray());
        if (splitargs[0] == "vrSpeak")
        {
            m_vrSpeak = String.Join(" ", splitargs, 4, splitargs.Length - 4);
            Repaint();

            // add a new cutscene first so that we don't overwrite our current one
            m_CutsceneEditorWindow.AddCutscene();

            string character = splitargs[1];
            string xml = String.Join(" ", splitargs, 4, splitargs.Length - 4);
            m_CutsceneEditorWindow.SequencerIO.LoadXMLString(character, xml);
            
        }
    }
    #endregion
}
