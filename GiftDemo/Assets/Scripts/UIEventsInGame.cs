using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class UIEventsInGame : MonoBehaviour
{
    float m_prevTimeScale = 1;
    GameObject m_menuPause;

    void Start()
    {
        m_menuPause = AddMenu("DYN_PanelPause", "Pause Menu");
        VHUtils.FindChildRecursive(m_menuPause, "Image_OverlayBox").SetActive(true);
        m_menuPause.SetActive(false);
        AddButton(m_menuPause, "DYN_MainMenu", "Main Menu", UIMainMenu);
        AddButton(m_menuPause, "DYN_Exit", "Exit", UIExit);
        AddButton(m_menuPause, "DYN_Return", "Resume", UIResume);

        AddMicrophone("DYN_Microphone");
        UIMicrophoneSetDisabled();
    }

    public GameObject AddMenu(string name, string displayText)
    {
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject menuPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "MenuPrefab");

        GameObject menu = GameObject.Instantiate<GameObject>(menuPrefab);
        menu.name = name;
        menu.transform.SetParent(canvaseUIPrefab.transform, false);
        VHUtils.FindChildOfType<UnityEngine.UI.Text>(menu).text = displayText;

        return menu;
    }

    public GameObject AddButton(GameObject menu, string name, string displayText, UnityEngine.Events.UnityAction onClick)
    {
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject buttonPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "ButtonPrefab");

        GameObject menuContent = VHUtils.FindChildRecursive(menu, "Content");

        GameObject button = GameObject.Instantiate<GameObject>(buttonPrefab);
        button.name = name;
        button.transform.SetParent(menuContent.transform, false);
        button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(onClick);
        VHUtils.FindChildOfType<UnityEngine.UI.Text>(button).text = displayText;

        return button;
    }

    public GameObject AddDropDown(GameObject menu, string name, string displayText, UnityEngine.Events.UnityAction<int> onValueChanged)
    {
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject buttonPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "ButtonDropdownPrefab");

        GameObject menuContent = VHUtils.FindChildRecursive(menu, "Content");

        GameObject button = GameObject.Instantiate<GameObject>(buttonPrefab);
        button.name = name;
        button.transform.SetParent(menuContent.transform, false);
        VHUtils.FindChildOfType<UnityEngine.UI.Dropdown>(button).onValueChanged.AddListener(onValueChanged);
        VHUtils.FindChildOfType<UnityEngine.UI.Text>(button).text = displayText;

        return button;
    }

    public GameObject AddToggle(GameObject menu, string name, string displayText, UnityEngine.Events.UnityAction<bool> onValueChanged)
    {
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject buttonPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "ButtonTogglePrefab");

        GameObject menuContent = VHUtils.FindChildRecursive(menu, "Content");

        GameObject button = GameObject.Instantiate<GameObject>(buttonPrefab);
        button.name = name;
        button.transform.SetParent(menuContent.transform, false);
        VHUtils.FindChildOfType<UnityEngine.UI.Toggle>(button).onValueChanged.AddListener(onValueChanged);
        VHUtils.FindChildOfType<UnityEngine.UI.Text>(button).text = displayText;

        return button;
    }

    public GameObject AddText(GameObject menu, string name, string displayTextLeft, string displayTextRight)
    {
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject buttonPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "TextPrefab");

        GameObject menuContent = VHUtils.FindChildRecursive(menu, "Content");

        GameObject button = GameObject.Instantiate<GameObject>(buttonPrefab);
        button.name = name;
        button.transform.SetParent(menuContent.transform, false);
        VHUtils.FindChild(button, "Text_Left").GetComponent<UnityEngine.UI.Text>().text = displayTextLeft;
        VHUtils.FindChild(button, "Text_Right").GetComponent<UnityEngine.UI.Text>().text = displayTextRight;

        return button;
    }

    public GameObject AddMicrophone(string name)
    {
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject buttonPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "ButtonMicPrefab");
        GameObject panel = VHUtils.FindChild(canvaseUIPrefab, "Panel");

        GameObject button = GameObject.Instantiate<GameObject>(buttonPrefab);
        button.name = name;
        button.transform.SetParent(panel.transform, false);

        return button;
    }


    public void UITogglePauseMenu()
    {
        if (m_menuPause.activeSelf)
        {
            // lock the screen cursor if they are looking around or using their mic
            //Cursor.lockState = (GameObject.FindObjectOfType<Main>().InAcquireSpeechMode || GameObject.FindObjectOfType<FreeMouseLook>().CameraRotationOn) ? CursorLockMode.Locked : CursorLockMode.None;
            //Cursor.visible = Cursor.lockState != CursorLockMode.Locked;

            Time.timeScale = m_prevTimeScale;

            m_menuPause.SetActive(false);
        }
        else
        {
            //Cursor.lockState = CursorLockMode.None;

            m_prevTimeScale = Time.timeScale;
            //Time.timeScale = 0;

            m_menuPause.SetActive(true);
        }
    }

    public bool UIPauseMenuIsOn()
    {
        return m_menuPause.activeSelf;
    }

    public void UIMicrophoneSetDisabled()
    {
        GameObject canvaseOnScreenDisplayPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject microphone = VHUtils.FindChildRecursive(canvaseOnScreenDisplayPrefab, "DYN_Microphone");

        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Disable").SetActive(false);
        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_On").SetActive(false);
        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Record").SetActive(false);
        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Disable").SetActive(true);
    }

    public void UIMicrophoneSetOn()
    {
        GameObject canvaseOnScreenDisplayPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject microphone = VHUtils.FindChildRecursive(canvaseOnScreenDisplayPrefab, "DYN_Microphone");

        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Disable").SetActive(false);
        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_On").SetActive(false);
        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Record").SetActive(false);
        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_On").SetActive(true);
    }

    public void UIMicrophoneSetInUse()
    {
        GameObject canvaseOnScreenDisplayPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject microphone = VHUtils.FindChildRecursive(canvaseOnScreenDisplayPrefab, "DYN_Microphone");

        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Disable").SetActive(false);
        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_On").SetActive(false);
        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Record").SetActive(false);
        VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Record").SetActive(true);
    }

    public bool UIMicrophoneIsDisabled()
    {
        GameObject canvaseOnScreenDisplayPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject microphone = VHUtils.FindChildRecursive(canvaseOnScreenDisplayPrefab, "DYN_Microphone");

        return VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Disable").activeSelf;
    }

    public bool UIMicrophoneIsOn()
    {
        GameObject canvaseOnScreenDisplayPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject microphone = VHUtils.FindChildRecursive(canvaseOnScreenDisplayPrefab, "DYN_Microphone");

        return VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_On").activeSelf;
    }

    public bool UIMicrophoneIsInUse()
    {
        GameObject canvaseOnScreenDisplayPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        GameObject microphone = VHUtils.FindChildRecursive(canvaseOnScreenDisplayPrefab, "DYN_Microphone");

        return VHUtils.FindChildRecursive(microphone, "Image_VHT_IconMic_Record").activeSelf;
    }


    public void UIMainMenu()
    {
        VHUtils.SceneManagerLoadScene("MainMenu");
    }

    public void UIResume()
    {
        UITogglePauseMenu();
    }

    public void UIExit()
    {
        VHUtils.ApplicationQuit();
    }
}
