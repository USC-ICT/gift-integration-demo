using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System;


public class VHGlobals
{
    public static int m_mainMenuCount = 0;   // process these variables only the first time we enter the MainMenu
    public static bool m_showIntro = false;
    public static string m_startScene;
    public static bool m_showDebugInfo = false;
    public static bool m_showDebugConsole = false;
    public static bool m_launchedFromLauncher = false;
}
