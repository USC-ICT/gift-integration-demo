using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class EditorMotionSet : EditorWindow
{
    SmartbodyMotionSet m_selectedMotionSet;


    [MenuItem("VH/MotionSet Editor")]
    static void Init()
    {
        EditorMotionSet window = (EditorMotionSet)EditorWindow.GetWindow(typeof(EditorMotionSet));
        window.autoRepaintOnSceneChange = true;
        //window.position = new Rect(PlayerPrefs.GetFloat(SavedWindowPosXKey, 0),
        //    PlayerPrefs.GetFloat(SavedWindowPosYKey, 0), PlayerPrefs.GetFloat(SavedWindowWKey, 435),
        //    PlayerPrefs.GetFloat(SavedWindowHKey, 309));

        window.Show();
    }


    void OnGUI()
    {
        EditorGUILayout.BeginVertical();


        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();


        EditorGUILayout.LabelField("Choose Smartbody Motion Set to modify:");

        EditorGUILayout.Space();

        m_selectedMotionSet = (SmartbodyMotionSet)EditorGUILayout.ObjectField("Motion Set", m_selectedMotionSet, typeof(SmartbodyMotionSet), true);


        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Clear Motions from Motion Set"))
        {
            m_selectedMotionSet.m_MotionsList = new SmartbodyMotion[0];
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Sort Motions in Motion Set"))
        {
            List<SmartbodyMotion> newList = new List<SmartbodyMotion>(m_selectedMotionSet.m_MotionsList);
            newList.Sort((a, b) => string.Compare(a.name, b.name));
            m_selectedMotionSet.m_MotionsList = newList.ToArray();
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Select your motion Prefabs in the Project Window,");
        EditorGUILayout.LabelField("and then select one of the buttons below:");

        if (GUILayout.Button("Add Selected Motions to Motion Set"))
        {
            List<SmartbodyMotion> selectedMotions = GetSelectedMotions();

            List<SmartbodyMotion> newList = new List<SmartbodyMotion>(m_selectedMotionSet.m_MotionsList);
            newList.AddRange(selectedMotions);
            m_selectedMotionSet.m_MotionsList = newList.ToArray();

            Debug.Log(string.Format("{0} motions added to {1} motion set", selectedMotions.Count, m_selectedMotionSet.name));
        }

        if (GUILayout.Button("Replace Motion Set with Selected Motions"))
        {
            List<SmartbodyMotion> selectedMotions = GetSelectedMotions();

            m_selectedMotionSet.m_MotionsList = selectedMotions.ToArray();

            Debug.Log(string.Format("{0} motions added to {1} motion set, removing existing motions", selectedMotions.Count, m_selectedMotionSet.name));
        }


        EditorGUILayout.Space();
        EditorGUILayout.Space();


        GUI.enabled = false;
        if (GUILayout.Button("Apply Changes to MotionSet Prefab (currently must be done manually)"))
        {
            //PrefabUtility.ReplacePrefab();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Save Project"))
        {
#if UNITY_5_5_OR_NEWER
            AssetDatabase.SaveAssets();
#else
            EditorApplication.SaveAssets();
#endif
        }


        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Close", GUILayout.Width(100)))
        {
            Close();
        }

        EditorGUILayout.EndVertical();
    }


    void OnDestroy()
    {
        SaveLocation();
    }


    void SaveLocation()
    {
    }


    List<SmartbodyMotion> GetSelectedMotions()
    {
        List<SmartbodyMotion> selectedMotions = new List<SmartbodyMotion>();
        UnityEngine.Object [] selectedObjects = Selection.GetFiltered(typeof(SmartbodyMotion), SelectionMode.Assets);
        foreach (UnityEngine.Object obj in selectedObjects)
        {
            if (obj is SmartbodyMotion)
            {
                selectedMotions.Add((SmartbodyMotion)obj);
            }
        }

        return selectedMotions;
    }
}
