using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class MainMenu : MonoBehaviour
{
    AsyncOperation m_loadingLevelStatus = null;
    RectTransform m_imageLoadingResizeRectTransform;
    float m_loadingBarMaxWidth = 0;

    void Awake()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        if (VHGlobals.m_mainMenuCount == 0)
        {
            // only do this the first time we enter MainMenu
            // only jump to a scene if showIntro or startScene is set to something

            string startScene = VHGlobals.m_startScene;

            if (VHGlobals.m_showIntro)
            {
                if (string.IsNullOrEmpty(startScene))
                {
                    startScene = "Campus";
                }
            }

            if (!string.IsNullOrEmpty(startScene))
            {
                StartCoroutine(LoadLevel(startScene));
            }
        }

        VHGlobals.m_mainMenuCount++;

        VHMsgBase vhmsg = VHMsgBase.Get();
        vhmsg.SubscribeMessage("vrAllCall");
        vhmsg.SubscribeMessage("vrKillComponent");

        vhmsg.AddMessageEventHandler(new VHMsgBase.MessageEventHandler(VHMsg_MessageEvent));

        vhmsg.SendVHMsg("vrComponent renderer");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            VHUtils.ApplicationQuit();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            GameObject.FindObjectOfType<DebugInfo>().NextMode();
        }

        if (m_loadingLevelStatus != null)
        {
            float progress = m_loadingLevelStatus.progress * m_loadingBarMaxWidth;
            m_imageLoadingResizeRectTransform.sizeDelta = new Vector2(progress, m_imageLoadingResizeRectTransform.rect.height);
        }
    }

    void VHMsg_MessageEvent(object sender, VHMsgBase.Message message)
    {
        string [] splitargs = message.s.Split( " ".ToCharArray() );

        if (splitargs.Length > 0)
        {
            if (splitargs[0] == "vrAllCall")
            {
                VHMsgBase.Get().SendVHMsg("vrComponent renderer");
            }
            else if (splitargs[0] == "vrKillComponent")
            {
                if (splitargs.Length > 1)
                {
                    if (splitargs[1] == "renderer" || splitargs[1] == "all")
                    {
                        VHUtils.ApplicationQuit();
                    }
                }
            }
        }
    }

    public IEnumerator LoadLevel(string levelName)
    {
        // disable all other buttons that are active in the scene
        UnityEngine.UI.Button [] buttons = GameObject.FindObjectsOfType<UnityEngine.UI.Button>();
        foreach (var button in buttons)
        {
            button.interactable = false;
        }

        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "Image_Loading").SetActive(true);
        m_imageLoadingResizeRectTransform = VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "Image_LoadingResize").GetComponent<RectTransform>();
        m_loadingBarMaxWidth = m_imageLoadingResizeRectTransform.rect.width;
        m_imageLoadingResizeRectTransform.sizeDelta = new Vector2(0, m_imageLoadingResizeRectTransform.rect.height);

        m_loadingLevelStatus = VHUtils.SceneManagerLoadSceneAsync(levelName);

        yield return m_loadingLevelStatus;
    }
}
