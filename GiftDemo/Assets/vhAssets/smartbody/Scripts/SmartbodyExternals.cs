using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public class SmartbodyExternals
{
    // this layout has to match SBM_CharacterFrameDataMarshalFriendly struct in Smartbody.dll
    [StructLayout(LayoutKind.Sequential)]
    public struct SmartbodyCharacterFrameData
    {
        public IntPtr m_name;
        public float x;
        public float y;
        public float z;
        public float rw;
        public float rx;
        public float ry;
        public float rz;
        public int m_numJoints;
        public IntPtr jname;
        public IntPtr jx;
        public IntPtr jy;
        public IntPtr jz;
        public IntPtr jrw;
        public IntPtr jrx;
        public IntPtr jry;
        public IntPtr jrz;
    }


#if UNITY_IPHONE
    public const string DLLIMPORT_NAME = "__Internal";
#else
    public const string DLLIMPORT_NAME = "vhwrapper";
#endif

    static Dictionary<string, IntPtr> m_stringCache = new Dictionary<string, IntPtr>();  // cache Marshal string conversions

    static bool m_fileLogging = false;
    static string m_fileLoggingName = string.Format(@".\unity-sb-python-{0:yyyy-MM-dd_hh-mm-ss-tt}.log", DateTime.Now);


    class LibraryData
    {
        public string configuration; // "both", "release", "debug"
        public string architecture;  // "both", "x86", "x64"
        public string library;

        public LibraryData(string configuration, string architecture, string library) { this.configuration = configuration; this.architecture = architecture; this.library = library; }
    }

    static List<LibraryData> m_libraries = new List<LibraryData>()
    {
        // order does matter here.  Dependencies must be loaded first.  Libraries are freed in reverse order
        new LibraryData("both",    "both",  "vcruntime140.dll"),
        new LibraryData("both",    "both",  "msvcp140.dll"),
        new LibraryData("both",    "x86",   "dbghelp.dll"),
        new LibraryData("both",    "both",  "pthreadVSE2.dll"),
        new LibraryData("both",    "both",  "glew32.dll"),
        new LibraryData("both",    "both",  "libapr-1.dll"),
        new LibraryData("both",    "both",  "libapriconv-1.dll"),
        new LibraryData("both",    "both",  "libaprutil-1.dll"),
        new LibraryData("both",    "both",  "OpenAL32.dll"),
        new LibraryData("both",    "both",  "wrap_oal.dll"),
        new LibraryData("both",    "both",  "alut.dll"),
        new LibraryData("both",    "both",  "libsndfile-1.dll"),
        new LibraryData("both",    "both",  "python27.dll"),
        new LibraryData("release", "both",  "xerces-c_3_1.dll"),
        new LibraryData("debug",   "both",  "xerces-c_3_1D.dll"),
        new LibraryData("release", "both",  "boost_system-vc140-mt-1_59.dll"),
        new LibraryData("debug",   "both",  "boost_system-vc140-mt-gd-1_59.dll"),
        new LibraryData("release", "both",  "boost_filesystem-vc140-mt-1_59.dll"),
        new LibraryData("debug",   "both",  "boost_filesystem-vc140-mt-gd-1_59.dll"),
        new LibraryData("release", "both",  "boost_regex-vc140-mt-1_59.dll"),
        new LibraryData("debug",   "both",  "boost_regex-vc140-mt-gd-1_59.dll"),
        new LibraryData("release", "both",  "boost_serialization-vc140-mt-1_59.dll"),
        new LibraryData("debug",   "both",  "boost_serialization-vc140-mt-gd-1_59.dll"),
        new LibraryData("release", "both",  "boost_python-vc140-mt-1_59.dll"),
        new LibraryData("debug",   "both",  "boost_python-vc140-mt-gd-1_59.dll"),
        new LibraryData("release", "both",  "activemq-cpp.dll"),
        new LibraryData("debug",   "both",  "activemq-cppd.dll"),
        new LibraryData("release", "both",  "steerlib.dll"),
        new LibraryData("debug",   "both",  "steerlibd.dll"),
        new LibraryData("release", "both",  "pprAI.dll"),
        new LibraryData("debug",   "both",  "pprAId.dll"),
        new LibraryData("release", "both",  "SmartBody.dll"),
        new LibraryData("debug",   "both",  "SmartBody_d.dll"),
        new LibraryData("both",    "both",  "vhwrapper.dll"),
    };

    static List<KeyValuePair<string, IntPtr>> m_nativeDlls = new List<KeyValuePair<string, IntPtr>>();


    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool FreeLibrary(IntPtr hModule);


#if true
    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern IntPtr WRAPPER_SBM_CreateSBM(bool releaseMode);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_Init(IntPtr sbmID, IntPtr pythonLibPath, bool logToFile);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_Shutdown(IntPtr sbmID);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_Update(IntPtr sbmID, double timeInSeconds);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_UpdateUsingDelta(IntPtr sbmID, double deltaTimeInSeconds);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_ProcessVHMsgs(IntPtr sbmID, IntPtr op, [MarshalAs(UnmanagedType.LPStr)]string args);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_InitCharacter(IntPtr sbmID, IntPtr name, ref SmartbodyCharacterFrameData character);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_GetCharacter(IntPtr sbmID, IntPtr name, ref SmartbodyCharacterFrameData character);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_ReleaseCharacter(ref SmartbodyCharacterFrameData character);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_IsCharacterCreated(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder objectClass, int maxObjectClassLen);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_IsCharacterDeleted(IntPtr sbmID, StringBuilder name, int maxNameLen);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_IsCharacterChanged(IntPtr sbmID, StringBuilder name, int maxNameLen);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_IsVisemeSet(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder visemeName, int maxVisemeNameLen, ref float weight, ref float blendTime);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_IsChannelSet(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder channelName, int maxChannelNameLen, ref float value);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_IsLogMessageWaiting(IntPtr sbmID, StringBuilder logMessage, int maxLogMessageLen, ref int logMessageType);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_IsBmlRequestWaiting(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder requestId, int maxRequestIdLength, StringBuilder bmlName, int maxBmlNameLength);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_SendBmlReply(IntPtr sbmID, string characterName, string requestId, string utteranceId, string rawBml);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_PythonCommandVoid(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_PythonCommandBool(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern int WRAPPER_SBM_PythonCommandInt(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern float WRAPPER_SBM_PythonCommandFloat(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_PythonCommandString(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command, StringBuilder output, int capacity);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_SBAssetManager_LoadSkeleton(IntPtr sbmID, IntPtr data, int sizeBytes, IntPtr skeletonName);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern bool WRAPPER_SBM_SBAssetManager_LoadMotion(IntPtr sbmID, IntPtr data, int sizeBytes, IntPtr motionName);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBDebuggerServer_SetID(IntPtr sbmID, IntPtr id);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBDebuggerServer_SetCameraValues(IntPtr sbmID, double x, double y, double z, double rx, double ry, double rz, double rw, double fov, double aspect, double zNear, double zFar);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBDebuggerServer_SetRendererIsRightHanded(IntPtr sbmID, bool enabled);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBMotion_AddChannel(IntPtr sbmID, IntPtr motionName, IntPtr channelName, IntPtr channelType);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBMotion_AddChannels(IntPtr sbmID, IntPtr motionName, IntPtr [] channelNames, IntPtr [] channelTypes, int count);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBMotion_AddFrame(IntPtr sbmID, IntPtr motionName, float frameTime, IntPtr frameData, int numFrameData);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBMotion_SetSyncPoint(IntPtr sbmID, IntPtr motionName, IntPtr syncTag, double time);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBJointMap_GetMapTarget(IntPtr sbmID, IntPtr jointMap, IntPtr jointName, StringBuilder mappedJointName, int maxMappedJointName);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBDiphoneManager_CreateDiphone(IntPtr sbmID, IntPtr fromPhoneme, IntPtr toPhoneme, IntPtr name);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBDiphone_AddKey(IntPtr sbmID, IntPtr fromPhoneme, IntPtr toPhoneme, IntPtr name, IntPtr viseme, float time, float weight);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBVHMsgManager_SetServer(IntPtr sbmID, IntPtr server);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBVHMsgManager_SetScope(IntPtr sbmID, IntPtr scope);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBVHMsgManager_SetPort(IntPtr sbmID, IntPtr port);

    [DllImport(DLLIMPORT_NAME, SetLastError = true)]
    static extern void WRAPPER_SBM_SBVHMsgManager_SetEnable(IntPtr sbmID, bool enable);
#else
    static IntPtr WRAPPER_SBM_CreateSBM(bool releaseMode) { return new IntPtr(0); }
    static bool WRAPPER_SBM_Init(IntPtr sbmID, IntPtr pythonLibPath, bool logToFile) { return true; }
    static bool WRAPPER_SBM_Shutdown(IntPtr sbmID) { return true; }
    static bool WRAPPER_SBM_Update(IntPtr sbmID, double timeInSeconds) { return true; }
    static bool WRAPPER_SBM_UpdateUsingDelta(IntPtr sbmID, double deltaTimeInSeconds) { return true; }
    static bool WRAPPER_SBM_ProcessVHMsgs(IntPtr sbmID, IntPtr op, [MarshalAs(UnmanagedType.LPStr)]string args) { return true; }
    static bool WRAPPER_SBM_InitCharacter(IntPtr sbmID, IntPtr name, ref SmartbodyCharacterFrameData character) { return true; }
    static bool WRAPPER_SBM_GetCharacter(IntPtr sbmID, IntPtr name, ref SmartbodyCharacterFrameData character) { return true; }
    static bool WRAPPER_SBM_ReleaseCharacter(ref SmartbodyCharacterFrameData character) { return true; }
    static bool WRAPPER_SBM_IsCharacterCreated(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder objectClass, int maxObjectClassLen) { return true; }
    static bool WRAPPER_SBM_IsCharacterDeleted(IntPtr sbmID, StringBuilder name, int maxNameLen) { return true; }
    static bool WRAPPER_SBM_IsCharacterChanged(IntPtr sbmID, StringBuilder name, int maxNameLen) { return true; }
    static bool WRAPPER_SBM_IsVisemeSet(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder visemeName, int maxVisemeNameLen, ref float weight, ref float blendTime) { return true; }
    static bool WRAPPER_SBM_IsChannelSet(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder channelName, int maxChannelNameLen, ref float value) { return true; }
    static bool WRAPPER_SBM_IsLogMessageWaiting(IntPtr sbmID, StringBuilder logMessage, int maxLogMessageLen, ref int logMessageType) { return true; }
    static bool WRAPPER_SBM_IsBmlRequestWaiting(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder requestId, int maxRequestIdLength, StringBuilder bmlName, int maxBmlNameLength) { return true; }
    static bool WRAPPER_SBM_SendBmlReply(IntPtr sbmID, string characterName, string requestId, string utteranceId, string rawBml) { return true; }
    static void WRAPPER_SBM_PythonCommandVoid(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command) { }
    static bool WRAPPER_SBM_PythonCommandBool(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command) { return true; }
    static int WRAPPER_SBM_PythonCommandInt(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command) { return 0; }
    static float WRAPPER_SBM_PythonCommandFloat(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command) { return 0; }
    static void WRAPPER_SBM_PythonCommandString(IntPtr sbmID, [MarshalAs(UnmanagedType.LPStr)]string command, StringBuilder output, int capacity) { }
    static bool WRAPPER_SBM_SBAssetManager_LoadSkeleton(IntPtr sbmID, IntPtr data, int sizeBytes, IntPtr skeletonName) { return true; }
    static bool WRAPPER_SBM_SBAssetManager_LoadMotion(IntPtr sbmID, IntPtr data, int sizeBytes, IntPtr motionName) { return true; }
    static void WRAPPER_SBM_SBDebuggerServer_SetID(IntPtr sbmID, IntPtr id) { }
    static void WRAPPER_SBM_SBDebuggerServer_SetCameraValues(IntPtr sbmID, double x, double y, double z, double rx, double ry, double rz, double rw, double fov, double aspect, double zNear, double zFar) { }
    static void WRAPPER_SBM_SBDebuggerServer_SetRendererIsRightHanded(IntPtr sbmID, bool enabled) { }
    static void WRAPPER_SBM_SBMotion_AddChannel(IntPtr sbmID, IntPtr motionName, IntPtr channelName, IntPtr channelType) { }
    static void WRAPPER_SBM_SBMotion_AddChannels(IntPtr sbmID, IntPtr motionName, IntPtr [] channelNames, IntPtr [] channelTypes, int count) { }
    static void WRAPPER_SBM_SBMotion_AddFrame(IntPtr sbmID, IntPtr motionName, float frameTime, IntPtr frameData, int numFrameData) { }
    static void WRAPPER_SBM_SBMotion_SetSyncPoint(IntPtr sbmID, IntPtr motionName, IntPtr syncTag, double time) { }
    static void WRAPPER_SBM_SBJointMap_GetMapTarget(IntPtr sbmID, IntPtr jointMap, IntPtr jointName, StringBuilder mappedJointName, int maxMappedJointName) { }
    static void WRAPPER_SBM_SBDiphoneManager_CreateDiphone(IntPtr sbmID, IntPtr fromPhoneme, IntPtr toPhoneme, IntPtr name) { }
    static void WRAPPER_SBM_SBDiphone_AddKey(IntPtr sbmID, IntPtr fromPhoneme, IntPtr toPhoneme, IntPtr name, IntPtr viseme, float time, float weight) { }
    static void WRAPPER_SBM_SBVHMsgManager_SetServer(IntPtr sbmID, IntPtr server) { }
    static void WRAPPER_SBM_SBVHMsgManager_SetScope(IntPtr sbmID, IntPtr scope) { }
    static void WRAPPER_SBM_SBVHMsgManager_SetPort(IntPtr sbmID, IntPtr port) { }
    static void WRAPPER_SBM_SBVHMsgManager_SetEnable(IntPtr sbmID, bool enable) { }
#endif


    public static void LoadLibraries(bool releaseMode)
    {
        if (!VHUtils.IsUnity5OrGreater())
            return;

        if (!VHUtils.IsWindows())
            return;

        if (m_nativeDlls.Count > 0)
            return;  // we've already called LoadLibraries()

        for (int i = 0; i < m_libraries.Count; i++)
        {
            var libraryEntry = m_libraries[i];
            string library = libraryEntry.library;
            string libraryConfig = libraryEntry.configuration;
            string libraryArchitecture = libraryEntry.architecture;
            bool loadLibrary = false;

            if (VHUtils.Is64Bit() && (libraryArchitecture == "both" || libraryArchitecture == "x64"))
                loadLibrary = true;
            if (!VHUtils.Is64Bit() && (libraryArchitecture == "both" || libraryArchitecture == "x86"))
                loadLibrary = true;

            if (!loadLibrary)
                continue;

            loadLibrary = false;

            if (releaseMode && (libraryConfig == "both" || libraryConfig == "release"))
                loadLibrary = true;
            if (!releaseMode && (libraryConfig == "both" || libraryConfig == "debug"))
                loadLibrary = true;

            if (loadLibrary)
            {
                string path;
                if (VHUtils.Is64Bit() && VHUtils.IsEditor())
                    path = Path.GetFullPath(Application.dataPath + "/vhAssets/smartbody/Plugins/x86_64/" + library);
                else if (VHUtils.IsEditor())
                    path = Path.GetFullPath(Application.dataPath + "/vhAssets/smartbody/Plugins/" + library);
                else
                    path = Path.GetFullPath(Application.dataPath + "/Plugins/" + library);

                IntPtr ptr = LoadLibraryInternal(path);

                m_nativeDlls.Add(new KeyValuePair<string,IntPtr>(path, ptr));
            }
        }
    }

    public static void FreeLibraries()
    {
        if (!VHUtils.IsUnity5OrGreater())
            return;

        if (!VHUtils.IsWindows())
            return;

        // free in reverse order
        for (int i = m_nativeDlls.Count - 1; i >= 0; i--)
        {
            KeyValuePair<string, IntPtr> entry = m_nativeDlls[i];

            FreeLibrary(entry.Value);

            //Debug.Log(string.Format("FreeLibrary({0} - {1}) - {2}", entry.Key, entry.Value, ret));
        }

        m_nativeDlls.Clear();
    }

    public static IntPtr CreateSBM(bool releaseMode)
    {
        ClearStringCache();

        return WRAPPER_SBM_CreateSBM(releaseMode);
    }

    public static bool Init(IntPtr sbmID, string pythonLibPath, bool logToFile)
    {
        return WRAPPER_SBM_Init(sbmID, GetStringIntPtr(pythonLibPath), logToFile);
    }

    public static bool Shutdown(IntPtr sbmID)
    {
        bool ret = WRAPPER_SBM_Shutdown(sbmID);

        ClearStringCache();

        return ret;
    }

    public static bool Update(IntPtr sbmID, double timeInSeconds)
    {
        return WRAPPER_SBM_Update(sbmID, timeInSeconds);
    }

    public static bool UpdateUsingDelta(IntPtr sbmID, double deltaTimeInSeconds)
    {
        return WRAPPER_SBM_UpdateUsingDelta(sbmID, deltaTimeInSeconds);
    }

    public static bool ProcessVHMsgs(IntPtr sbmID, string op, string args)
    {
        return WRAPPER_SBM_ProcessVHMsgs(sbmID, GetStringIntPtr(op), args);
    }

    public static bool InitCharacter(IntPtr sbmID, string name, ref SmartbodyCharacterFrameData character)
    {
        return WRAPPER_SBM_InitCharacter(sbmID, GetStringIntPtr(name), ref character);
    }

    public static bool GetCharacter(IntPtr sbmID, string name, ref SmartbodyCharacterFrameData character)
    {
        return WRAPPER_SBM_GetCharacter(sbmID, GetStringIntPtr(name), ref character);
    }

    public static bool ReleaseCharacter(ref SmartbodyCharacterFrameData character)
    {
        return WRAPPER_SBM_ReleaseCharacter(ref character);
    }

    public static bool IsCharacterCreated(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder objectClass, int maxObjectClassLen)
    {
        return WRAPPER_SBM_IsCharacterCreated(sbmID, name, maxNameLen, objectClass, maxObjectClassLen);
    }

    public static bool IsLogMessageWaiting(IntPtr sbmID, StringBuilder logMessage, int maxLogMessageLen, ref int logMessageType)
    {
        return WRAPPER_SBM_IsLogMessageWaiting(sbmID, logMessage, maxLogMessageLen, ref logMessageType);
    }

    public static bool IsCharacterDeleted(IntPtr sbmID, StringBuilder name, int maxNameLen)
    {
        return WRAPPER_SBM_IsCharacterDeleted(sbmID, name, maxNameLen);
    }

    public static bool IsCharacterChanged(IntPtr sbmID, StringBuilder name, int maxNameLen)
    {
        return WRAPPER_SBM_IsCharacterChanged(sbmID, name, maxNameLen);
    }

    public static bool IsVisemeSet(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder visemeName, int maxVisemeNameLen, ref float weight, ref float blendTime)
    {
        return WRAPPER_SBM_IsVisemeSet(sbmID, name, maxNameLen, visemeName, maxVisemeNameLen, ref weight, ref blendTime);
    }

    public static bool IsChannelSet(IntPtr sbmID, StringBuilder name, int maxNameLen, StringBuilder channelName, int maxChannelNameLen, ref float value)
    {
        return WRAPPER_SBM_IsChannelSet(sbmID, name, maxNameLen, channelName, maxChannelNameLen, ref value);
    }

    public static bool IsBmlRequestWaiting(IntPtr sbmId, StringBuilder name, int maxNameLen, StringBuilder requestId, int maxRequestIdLen, StringBuilder bmlName, int maxBmlNameLen)
    {
        return WRAPPER_SBM_IsBmlRequestWaiting(sbmId, name, maxBmlNameLen, requestId, maxRequestIdLen, bmlName, maxBmlNameLen);
    }

    public static void SendBmlReply(IntPtr sbmId, string charName, string requestId, string utteranceId, string rawBml)
    {
        WRAPPER_SBM_SendBmlReply(sbmId, charName, requestId, utteranceId, rawBml);
    }

    public static void PythonCommandVoid(IntPtr sbmID, string command)
    {
        if (m_fileLogging)  // check the variable first as a pre-check to optimize out the Replace() calls.
            LogMessage(command.Replace("{", "{{").Replace("}", "}}") + "\n");  // we have to re-insert the brace escape characters, if braces are used in the python code (eg, python str.format() calls)

        WRAPPER_SBM_PythonCommandVoid(sbmID, command);
    }

    public static bool PythonCommandBool(IntPtr sbmID, string command)
    {
        return WRAPPER_SBM_PythonCommandBool(sbmID, command);
    }

    public static int PythonCommandInt(IntPtr sbmID, string command)
    {
        return WRAPPER_SBM_PythonCommandInt(sbmID, command);
    }

    public static float PythonCommandFloat(IntPtr sbmID, string command)
    {
        return WRAPPER_SBM_PythonCommandFloat(sbmID, command);
    }

    public static void PythonCommandString(IntPtr sbmID, string command, StringBuilder output, int capacity)
    {
        WRAPPER_SBM_PythonCommandString(sbmID, command, output, capacity);
    }

    public static bool SBAssetManager_LoadSkeleton(IntPtr sbmID, IntPtr data, int sizeBytes, string skeletonName)
    {
        LogMessage("SBAssetManager_LoadSkeleton({0}, {1})\n", sizeBytes, skeletonName);

        return WRAPPER_SBM_SBAssetManager_LoadSkeleton(sbmID, data, sizeBytes, GetStringIntPtr(skeletonName));
    }

    public static bool SBAssetManager_LoadMotion(IntPtr sbmID, IntPtr data, int sizeBytes, string motionName)
    {
        LogMessage("SBAssetManager_LoadMotion({0}, {1})\n", sizeBytes, motionName);

        return WRAPPER_SBM_SBAssetManager_LoadMotion(sbmID, data, sizeBytes, GetStringIntPtr(motionName));
    }

    public static void SBDebuggerServer_SetID(IntPtr sbmID, string id)
    {
        WRAPPER_SBM_SBDebuggerServer_SetID(sbmID, GetStringIntPtr(id));
    }

    public static void SBDebuggerServer_SetCameraValues(IntPtr sbmID, double x, double y, double z, double rx, double ry, double rz, double rw, double fov, double aspect, double zNear, double zFar)
    {
        WRAPPER_SBM_SBDebuggerServer_SetCameraValues(sbmID, x, y, z, rx, ry, rz, rw, fov, aspect, zNear, zFar);
    }

    public static void SBDebuggerServer_SetRendererIsRightHanded(IntPtr sbmID, bool enabled)
    {
        WRAPPER_SBM_SBDebuggerServer_SetRendererIsRightHanded(sbmID, enabled);
    }

    public static void SBMotion_AddChannel(IntPtr sbmID, string motionName, string channelName, string channelType)
    {
        LogMessage("SBMotion_AddChannel({0}, {1}, {2})\n", motionName, channelName, channelType);

        WRAPPER_SBM_SBMotion_AddChannel(sbmID, GetStringIntPtr(motionName), GetStringIntPtr(channelName), GetStringIntPtr(channelType));
    }

    public static void SBMotion_AddChannels(IntPtr sbmID, string motionName, IntPtr [] channelNames, IntPtr [] channelTypes)
    {
        LogMessage("SBMotion_AddChannels({0}, {1}, {2})\n", motionName, channelNames.Length, channelTypes.Length);

        WRAPPER_SBM_SBMotion_AddChannels(sbmID, GetStringIntPtr(motionName), channelNames, channelTypes, channelNames.Length);  // both arrays need to be the same length
    }

    public static void SBMotion_AddFrame(IntPtr sbmID, string motionName, float frameTime, IntPtr frameData, int numFrameData)
    {
        LogMessage("SBMotion_AddFrame({0}, {1}, {2})\n", motionName, frameTime, numFrameData);

        WRAPPER_SBM_SBMotion_AddFrame(sbmID, GetStringIntPtr(motionName), frameTime, frameData, numFrameData);
    }

    public static void SBMotion_SetSyncPoint(IntPtr sbmID, string motionName, string syncTag, double time)
    {
        LogMessage("SBMotion_SetSyncPoint({0}, {1}, {2})\n", motionName, syncTag, time);

        WRAPPER_SBM_SBMotion_SetSyncPoint(sbmID, GetStringIntPtr(motionName), GetStringIntPtr(syncTag), time);
    }

    public static void SBJointMap_GetMapTarget(IntPtr sbmID, string jointMap, string jointName, StringBuilder mappedJointName, int maxMappedJointName)
    {
        WRAPPER_SBM_SBJointMap_GetMapTarget(sbmID, GetStringIntPtr(jointMap), GetStringIntPtr(jointName), mappedJointName, maxMappedJointName);
    }

    public static void SBDiphoneManager_CreateDiphone(IntPtr sbmID, string fromPhoneme, string toPhoneme, string name)
    {
        WRAPPER_SBM_SBDiphoneManager_CreateDiphone(sbmID, GetStringIntPtr(fromPhoneme), GetStringIntPtr(toPhoneme), GetStringIntPtr(name));
    }

    public static void SBDiphone_AddKey(IntPtr sbmID, string fromPhoneme, string toPhoneme, string name, string viseme, float time, float weight)
    {
        WRAPPER_SBM_SBDiphone_AddKey(sbmID, GetStringIntPtr(fromPhoneme), GetStringIntPtr(toPhoneme), GetStringIntPtr(name), GetStringIntPtr(viseme), time, weight);
    }

    public static void SBVHMsgManager_SetServer(IntPtr sbmID, string server)
    {
        WRAPPER_SBM_SBVHMsgManager_SetServer(sbmID, GetStringIntPtr(server));
    }

    public static void SBVHMsgManager_SetScope(IntPtr sbmID, string scope)
    {
        WRAPPER_SBM_SBVHMsgManager_SetScope(sbmID, GetStringIntPtr(scope));
    }

    public static void SBVHMsgManager_SetPort(IntPtr sbmID, string port)
    {
        WRAPPER_SBM_SBVHMsgManager_SetPort(sbmID, GetStringIntPtr(port));
    }

    public static void SBVHMsgManager_SetEnable(IntPtr sbmID, bool enable)
    {
        WRAPPER_SBM_SBVHMsgManager_SetEnable(sbmID, enable);
    }


    protected static IntPtr LoadLibraryInternal(string path)
    {
        IntPtr ptr = LoadLibrary(path);
        if (ptr == IntPtr.Zero)
        {
            int errorCode = Marshal.GetLastWin32Error();
            Debug.LogError(string.Format("Failed to load {1} (ErrorCode: {0})", errorCode, path));
        }
        else
        {
            //Debug.Log("Loaded: " + path);
        }
        return ptr;
    }


    public static void LogMessage(string format, params object[] args)
    {
        if (m_fileLogging)
            VHFile.FileWrapper.AppendAllText(m_fileLoggingName, string.Format(format, args));
    }

    public static IntPtr GetStringIntPtr(string value)
    {
        IntPtr valueIntPtr;
        if (!m_stringCache.TryGetValue(value, out valueIntPtr))
        {
            valueIntPtr = Marshal.StringToHGlobalAnsi(value);
            m_stringCache[value] = valueIntPtr;
        }

        return valueIntPtr;
    }

    public static void ClearStringCache()
    {
        // Should clear on exit to free unmanaged memory.  Or whenever cache grows too large.

        foreach (var entry in m_stringCache)
        {
            Marshal.FreeHGlobal(entry.Value);
        }

        m_stringCache.Clear();
    }
}
