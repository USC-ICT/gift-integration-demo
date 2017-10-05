using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class ImportWindow : EditorWindow
{
    #region Constants
    const string SavedWindowPosXKey = "ImportWindowX";
    const string SavedWindowPosYKey = "ImportWindowY";
    const string SavedWindowWKey = "ImportWindowW";
    const string SavedWindowHKey = "ImportWindowH";
    const string LastFolderPathKey = "LastUtteranceFolder";

    const string PlayAudio = "Play Audio";
    const string StopSaccade = "Stop Saccades";
    const string GazeAdvanced = "Gaze Advanced";
    const string Posture1 = "Posture 1";
    const string Posture2 = "Posture 2";
    const string Nod = "Nod";
    const string Shake = "Shake";
    const string FAC1 = "FAC 1";
    const string FAC2 = "FAC 2";
    const string FAC4 = "FAC 4";
    const string FAC7 = "FAC 7";
    const string PlayAnim = "Play Anim";
    const string VRSpoke = "VR Spoke";
    const string Saccade = "Saccade";
    const string StopGaze = "Stop Gaze";


    ImportEvent[] StandardEvents = new ImportEvent[]
    {
        new ImportEvent(PlayAudio, true),
        new ImportEvent(StopSaccade, true),
        new ImportEvent(GazeAdvanced, true, new ImportEventParameter[] { new ImportEventParameter("Gaze Target", "camera") }),
        new ImportEvent(Posture1, true, new ImportEventParameter[] { new ImportEventParameter("Posture Name", "") } ),
        new ImportEvent(Posture2, true, new ImportEventParameter[] { new ImportEventParameter("Posture Name", "") }),
        new ImportEvent(PlayAnim, true, new ImportEventParameter[] { new ImportEventParameter("Anim Name", "") }),
        new ImportEvent(Nod, true),
        new ImportEvent(Shake, true),
        new ImportEvent(FAC1, true),
        new ImportEvent(FAC2, true),
        new ImportEvent(FAC4, true),
        new ImportEvent(FAC7, true), 
        new ImportEvent(VRSpoke, true),
        new ImportEvent(Saccade, true),
        new ImportEvent(StopGaze, true),
    };

    class ImportData
    {
        public string m_CharacterName = "";
        public bool m_UseSmartbodyEvents = false;
    }

    class ImportEvent
    {
        public string m_DisplayName = "";
        public bool m_Use = true;
        public ImportEventParameter[] m_Params;

        public ImportEvent(string displayName, bool use)
        {
            m_DisplayName = displayName;
            m_Use = use;
        }

        public ImportEvent(string displayName, bool use, ImportEventParameter[] p)
        {
            m_DisplayName = displayName;
            m_Use = use;
            m_Params = p;
        }
    }

    public class ImportEventParameter
    {
        public enum Type
        {
            String,
            Int,
            Bool,
            Float,
        }

        public string m_DisplayName = "";
        public string m_ValueStr = "";
        public Type m_Type;

        public ImportEventParameter(string displayName, string valueStr)
        {
            m_DisplayName = displayName;
            m_ValueStr = valueStr;
            m_Type = Type.String;
        }
    }



    #endregion

    #region Variables
    public CutsceneEditor m_CutsceneEditorWindow;
    ImportData m_ImportData = new ImportData();
    //List<ImportEvent> m_ImportEvents = new List<ImportEvent>();
    #endregion

    #region Functions
    /*[MenuItem("VH/Importer")]
    public static void Init()
    {
        ImportWindow window = (ImportWindow)EditorWindow.GetWindow(typeof(ImportWindow));
        window.Setup(0, 0);
        window.ShowPopup();
        window.titleContent.text = "Importer";
    }*/


    public static void Init(Rect windowPos, CutsceneEditor cutsceneEditorWindow)
    {
        ImportWindow window = (ImportWindow)EditorWindow.GetWindow(typeof(ImportWindow));
        window.Setup(windowPos.x, windowPos.y);
        window.ShowPopup();
        window.titleContent.text = "Importer";
        window.m_CutsceneEditorWindow = cutsceneEditorWindow;
        cutsceneEditorWindow.SequencerIO.AddOnCreatedCutsceneCallback(window.OnImportedCutscene);
    }

    public void Setup(float xPos, float yPos)
    {
        position = new Rect(PlayerPrefs.GetFloat(SavedWindowPosXKey, 0),
           PlayerPrefs.GetFloat(SavedWindowPosYKey, 0), PlayerPrefs.GetFloat(SavedWindowWKey, 600),
           PlayerPrefs.GetFloat(SavedWindowHKey, 380));

    }

    void OnDestroy()
    {
        SaveLocation();
        m_CutsceneEditorWindow.SequencerIO.RemoveOnCreatedCutsceneCallback(OnImportedCutscene);
        //Debug.Log(position);
    }

    void SaveLocation()
    {
        PlayerPrefs.SetFloat(SavedWindowPosXKey, position.x);
        PlayerPrefs.SetFloat(SavedWindowPosYKey, position.y);
        PlayerPrefs.SetFloat(SavedWindowWKey, position.width);
        PlayerPrefs.SetFloat(SavedWindowHKey, position.height);
    }

    void OnGUI()
    {
        GUILayout.Label("Characters", EditorStyles.boldLabel);
        m_ImportData.m_CharacterName = EditorGUILayout.TextField("Character Name", m_ImportData.m_CharacterName);
        m_ImportData.m_UseSmartbodyEvents = EditorGUILayout.Toggle("Use Smartbody Events", m_ImportData.m_UseSmartbodyEvents);

        GUILayout.Label("Events", EditorStyles.boldLabel);

        for (int i = 0; i < StandardEvents.Length; i += 1)
        {
            GUILayout.BeginHorizontal();

            ImportEvent left = StandardEvents[i];
            DrawStandardEvent(left);
            /*if (i + 1 < StandardEvents.Length)
            {
                ImportEvent right = StandardEvents[i + 1];
                DrawStandardEvent(right);
            }*/

            GUILayout.EndHorizontal();
        }


        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Batch Import"))
        {
            m_CutsceneEditorWindow.AudioSpeechFileBatchImport();
        }
        if (GUILayout.Button("Single Import"))
        {
            m_CutsceneEditorWindow.RequestFileOpenAudioSpeechFile();
        }
        GUILayout.EndHorizontal();
    }

    void DrawStandardEvent(ImportEvent e)
    {
        e.m_Use = EditorGUILayout.Toggle(e.m_DisplayName, e.m_Use, GUILayout.Width(200));

        if (e.m_Params != null)
        {
            foreach (ImportEventParameter p in e.m_Params)
            {
                switch (p.m_Type)
                {
                    case ImportEventParameter.Type.String:
                        p.m_ValueStr = EditorGUILayout.TextField(p.m_DisplayName, p.m_ValueStr);
                        break;
                }
            }
            
        }
    }
   

    void OnImportedCutscene(Cutscene cutscene)
    {
        string cutsceneName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(m_CutsceneEditorWindow.LastFileImported));
        m_CutsceneEditorWindow.ChangeCutsceneName(cutscene.CutsceneName, cutsceneName);
        

        foreach (ImportEvent importEvent in StandardEvents)
        {
            if (!importEvent.m_Use)
            {
                continue;
            }

            m_CutsceneEditorWindow.AddTrackGroup();

            CutsceneEvent ce = null;
            if (importEvent.m_DisplayName == StopSaccade)
            {
                ce = CreateStandardEvent("StopSaccade", 0);
            }
            else if (importEvent.m_DisplayName == GazeAdvanced)
            {
                ce = CreateStandardEvent("GazeAdvanced", 0);
                ce.FindParameter("headSpeed").floatData = 100;
                ce.FindParameter("eyeSpeed").floatData = 50;
                ce.FindParameter("gazeTarget").stringData = importEvent.m_Params[0].m_ValueStr;
            }
            else if (importEvent.m_DisplayName == Posture1 || importEvent.m_DisplayName == Posture2)
            {
                ce = CreateStandardEvent("Posture", 1);
                ce.FindParameter("motion").stringData = importEvent.m_Params[0].m_ValueStr;
            }
            else if (importEvent.m_DisplayName == Nod)
            {
                ce = CreateStandardEvent("Nod", 0);
                ce.FindParameter("amount").floatData = 0.08f;
                ce.FindParameter("time").floatData = 0.5f;
                ce.FindParameter("repeats").floatData = 0.5f;
            }
            else if (importEvent.m_DisplayName == Shake)
            {
                ce = CreateStandardEvent("Shake", 0);
                ce.FindParameter("amount").floatData = 0.07f;
                ce.FindParameter("time").floatData = 1.0f;
                ce.FindParameter("repeats").floatData = 2.0f;
            }
            else if (importEvent.m_DisplayName == FAC1 || importEvent.m_DisplayName == FAC2
                || importEvent.m_DisplayName == FAC4 || importEvent.m_DisplayName == FAC7)
            {
                string resultString = Regex.Match(importEvent.m_DisplayName, @"\d+").Value;

                ce = CreateStandardEvent("PlayFAC", 0);
                ce.FindParameter("au").intData = int.Parse(resultString);
                ce.FindParameter("weight").floatData = 0.6f;
                ce.FindParameter("duration").floatData = 0.5f;
            }
            else if (importEvent.m_DisplayName == PlayAnim)
            {
                ce = CreateStandardEvent("PlayAnim", 0);
                ce.FindParameter("motion").stringData = importEvent.m_Params[0].m_ValueStr;
            }
            else if (importEvent.m_DisplayName == Saccade)
            {
                ce = CreateStandardEvent("Saccade", 0);
            }
            else if (importEvent.m_DisplayName == StopGaze)
            {
                ce = CreateStandardEvent("StopGaze", 0);
            }
            else if (importEvent.m_DisplayName == PlayAudio)
            {
                ce = CreateStandardEvent("PlayAudio", 2);
                ce.Name = ce.m_Params[1].stringData = cutscene.CutsceneName;
            }

            if (ce != null)
            {
                SetupCharacterParam(ce, m_ImportData.m_CharacterName);
            }
        }

    }

    CutsceneEvent CreateStandardEvent(string functionName, int overload)
    {
        return CreateStandardEvent(functionName, overload, m_ImportData.m_UseSmartbodyEvents ? GenericEventNames.SmartBody : GenericEventNames.Mecanim);
    }

    CutsceneEvent CreateStandardEvent(string functionName, int overload, string eventType)
    {
        CutsceneEvent ce = new CutsceneEvent(new Rect(), Guid.NewGuid().ToString());
        BMLParser.ChangedCutsceneEventType(eventType, ce);
        ce.ChangedEventFunction(functionName, overload);
        ce = m_CutsceneEditorWindow.SequencerIO.AddEvent(ce);
        ce.Name = functionName;
        return ce;
    }

    void SetupCharacterParam(CutsceneEvent ce, string charName)
    {
        if (!string.IsNullOrEmpty(m_ImportData.m_CharacterName))
        {
            CutsceneEventParam characterParam = ce.FindParameter("character");
            if (characterParam != null)
            {
                characterParam.stringData = charName;
            }
        }
    }

    #endregion
}
