using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class UIEvents : MonoBehaviour
{
    bool m_settingsIgnoreCallbacks = false;

    void Start()
    {
        //GameObject menuPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "MenuPrefab");
        //GameObject buttonTogglePrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "ButtonTogglePrefab");
        //GameObject textPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "TextPrefab");
        //GameObject buttonDropDownPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "ButtonDropDownPrefab");
        //GameObject buttonPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "ButtonPrefab");

        GameObject menuMain = AddMenu("DYN_PanelMainMenu", "Main Menu", 0, 0, 394, 326);
        menuMain.SetActive(true);
        AddButton(menuMain, "DYN_StartDemo",    "Start Demo",     UIMainMenuStartDemo);
        AddButton(menuMain, "DYN_LoadDevLevel", "Load Dev Level", UIMainMenuLoadDevLevel);
        AddButton(menuMain, "DYN_SelectLevel",  "Select Level",   UIMainMenuSelectLevel);
        AddButton(menuMain, "DYN_Settings",     "Settings",       UIMainMenuSettings);
        AddButton(menuMain, "DYN_Help",         "Help",           UIMainMenuHelp);
        AddButton(menuMain, "DYN_Quit",         "Quit",           UIMainMenuQuit);

        GameObject menuSelectLevel = AddMenu("DYN_PanelSelectLevel", "Select Level", 0, 0, 480, 384);
        menuSelectLevel.SetActive(false);
        AddButton(menuSelectLevel, "DYN_Campus",       "Campus",         UISelectLevelCampus);
        AddButton(menuSelectLevel, "DYN_House",        "House",          UISelectLevelHouse);
        AddButton(menuSelectLevel, "DYN_Lineup",       "Line Up",        UISelectLevelLineup);
        AddButton(menuSelectLevel, "DYN_Customizer",   "Customizer",     UISelectLevelCustomizer);
        //AddButton(menuSelectLevel, "DYN_Tacq",         "TACQ",           UISelectLevelTacq);
        AddButton(menuSelectLevel, "DYN_CampusEmpty",  "Campus Empty",   UISelectLevelCampusEmpty);
        //AddButton(menuSelectLevel, "DYN_CampusOculus", "Campus Oculus",  UISelectLevelCampusOculus);
        AddButton(menuSelectLevel, "DYN_CampusTab",    "Campus Tab",     UISelectLevelCampusTab);
        AddButton(menuSelectLevel, "DYN_Back",         "Back",           UISelectLevelBack);

        GameObject menuSettings = AddMenu("DYN_PanelSettings", "Settings", 0, 0, 590, 286);
        menuSettings.SetActive(false);
        AddDropDown(menuSettings, "DYN_AspectRatio", "Aspect Ratio", UISettingsAspectRatioOnValueChanged);
        AddDropDown(menuSettings, "DYN_Resolution", "Resolution", UISettingsResolutionOnValueChanged);
        AddDropDown(menuSettings, "DYN_Quality", "Quality", UISettingsQualityOnValueChanged);
        AddToggle(menuSettings, "DYN_Fullscreen", "Full Screen", UISettingsFullscreenToggle);
        AddButton(menuSettings, "DYN_Back", "Back", UISettingsBack);

        GameObject menuHelp = AddMenu("DYN_PanelHelp", "Help", 0, 0, 550, 210);
        menuHelp.SetActive(false);
        AddButton(menuHelp, "DYN_KeyboardShortcuts", "Keyboard Shortcuts", UIHelpKeyboardShortcuts);
        AddButton(menuHelp, "DYN_Documentation", "Documentation", UIHelpDocumentation);
        AddButton(menuHelp, "DYN_Back", "Back", UIHelpBack);

        GameObject menuKeyboard = AddMenu("DYN_PanelKeyboard", "Keyboard Shortcuts", 0, 0, 800, 717);
        menuKeyboard.SetActive(false);
        AddText(menuKeyboard, "DYN_Shortcut", "W, A, S, D", "Camera Movement");
        AddText(menuKeyboard, "DYN_Shortcut", "Q, E", "Camera up/down");
        AddText(menuKeyboard, "DYN_Shortcut", "J", "Toggle mouse look/cursor");
        AddText(menuKeyboard, "DYN_Shortcut", "M", "Toggle speech recognition mode");
        AddText(menuKeyboard, "DYN_Shortcut", "", "When on, click and hold mouse");
        AddText(menuKeyboard, "DYN_Shortcut", "", "button and talk in the mic.");
        AddText(menuKeyboard, "DYN_Shortcut", "", "Release when done talking.");
        AddText(menuKeyboard, "DYN_Shortcut", "I", "Toggle character subtitles");
        AddText(menuKeyboard, "DYN_Shortcut", "O", "Toggle user's recognized text");
        AddText(menuKeyboard, "DYN_Shortcut", "L", "Toggle text input box");
        AddText(menuKeyboard, "DYN_Shortcut", "P", "Toggle entire GUI");
        AddText(menuKeyboard, "DYN_Shortcut", "X", "Reset camera");
        AddText(menuKeyboard, "DYN_Shortcut", "Z", "Show debug statistics");
        AddText(menuKeyboard, "DYN_Shortcut", "C", "Toggle debug menu");
        AddText(menuKeyboard, "DYN_Shortcut", "Alt+Enter", "Toggle windowed/fullscreen");
        AddButton(menuKeyboard, "DYN_Back", "Back", UIKeyboardShortcutsBack);
    }

    public GameObject AddMenu(string name, string displayText, float x, float y, float width, float height)
    {
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_UIPrefab");
        GameObject menuPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "MenuPrefab");

        GameObject menu = GameObject.Instantiate<GameObject>(menuPrefab);
        menu.name = name;
        menu.transform.SetParent(canvaseUIPrefab.transform, false);

        RectTransform menuTransform = VHUtils.FindChildRecursive(menu, "Menu_Size").GetComponent<RectTransform>();
        float newX = x == float.MaxValue ? menuTransform.localPosition.x : x;
        float newY = y == float.MaxValue ? menuTransform.localPosition.y : y;
        float newWidth = width == float.MaxValue ? menuTransform.sizeDelta.x : width;
        float newHeight = height == float.MaxValue ? menuTransform.sizeDelta.y : height;
        menuTransform.localPosition = new Vector2(newX, newY);
        menuTransform.sizeDelta = new Vector2(newWidth, newHeight);

        VHUtils.FindChildOfType<UnityEngine.UI.Text>(menu).text = displayText;

        return menu;
    }

    public GameObject AddMenu(string name, string displayText)
    {
        return AddMenu(name, displayText, float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
    }

    public GameObject AddButton(GameObject menu, string name, string displayText, UnityEngine.Events.UnityAction onClick)
    {
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_UIPrefab");
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
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_UIPrefab");
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
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_UIPrefab");
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
        GameObject canvaseUIPrefab = GameObject.Find("Canvas_UIPrefab");
        GameObject buttonPrefab = VHUtils.FindChildRecursive(canvaseUIPrefab, "TextPrefab");

        GameObject menuContent = VHUtils.FindChildRecursive(menu, "Content");

        GameObject button = GameObject.Instantiate<GameObject>(buttonPrefab);
        button.name = name;
        button.transform.SetParent(menuContent.transform, false);
        VHUtils.FindChild(button, "Text_Left").GetComponent<UnityEngine.UI.Text>().text = displayTextLeft;
        VHUtils.FindChild(button, "Text_Right").GetComponent<UnityEngine.UI.Text>().text = displayTextRight;

        return button;
    }


    public void UIMainMenuStartDemo()
    {
        VHGlobals.m_showIntro = true;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("Campus"));
    }

    public void UIMainMenuLoadDevLevel()
    {
        VHGlobals.m_showIntro = false;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("Campus"));
    }

    public void UIMainMenuSelectLevel()
    {
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelMainMenu").SetActive(false);
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelSelectLevel").SetActive(true);
    }

    public void UIMainMenuSettings()
    {
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelMainMenu").SetActive(false);
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelSettings").SetActive(true);

        StartCoroutine(SettingsRefreshView());
    }

    public void UIMainMenuHelp()
    {
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelMainMenu").SetActive(false);
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelHelp").SetActive(true);
    }

    public void UIMainMenuQuit()
    {
        VHUtils.ApplicationQuit();
    }


    public void UISelectLevelCampus()
    {
        VHGlobals.m_showIntro = false;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("Campus"));
    }

    public void UISelectLevelHouse()
    {
        VHGlobals.m_showIntro = false;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("House"));
    }

    public void UISelectLevelLineup()
    {
        VHGlobals.m_showIntro = false;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("Lineup"));
    }

    public void UISelectLevelCustomizer()
    {
        VHGlobals.m_showIntro = false;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("Customizer"));
    }

    public void UISelectLevelTacq()
    {
        VHGlobals.m_showIntro = false;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("CampusTacQ"));
    }

    public void UISelectLevelCampusEmpty()
    {
        VHGlobals.m_showIntro = false;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("CampusEmpty"));
    }

    public void UISelectLevelCampusOculus()
    {
        VHGlobals.m_showIntro = false;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("OculusRiftTest"));
    }

    public void UISelectLevelCampusTab()
    {
        VHGlobals.m_showIntro = false;
        StartCoroutine(GameObject.FindObjectOfType<MainMenu>().LoadLevel("CampusTAB"));
    }

    public void UISelectLevelBack()
    {
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelSelectLevel").SetActive(false);
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelMainMenu").SetActive(true);
    }


    public void UISettingsAspectRatioOnValueChanged(int value)
    {
        if (m_settingsIgnoreCallbacks)
            return;

        GameObject aspectRatio = VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_AspectRatio");
        UnityEngine.UI.Dropdown aspectRatioDropDown = VHUtils.FindChildRecursive(aspectRatio, "Dropdown").GetComponent<UnityEngine.UI.Dropdown>();

        string selectedText = aspectRatioDropDown.options[aspectRatioDropDown.value].text;

        for (int i = Screen.resolutions.Length - 1; i >= 0; i--)
        {
            var resolution = Screen.resolutions[i];
            if (VHUtils.GetCommonAspectText((float)resolution.width / resolution.height) == selectedText)
            {
                //Debug.Log(string.Format("Found resolution: {0}x{1}", resolution.width, resolution.height));

                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
                break;
            }
        }

        StartCoroutine(SettingsRefreshView());
    }

    public void UISettingsResolutionOnValueChanged(int value)
    {
        if (m_settingsIgnoreCallbacks)
            return;

        GameObject resolutionButton = VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_Resolution");
        UnityEngine.UI.Dropdown resolutionDropDown = VHUtils.FindChildRecursive(resolutionButton, "Dropdown").GetComponent<UnityEngine.UI.Dropdown>();

        string selectedText = resolutionDropDown.options[resolutionDropDown.value].text;

        //Debug.Log(resolutionDropDown.value);
        //Debug.Log(selectedText);

        foreach (var resolution in Screen.resolutions)
        {
            string resolutionText = string.Format("{0}x{1}", resolution.width, resolution.height);
            if (resolutionText == selectedText)
            {
                //Debug.Log(string.Format("Found resolution: {0}x{1}", resolution.width, resolution.height));

                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
                break;
            }
        }

        StartCoroutine(SettingsRefreshView());
    }

    public void UISettingsQualityOnValueChanged(int value)
    {
        if (m_settingsIgnoreCallbacks)
            return;

        GameObject qualityButton = VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_Quality");
        UnityEngine.UI.Dropdown qualityDropDown = VHUtils.FindChildRecursive(qualityButton, "Dropdown").GetComponent<UnityEngine.UI.Dropdown>();

        string selectedText = qualityDropDown.options[qualityDropDown.value].text;

        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            var quality = QualitySettings.names[i];
            if (quality == selectedText)
            {
                QualitySettings.SetQualityLevel(i);
            }
        }

        StartCoroutine(SettingsRefreshView());
    }

    public void UISettingsFullscreenToggle(bool value)
    {
        if (m_settingsIgnoreCallbacks)
            return;

        GameObject panelSettings = VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_Fullscreen");
        UnityEngine.UI.Toggle fullscreenToggle = VHUtils.FindChildRecursive(panelSettings, "Toggle").GetComponent<UnityEngine.UI.Toggle>();

        Debug.Log(string.Format("Attempting {0}x{1} {2}", Screen.width, Screen.height, fullscreenToggle.isOn ? "fullscreen" : "windowed"));

        Screen.SetResolution(Screen.width, Screen.height, fullscreenToggle.isOn);

        StartCoroutine(SettingsRefreshView());
    }

    public void UISettingsBack()
    {
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelSettings").SetActive(false);
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelMainMenu").SetActive(true);
    }

    public IEnumerator SettingsRefreshView()
    {
        m_settingsIgnoreCallbacks = true;

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        // aspect
        GameObject aspectRatioButton = VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_AspectRatio");
        UnityEngine.UI.Dropdown aspectRatioDropDown = VHUtils.FindChildRecursive(aspectRatioButton, "Dropdown").GetComponent<UnityEngine.UI.Dropdown>();
        aspectRatioDropDown.ClearOptions();
        aspectRatioDropDown.AddOptions(new List<string>() { "", "4:3", "3:2", "16:9", "16:10" } );

        string aspect = VHUtils.GetCommonAspectText((float)Screen.width / Screen.height);
        for (int i = 0; i < aspectRatioDropDown.options.Count; i++)
        {
            var option = aspectRatioDropDown.options[i];

            if (option.text == aspect)
            {
                aspectRatioDropDown.value = i;
                break;
            }
        }

        // resolution
        GameObject resolutionButton = VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_Resolution");
        UnityEngine.UI.Dropdown resolutionDropDown = VHUtils.FindChildRecursive(resolutionButton, "Dropdown").GetComponent<UnityEngine.UI.Dropdown>();
        resolutionDropDown.ClearOptions();

        resolutionDropDown.AddOptions(new List<string>() { "" } );
        foreach (var resolution in Screen.resolutions)
        {
            //Debug.Log(string.Format("Resolutions - Total: {0} - Option: {1}x{2}", Screen.resolutions.Length, resolution.width, resolution.height));

            resolutionDropDown.AddOptions(new List<string>() { string.Format("{0}x{1}", resolution.width, resolution.height) } );
        }

        string currentResolution = string.Format("{0}x{1}", Screen.width, Screen.height);
        for (int i = 0; i < resolutionDropDown.options.Count; i++)
        {
            var option = resolutionDropDown.options[i];

            if (option.text == currentResolution)
            {
                resolutionDropDown.value = i;
                break;
            }
        }

        // quality
        GameObject qualityButton = VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_Quality");
        UnityEngine.UI.Dropdown qualityDropDown = VHUtils.FindChildRecursive(qualityButton, "Dropdown").GetComponent<UnityEngine.UI.Dropdown>();
        qualityDropDown.ClearOptions();
        foreach (var quality in QualitySettings.names)
        {
            qualityDropDown.AddOptions(new List<string>() { quality } );
        }

        string currentQuality = QualitySettings.names[QualitySettings.GetQualityLevel()];
        for (int i = 0; i < qualityDropDown.options.Count; i++)
        {
            var option = qualityDropDown.options[i];

            if (option.text == currentQuality)
            {
                qualityDropDown.value = i;
                break;
            }
        }

        // fullscreen toggle
        GameObject panelSettings = VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_Fullscreen");
        UnityEngine.UI.Toggle fullscreenToggle = VHUtils.FindChildRecursive(panelSettings, "Toggle").GetComponent<UnityEngine.UI.Toggle>();
        fullscreenToggle.isOn = Screen.fullScreen;

        m_settingsIgnoreCallbacks = false;
    }


    public void UIHelpTips()
    {
    }

    public void UIHelpKeyboardShortcuts()
    {
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelHelp").SetActive(false);
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelKeyboard").SetActive(true);
    }

    public void UIHelpDocumentation()
    {
        Application.OpenURL("https://vhtoolkit.ict.usc.edu/documentation/");
    }

    public void UIHelpBack()
    {
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelHelp").SetActive(false);
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelMainMenu").SetActive(true);
    }


    public void UIKeyboardShortcutsBack()
    {
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelKeyboard").SetActive(false);
        VHUtils.FindChildRecursive(GameObject.Find("Canvas_UIPrefab"), "DYN_PanelHelp").SetActive(true);
    }
}
