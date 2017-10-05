using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class MarkupTesterWindow : EditorWindow
{
    #region Constants
    const string SavedWindowPosXKey = "MarkupTesterWindowX";
    const string SavedWindowPosYKey = "MarkupTesterWindowY";
    const string SavedWindowWKey = "MarkupTesterWindowW";
    const string SavedWindowHKey = "MarkupTesterWindowH";
    const string LastXmlPathKey = "MarkupTesterLastXmlPath";
    #endregion

    #region Variables
    UnitySmartbodyCharacter m_SelectedCharacter;
    Vector2 m_ScrollPos = new Vector2();
    List<string> m_LoadedMarkupFullPaths = new List<string>();
    List<string> m_LoadedMarkupFileNames = new List<string>();
    List<string> m_FilteredMarkupFullPaths = new List<string>();
    List<string> m_FilteredMarkupFileNames = new List<string>();
    string m_Filter = "";
    #endregion

    #region Functions
    [MenuItem("VH/Markup Tester")]
    static void Init()
    {
        MarkupTesterWindow window = (MarkupTesterWindow)EditorWindow.GetWindow(typeof(MarkupTesterWindow));
        window.autoRepaintOnSceneChange = true;
        window.position = new Rect(PlayerPrefs.GetFloat(SavedWindowPosXKey, 0),
            PlayerPrefs.GetFloat(SavedWindowPosYKey, 0), PlayerPrefs.GetFloat(SavedWindowWKey, 390),
            PlayerPrefs.GetFloat(SavedWindowHKey, 305));
#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0
        window.title = "Markup Tester";
#else
        window.titleContent.text = "Markup Tester";
#endif
        window.Setup();
        window.Show();
    }

    void Setup()
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

    void OnGUI()
    {
        // sb characer list
        m_SelectedCharacter = (UnitySmartbodyCharacter)EditorGUILayout.ObjectField("Selected Character", m_SelectedCharacter, typeof(UnitySmartbodyCharacter), true);

        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Select Your Markup Folder");
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                OpenXmlFolder();
            }
        }
        GUILayout.EndHorizontal();

        string prevFilter = m_Filter;
        m_Filter = EditorGUILayout.TextField("Filter", m_Filter);
        if (prevFilter != m_Filter)
        {
            MatchFilter(m_Filter);
        }

        m_ScrollPos = GUILayout.BeginScrollView(m_ScrollPos);
        {
            for (int i = 0; i < m_FilteredMarkupFileNames.Count; i++)
            {
                if (GUILayout.Button(m_FilteredMarkupFileNames[i]))
                {
                    TestMarkup(m_SelectedCharacter, m_FilteredMarkupFullPaths[i]);
                }
            }
        }
        GUILayout.EndScrollView();
    }

    void MatchFilter(string filter)
    {
        m_FilteredMarkupFullPaths.Clear();
        m_FilteredMarkupFileNames.Clear();
        for (int i = 0; i < m_LoadedMarkupFileNames.Count; i++)
        {
            if (Regex.IsMatch(m_LoadedMarkupFileNames[i], filter, RegexOptions.IgnoreCase))
            {
                m_FilteredMarkupFullPaths.Add(m_LoadedMarkupFullPaths[i]);
                m_FilteredMarkupFileNames.Add(m_LoadedMarkupFileNames[i]);
            }
        }
    }

    void OpenXmlFolder()
    {
        string folder = EditorUtility.OpenFolderPanel("Select Markup Folder", PlayerPrefs.GetString(LastXmlPathKey, Application.streamingAssetsPath), "");
        if (!string.IsNullOrEmpty(folder))
        {
            PlayerPrefs.SetString(LastXmlPathKey, Path.GetDirectoryName(folder));
            string[] xmlFiles = Directory.GetFiles(folder, "*.xml");

            m_LoadedMarkupFullPaths.Clear();
            m_LoadedMarkupFileNames.Clear();
            m_FilteredMarkupFullPaths.Clear();
            m_FilteredMarkupFileNames.Clear();

            for (int i = 0; i < xmlFiles.Length; i++)
            {
                m_LoadedMarkupFullPaths.Add(xmlFiles[i].Replace("\\", "/"));
                m_LoadedMarkupFileNames.Add(Path.GetFileNameWithoutExtension(xmlFiles[i]));

                m_FilteredMarkupFullPaths.Add(m_LoadedMarkupFullPaths[i]);
                m_FilteredMarkupFileNames.Add(m_LoadedMarkupFileNames[i]);
            }
        }
    }

    void TestMarkup(UnitySmartbodyCharacter character, string markupPath)
    {
        if (!Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Error", "The scene has to be running in order to test the markup", "Ok");
            return;
        }

        if (m_SelectedCharacter == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a character in the scene to play the mark up on", "Ok");
            return;
        }

        if (string.IsNullOrEmpty(markupPath) || Path.GetExtension(markupPath) != ".xml")
        {
            EditorUtility.DisplayDialog("Error", "Please select an xml file by using the ... button", "Ok");
            return;
        }

        SmartbodyManager.Get().SBPlayXml(character.SBMCharacterName, markupPath);
    }
    #endregion
}
