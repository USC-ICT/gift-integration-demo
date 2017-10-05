using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using System.Runtime.InteropServices;

public class ExternalProcesses : MonoBehaviour
{
    public string m_sceneToLoadWhenFinished = "Game";

    //bool m_npcEditorLoaded = false;
    bool m_nvbgLoaded = false;
    bool m_stanfordParserLoaded = false;
    bool m_ttsRelayLoaded = false;
    //bool m_floresLoaded = false;
    //bool m_loggerLoaded = false;
    bool m_elsenderLoaded = false;

    //bool m_vrComponentNpcEditorReceived = false;
    bool m_vrComponentNvbGeneratorReceived = false;
    bool m_vrComponentStanfordParserReceived = false;
    bool m_vrComponentTtsReceived = false;
    //bool m_vrComponentFloresReceived = false;
    //bool m_vrComponentLoggerReceived = false;
    bool m_vrComponentElsenderReceived = false;

    static bool m_externalProcessesLaunched = false;


    public void Start()
    {
        // only do this once, at startup
        if (!m_externalProcessesLaunched)
        {
            VHMsgBase vhmsg = VHMsgBase.Get();
            vhmsg.SubscribeMessage("vrAllCall");
            vhmsg.SubscribeMessage("vrKillComponent");
            vhmsg.SubscribeMessage("vrComponent");
            vhmsg.SubscribeMessage("vrProcEnd");
            vhmsg.AddMessageEventHandler(VHMsg_MessageEvent);

            StartCoroutine(StartProcesses());
        }
    }


    public void Update()
    {
        // only do this while we're waiting on external processes to launch (at startup)
        if (m_externalProcessesLaunched)
            return;


        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            // bypass waiting on external processes
            m_externalProcessesLaunched = true;
            if (!string.IsNullOrEmpty(m_sceneToLoadWhenFinished))
                VHUtils.SceneManagerLoadScene(m_sceneToLoadWhenFinished);
        }


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // early quit if something went wrong
            VHUtils.ApplicationQuit();
        }
    }


    public void OnApplicationQuit()
    {
        //VHMsgBase.Get().SendVHMsg("vrKillComponent npceditor");

        //Debug.LogFormat("OnApplicationQuit()");

        // same values as in the Player Preferences.  We want these values on startup because we launch separate processes
        // we want a small windowed screen while the processes load up, then switch to fullscreen after they are loaded (see StartProcesses())
        // we set these on exit because they get set to the last used resolution on every runthrough, which is not what we want.
        PlayerPrefs.SetInt("Screenmanager Is Fullscreen mode", 0);
        PlayerPrefs.SetInt("Screenmanager Resolution Width", 1280);
        PlayerPrefs.SetInt("Screenmanager Resolution Height", 720);

        // this is handled in VHMsgManager -> Messages to send at quit
        //StopNPCEditor();
        //StopNVBG();
        //StopTTSRelay();
        //StopFlores();
    }


    IEnumerator StartProcesses()
    {
        IntPtr hWnd = GetActiveWindow();

        // set to windowed mode, for splash screen
        Screen.SetResolution(1280, 720, false);

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // maximize it to fill the screen
        if (!VHUtils.IsEditor())
        {
            ShowWindow(hWnd, SW_MAXIMIZE);
        }


        //StartCoroutine(StartNPCEditor());
        StartCoroutine(StartNVBG());
        StartCoroutine(StartStanfordParser());
        StartCoroutine(StartTTSRelay());
        //StartCoroutine(StartFlores());
        //StartCoroutine(StartLogger());
        StartCoroutine(StartElsender());

        while (!(m_nvbgLoaded && m_stanfordParserLoaded && m_ttsRelayLoaded && m_elsenderLoaded))
        {
            yield return new WaitForEndOfFrame();
        }

        Debug.LogFormat("ExternalProcesses.StartProcesses() - All external processes loaded");

        // bring this app back into focus
        if (!VHUtils.IsEditor())
        {
            SetForegroundWindow(hWnd);
        }

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // switch to full screen, now that external processes are launched

        // find the 'native' resolution.
        // According to http://docs.unity3d.com/ScriptReference/Screen-resolutions.html the last entry will be the largest width.
        // In most cases, this will match the native res.  Exceptions would be different aspect variations with the same width value.
        // I would hope that if the same width is found, the list is then sorted by height.
        Resolution highestResolution = Screen.currentResolution;
        if (Screen.resolutions.Length > 0)
            highestResolution = Screen.resolutions[Screen.resolutions.Length - 1];

        int prefsWidth = PlayerPrefs.GetInt("palUserSettingResolutionX", -1);
        if (prefsWidth != -1)
            highestResolution.width = prefsWidth;

        int prefsHeight = PlayerPrefs.GetInt("palUserSettingResolutionY", -1);
        if (prefsHeight != -1)
            highestResolution.height = prefsHeight;

        if (VHUtils.HasCommandLineArgument("screen-width") &&
            VHUtils.HasCommandLineArgument("screen-height"))
        {
            highestResolution.width = Convert.ToInt32(VHUtils.GetCommandLineArgumentValue("screen-width"));
            highestResolution.height = Convert.ToInt32(VHUtils.GetCommandLineArgumentValue("screen-height"));
        }

        Screen.SetResolution(highestResolution.width, highestResolution.height, true);

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        m_externalProcessesLaunched = true;
        if (!string.IsNullOrEmpty(m_sceneToLoadWhenFinished))
            VHUtils.SceneManagerLoadScene(m_sceneToLoadWhenFinished);
    }


    public IEnumerator StartNVBG()
    {
        // vrComponent nvb generator
        // vrComponent nvb parser
        // vrKillComponent nvb
        // vrProcEnd nvb
        // vrProcEnd nvb parser

        VHMsgBase.Get().SendVHMsg("vrKillComponent nvb");
        
        yield return new WaitForSeconds(0.5f);

        // pskill nvb

        yield return new WaitForSeconds(0.5f);

        m_vrComponentNvbGeneratorReceived = false;

        // start external process
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = Application.streamingAssetsPath + "/Bin/" + "run-nvbg.bat";
        startInfo.Arguments = "";
        startInfo.WorkingDirectory = Application.streamingAssetsPath + "/Bin";
        //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
        System.Diagnostics.Process.Start(startInfo);

        float startTime = Time.time;
        float timeOut = 18; // in seconds

        // wait for vrComponent message, or timeout after a period
        while (m_vrComponentNvbGeneratorReceived &&
               Time.time - startTime < timeOut)
        {
            yield return new WaitForEndOfFrame();
        }

        m_nvbgLoaded = true;
    }


#if false
    void StopNVBG()
    {
        VHMsgBase.Get().SendVHMsg("vrKillComponent nvb");

        System.Threading.Thread.Sleep(250);

        // pskill nvb

        System.Threading.Thread.Sleep(250);
    }
#endif


    public IEnumerator StartStanfordParser()
    {
        // vrComponent nvb generator
        // vrComponent nvb parser
        // vrKillComponent nvb
        // vrProcEnd nvb
        // vrProcEnd nvb parser

        //VHMsgBase.Get().SendVHMsg("vrKillComponent nvb");
        
        yield return new WaitForSeconds(0.5f);

        // pskill nvb

        yield return new WaitForSeconds(0.5f);

        m_vrComponentStanfordParserReceived = false;

        // start external process
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = Application.streamingAssetsPath + "/Bin/" + "run-stanford-parser.bat";
        startInfo.Arguments = "";
        startInfo.WorkingDirectory = Application.streamingAssetsPath + "/Bin";
        //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
        System.Diagnostics.Process.Start(startInfo);

        float startTime = Time.time;
        float timeOut = 18; // in seconds

        // wait for vrComponent message, or timeout after a period
        while (m_vrComponentStanfordParserReceived == false &&
               Time.time - startTime < timeOut)
        {
            yield return new WaitForEndOfFrame();
        }

        m_stanfordParserLoaded = true;
    }


    public IEnumerator StartTTSRelay()
    {
        // vrComponent ttsmsspeechrelay  *or*
        // vrComponent tts msspeechrelay
        // vrKillComponent tts
        // vrProcEnd tts msspeechrelay

        VHMsgBase.Get().SendVHMsg("vrKillComponent tts");

        yield return new WaitForSeconds(0.5f);

        // pskill tts

        yield return new WaitForSeconds(0.5f);

        m_vrComponentTtsReceived = false;

        // start external process
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = Application.streamingAssetsPath + "/Bin/" + "run-ttsrelay.bat";
        startInfo.Arguments = "";
        startInfo.WorkingDirectory = Application.streamingAssetsPath + "/Bin";
        //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
        System.Diagnostics.Process.Start(startInfo);

        float startTime = Time.time;
        float timeOut = 18; // in seconds

        // wait for vrComponent message, or timeout after a period
        while (m_vrComponentTtsReceived == false &&
               Time.time - startTime < timeOut)
        {
            yield return new WaitForEndOfFrame();
        }

        m_ttsRelayLoaded = true;
    }

#if false
    void StopTTSRelay()
    {
        VHMsgBase.Get().SendVHMsg("vrKillComponent tts");

        System.Threading.Thread.Sleep(250);

        // pskill tts

        System.Threading.Thread.Sleep(250);
    }
#endif


    public IEnumerator StartElsender()
    {
        VHMsgBase.Get().SendVHMsg("vrKillComponent elsender");

        yield return new WaitForSeconds(0.5f);

        // pskill elsender

        yield return new WaitForSeconds(0.5f);

        m_vrComponentElsenderReceived = false;

        // start external process
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.FileName = Application.streamingAssetsPath + "/Bin/" + "run-elsender.bat";
        startInfo.Arguments = "";
        startInfo.WorkingDirectory = Application.streamingAssetsPath + "/Bin";
        //startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
        System.Diagnostics.Process.Start(startInfo);

        float startTime = Time.time;
        float timeOut = 18; // in seconds

        // wait for vrComponent message, or timeout after a period
        while (m_vrComponentElsenderReceived == false &&
               Time.time - startTime < timeOut)
        {
            yield return new WaitForEndOfFrame();
        }

        m_elsenderLoaded = true;
    }


    void VHMsg_MessageEvent(object sender, VHMsgBase.Message message)
    {
        // only do this while we're waiting on external processes to launch (at startup, in CommandLine scene)
        if (m_externalProcessesLaunched)
            return;

        string [] splitargs = message.s.Split( " ".ToCharArray() );

        if (splitargs.Length > 0)
        {
            if (splitargs[0] == "vrComponent")
            {
                // vrComponent nvb generator
                // vrComponent nvb parser
                // vrComponent ttsmsspeechrelay  *or*
                // vrComponent tts msspeechrelay
                // vrComponent pal3nl pal3nl
                // vrComponent npceditor
                // vrComponent logger jlogger

                if (splitargs[1] == "nvb")
                {
                    if (splitargs[2] == "generator")
                    {
                        m_vrComponentNvbGeneratorReceived = true;
                    }
                    else if (splitargs[2] == "parser")
                    {
                        m_vrComponentStanfordParserReceived = true;
                    }
                }
                else if (splitargs[1] == "ttsmsspeechrelay")
                {
                    m_vrComponentTtsReceived = true;
                }
                else if (splitargs[1] == "tts")
                {
                    m_vrComponentTtsReceived = true;
                }
                else if (splitargs[1] == "pal3nl")
                {
                    //m_vrComponentFloresReceived = true;
                }
                else if (splitargs[1] == "npceditor")
                {
                    //m_vrComponentNpcEditorReceived = true;
                }
                else if (splitargs[1] == "logger")
                {
                    //m_vrComponentLoggerReceived = true;
                }
                else if (splitargs[1] == "elsender")
                {
                    m_vrComponentElsenderReceived = true;
                }
            }
        }
    }


    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern Int32 SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int SW_MAXIMIZE = 3;
}
