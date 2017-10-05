using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MaterialCustomizer : MonoBehaviour
{
    #region Constants
    public const float SliderWidth = 200;

#if false
    [System.Serializable]
    public class MaterialData
    {
        public Material m_Mat;
        public string m_PropertyName = "_Color";
        public string m_DisplayName = "";

        [HideInInspector]
        public Color m_OriginalColor = new Color();
    }
#endif
    #endregion
    
    #region Variables
    public bool m_Draw = false;
    //public List<MaterialData> m_MaterialColorDatas = new List<MaterialData>();
    //public List<MaterialData> m_MaterialFloatDatas = new List<MaterialData>();
    Vector2 m_ScrollViewPos = Vector2.zero;

    [System.Serializable]
    public class MaterialDataV2
    {
        public Material m_material;
        public string m_materialName;
    }

    [System.Serializable]
    public class MaterialCustomizeDataV2
    {
        public SkinnedMeshRenderer m_target;
        public int m_targetMaterialIndex;
        public string m_targetName;
        public List<MaterialDataV2> m_materialData = new List<MaterialDataV2>();
    }

    public List<MaterialCustomizeDataV2> m_materialDataV2 = new List<MaterialCustomizeDataV2>();

    #endregion

    #region Functions
    void Start()
    {
#if false
        // save the initial colors
        foreach (MaterialData matData in m_MaterialColorDatas)
        {
            matData.m_OriginalColor = matData.m_Mat.GetColor(matData.m_PropertyName);
        }

        //save initial floats
        foreach (MaterialData matData in m_MaterialFloatDatas)
        {
            matData.m_OriginalColor.a = matData.m_Mat.GetFloat(matData.m_PropertyName);
        }
#endif
    }

    public void Draw()
    {
#if false
        foreach (MaterialData matData in m_MaterialColorDatas)
        {
            GUILayout.Label(string.Format("{0} {1}", matData.m_Mat.name, matData.m_PropertyName));
            Color color = matData.m_Mat.GetColor(matData.m_PropertyName);
            
            color.r = DrawFloatSlider("Red", color.r, 0, 1);
            color.g = DrawFloatSlider("Green", color.g, 0, 1);
            color.b = DrawFloatSlider("Blue", color.b, 0, 1);
            color.a = DrawFloatSlider("Alpha", color.a, 0, 1);

            matData.m_Mat.SetColor(matData.m_PropertyName, color);

            GUILayout.Space(15);
        }

        foreach (MaterialData matData in m_MaterialFloatDatas)
        {
            GUILayout.Label(string.Format("{0} {1}", matData.m_Mat.name, matData.m_PropertyName));
            float data = matData.m_Mat.GetFloat(matData.m_PropertyName);

            data = DrawFloatSlider(matData.m_DisplayName, data, 0, 1);
            matData.m_Mat.SetFloat(matData.m_PropertyName, data);

            GUILayout.Space(15);
        }
#endif
    }

    void OnGUI()
    {
        if (!m_Draw)
        {
            return;
        }

        GUILayout.BeginVertical();
        {
            GUILayout.BeginScrollView(m_ScrollViewPos);
            {
                Draw();
            }
            GUILayout.EndScrollView();
        }
        GUILayout.EndVertical();
    }

    float DrawFloatSlider(string name, float current, float min, float max)
    {
        float retVal = 0;
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label(name);
            retVal = GUILayout.HorizontalSlider(current, min, max, GUILayout.Width(SliderWidth));
        }
        GUILayout.EndHorizontal();

        return retVal;
    }

    void OnApplicationQuit()
    {
#if false
        // reset all the colors back to what they were when the scene first started
        foreach (MaterialData matData in m_MaterialColorDatas)
        {
            if (matData.m_Mat.GetColor(matData.m_PropertyName) != matData.m_OriginalColor)
            {
                matData.m_Mat.SetColor(matData.m_PropertyName, matData.m_OriginalColor);
            } 
        }

        // reset
        foreach (MaterialData matData in m_MaterialFloatDatas)
        {
            if (matData.m_Mat.GetFloat(matData.m_PropertyName) != matData.m_OriginalColor.a)
            {
                matData.m_Mat.SetFloat(matData.m_PropertyName, matData.m_OriginalColor.a);
            }
        }
#endif
    }

    public void SetColor(string displayName, Color color)
    {
#if false
        foreach (MaterialData matData in m_MaterialColorDatas)
        {
            if (matData.m_DisplayName == displayName)
            {
                matData.m_Mat.SetColor(matData.m_PropertyName, color);
            }
        }
#endif
    }

    public void SetFloat(string displayName, float val)
    {
#if false
        foreach (MaterialData matData in m_MaterialFloatDatas)
        {
            if (matData.m_DisplayName == displayName)
            {
                matData.m_Mat.SetFloat(matData.m_PropertyName, val);
            }
        }
#endif
    }
    #endregion
}
