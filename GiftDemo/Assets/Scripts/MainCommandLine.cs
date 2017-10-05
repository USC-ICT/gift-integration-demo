using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

public class MainCommandLine : MonoBehaviour
{
    void Awake()
    {
        Application.runInBackground = true;
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        // -intro
        // -scene Campus
        // -fullscreen
        // -resolution 640x480
        // -showdebuginfo
        // -showdebugconsole
        // -fromlauncher


        if (VHUtils.HasCommandLineArgument("intro"))
        {
            VHGlobals.m_showIntro = true;
        }

        if (VHUtils.HasCommandLineArgument("scene"))
        {
            string scene = VHUtils.GetCommandLineArgumentValue("scene");
            VHGlobals.m_startScene = scene;
        }

        // defaults since we have to call SetResolution() no matter what.  Unity saves the resolution of the last time this process was run, so we have to override that behavior.
        bool fullscreen = false;
        int screenWidth = 1024;
        int screenHeight = 768;

        if (VHUtils.HasCommandLineArgument("fullscreen"))
        {
            fullscreen = true;
        }

        if (VHUtils.HasCommandLineArgument("resolution"))
        {
            string resolution = VHUtils.GetCommandLineArgumentValue("resolution");
            string[] widthHeightStrings = resolution.Split('x');
            if (widthHeightStrings.Length == 2)
            {
                int width;
                int height;
                if (int.TryParse(widthHeightStrings[0], out width))  screenWidth = width;
                if (int.TryParse(widthHeightStrings[1], out height)) screenHeight = height;
            }
        }

        Screen.SetResolution(screenWidth, screenHeight, fullscreen);

        if (VHUtils.HasCommandLineArgument("showdebuginfo"))
        {
            VHGlobals.m_showDebugInfo = true;
        }

        if (VHUtils.HasCommandLineArgument("showdebugconsole"))
        {
            VHGlobals.m_showDebugConsole = true;
        }

        if (VHUtils.HasCommandLineArgument("fromlauncher"))
        {
            VHGlobals.m_launchedFromLauncher = true;
        }


        VHUtils.SceneManagerLoadScene("Splash");
    }

    void Update()
    {
    }

    void OnGUI()
    {
    }
}
