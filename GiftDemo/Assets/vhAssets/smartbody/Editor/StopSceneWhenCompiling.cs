using UnityEditor;
using UnityEngine;

/// <summary>
/// This class stops the unity scene from playing whenever script changes are made while a scene is playing
/// </summary>
[InitializeOnLoad]
class StopSceneWhenCompiling
{
    static StopSceneWhenCompiling()
    {
        EditorApplication.update += Update;
    }

    static void Update()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorApplication.isPlaying && EditorApplication.isCompiling)
            {
                EditorApplication.isPlaying = false;
                Debug.LogWarning("Stopped playmode because compilation started");
            }
        }
    }
}
