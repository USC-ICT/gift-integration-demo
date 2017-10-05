using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;


public class CreatePackages
{
    [MenuItem("VH/Build/VHToolkit Android App")]
    static void MenuCreateAndroidApp()
    {
        CreatePackages.CreateAndroidApp();
    }

    [MenuItem("VH/Build/VHToolkit OSX App")]
    static void MenuCreateOSXApp()
    {
        CreatePackages.CreateOSXApp();
    }


    public static void CreateAndroidApp()
    {
        BuildPlayer.PerformBuild(BuildTarget.Android, BuildTargetGroup.Android, "BuildSettingsMobile.xml");
    }

    public static void CreateOSXApp()
    {
        var original = PlayerSettings.displayResolutionDialog;
        PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Enabled;

        BuildPlayer.PerformBuild(BuildTarget.StandaloneOSXIntel, BuildTargetGroup.Standalone, "BuildSettingsOSX.xml");

        PlayerSettings.displayResolutionDialog = original;
    }
}
