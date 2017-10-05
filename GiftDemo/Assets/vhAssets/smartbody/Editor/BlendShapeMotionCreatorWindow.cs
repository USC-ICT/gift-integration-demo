using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

public class BlendShapeMotionCreatorWindow : EditorWindow
{
    #region Constants
    const string SavedWindowPosXKey = "BlendShapeMotionCreatorWindowX";
    const string SavedWindowPosYKey = "BlendShapeMotionCreatorWindowY";
    const string SavedWindowWKey = "BlendShapeMotionCreatorWindowW";
    const string SavedWindowHKey = "BlendShapeMotionCreatorWindowH";
    const string Precision = "f6";
    const float OneOverThirty = 1.0f / 30.0f;
    #endregion

    #region Variables
    SkinnedMeshRenderer m_SkinnedMeshRenderer;
    int m_FramesPerBlendShape = 30;
    #endregion

    #region Functions
    [MenuItem("VH/BlendShape Motion Creator")]
    static void Init()
    {
        BlendShapeMotionCreatorWindow window = (BlendShapeMotionCreatorWindow)EditorWindow.GetWindow(typeof(BlendShapeMotionCreatorWindow));
        window.autoRepaintOnSceneChange = true;
        window.position = new Rect(PlayerPrefs.GetFloat(SavedWindowPosXKey, 0),
            PlayerPrefs.GetFloat(SavedWindowPosYKey, 0), PlayerPrefs.GetFloat(SavedWindowWKey, 812),
            PlayerPrefs.GetFloat(SavedWindowHKey, 236));
#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0
        window.title = "BlendShapeMotionCreator";
#else
        window.titleContent.text = "BlendShapeMotionCreator";
#endif
        window.Show();
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
        m_SkinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh", m_SkinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);
        m_FramesPerBlendShape = EditorGUILayout.IntField("Frames Per BlendShape", m_FramesPerBlendShape);

        if (GUILayout.Button("Create Motions"))
        {
            CreateMotions();
        }
    }

    void CreateMotions()
    {
        string outputFolder = EditorUtility.SaveFolderPanel("Motions", "Prefabs", "Prefabs");
        if (string.IsNullOrEmpty(outputFolder))
        {
            return;
        }

        int numBlendShapes = m_SkinnedMeshRenderer.sharedMesh.blendShapeCount;
        for (int blendShapeIndex = 0; blendShapeIndex < numBlendShapes; blendShapeIndex++)
        {
            string blendShapeName = m_SkinnedMeshRenderer.sharedMesh.GetBlendShapeName(blendShapeIndex);
            blendShapeName = UnitySmartbodyCharacter.FixJointName(blendShapeName);
            CreateMotion(outputFolder, blendShapeName);
        }
    }

    void CreateMotion(string outputDir, string blendShapeName)
    {
        // create a motion
        GameObject sbMotionGO = new GameObject(string.Format("{0}", blendShapeName));
        SmartbodyMotion sbMotion = sbMotionGO.AddComponent<SmartbodyMotion>();
        sbMotion.AddChannel(blendShapeName + " XPos");
        sbMotion.SetNumFrames(m_FramesPerBlendShape);

        string frameDataTextAssetPath = string.Format("{0}/MotionData/{1}.txt",outputDir, sbMotionGO.name);
        if (!Directory.Exists(Path.GetDirectoryName(frameDataTextAssetPath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(frameDataTextAssetPath));
        }

        StreamWriter writer = new StreamWriter(frameDataTextAssetPath);

        for (int frameIndex = 0; frameIndex < m_FramesPerBlendShape; frameIndex++)
        {
            // time
            //writer.WriteLine(((float)frameIndex / (float)m_FramesPerBlendShape).ToString(Precision));
            writer.WriteLine(((float)frameIndex * OneOverThirty).ToString(Precision));

            // frame data
            writer.WriteLine(((float)frameIndex / (float)m_FramesPerBlendShape * 100.0f).ToString(Precision));
        }

        // start and stop aren't in the fbx meta data
        float clipLength = (float)(m_FramesPerBlendShape - 1) * OneOverThirty;
        sbMotion.AddSyncPoint("readyTime", clipLength * 0.25f);
        sbMotion.AddSyncPoint("strokeStartTime", clipLength * 0.5f);
        sbMotion.AddSyncPoint("emphasisTime", clipLength * 0.5f);
        sbMotion.AddSyncPoint("strokeTime", clipLength * 0.5f);
        sbMotion.AddSyncPoint("relaxTime", clipLength * 0.75f);

        sbMotion.AddSyncPoint(SmartbodyMotion.StartSyncPointName, 0);
        sbMotion.AddSyncPoint(SmartbodyMotion.StopSyncPointName, clipLength);

        writer.Close();

        FbxToSbmConverter.ConnectFrameDataToMotion(sbMotion, frameDataTextAssetPath.Replace(Application.dataPath, "Assets"), outputDir.Replace(Application.dataPath, "Assets"));
    }
    #endregion
}
