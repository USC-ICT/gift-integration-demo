using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;

public class MainTab : VHMain
{
    #region Constants
    const string TipBoxStartKey = "vhToolkitShowTipsAtStart";

    /// <summary>
    ///  private stuff
    /// </summary>
    enum AcquireSpeechState
    {
        Disabled,
        Off,
        On,
        InUse,
        NUM_STATES
    }

    enum PerceptionApplicationState
    {
        Disabled,
        TrackHead,
        TrackGaze
    }

    enum GameMode
    {
        FreeLook,
        Character,
    }
    #endregion

    #region Variables
    public VHMsgManager vhmsg;
    public FreeMouseLook m_camera;
    public SmartbodyManager m_SBM;
    public SpeechBox m_SpeechBox;
    public SBCharacterController m_CharacterController;
    public bool m_displayVhmsgLog = false;
    public Texture[] m_MicrophoneImages = new Texture[(int)AcquireSpeechState.NUM_STATES];
    public Texture2D m_whiteTexture;
    public Cutscene m_IntroCutscene;
    Color m_currentColor;    
    public GameObject m_LoadingScreenWhiteBg;
    public float m_DelayTimeAfterCutsceneFinishes = 2;
    public UnitySmartbodyCharacter[] m_Characters;
    
    // cameras
    public GameObject normalCamera;
    public MaterialCustomizer[] m_MaterialCustomizers;
    private bool m_forceGazeOnSetCamera = false;
    GameObject[] allCameras;
    
    Vector3 m_StartingCameraPosition;
    Quaternion m_StartingCameraRotation;

    AcquireSpeechState m_AcquireSpeechState = AcquireSpeechState.Disabled;
    PerceptionApplicationState m_PerceptionApplicationState = PerceptionApplicationState.Disabled;
    GameMode m_GameMode = GameMode.FreeLook;
    string m_SeqFile = "";
    string m_PyFile = "init-unity";
    //string m_locoCharacterName = "brad";

    // for acquire speech
    int m_BradTalkId = 128;

    // toggles
    bool m_bLocomotionEnabled = true;
    bool m_showCustomizerGUI = false;
    bool m_bFinishedPreviousUtterance = true;
    bool m_bStartInAcquireSpeechMode = true;
    bool m_bIntroSequencePlaying = false;
    private bool m_showController = false;
    private float m_timeSlider = 1.0f;
    bool m_walkToMode = false;
    Vector3 m_walkToPoint;
    bool m_disableGUI = false;
    Texture2D m_CachedBgTexture;

    Rect m_MicImagePos;

    private string [] testUtteranceButtonText = { "1", "2", "Tts", "Tts2", "V2a", "V2b" };
    private string[] GameModeNames;
    private int testUtteranceSelected = 0;
    private string [] testUtteranceCharacter = { "Brad", "Rachel", "Brad", "Rachel", "*", "*" };
    private string [] testUtteranceName = { "brad_byte", "rachel_usc", "speech_womanTTS", "speech_womanTTS", "z_viseme_test2", "z_viseme_test3" };
    private string [] testUtteranceText = { "", "", "If the system cannot find my regular voice, it defaults back to the Windows standard voice. Depending on your version of Windows that can be a womans voice. Dont I sound delightful?", "If the system cannot find my regular voice, it defaults back to the Windows standard voice. Depending on your version of Windows that can be a womans voice. Dont I sound delightful?", "", "" };  // the TTS text
    private string [] testTtsVoices = { "Festival_voice_cmu_us_jmk_arctic_clunits", "Festival_voice_cmu_us_clb_arctic_clunits", "Festival_voice_rab_diphone", "Festival_voice_kal_diphone", "Festival_voice_ked_diphone", "Microsoft|Anna", "star", "katherine" };
    private string[] perceptionButtonText = { @"PerceptionApp OFF", @"Track Head", @"Track Gaze" };
    private string[] sceneNames = { "Campus", "House", "LineUp", "Customizer", "CampusEmpty", "OculusRiftTest", "CampusTacQ" };
    private string[] characterNames;
    private int testTtsSelected = 0;
    private int m_SelectedCharacter;
   
    private int perceptionSelected = 0;

    Vector3    m_chrBradStartPos;
    Quaternion m_chrBradStartRot;
    Vector3    m_chrRachelStartPos;
    Quaternion m_chrRachelStartRot;

    int m_gazingMode = 1;  // 0 - off, 1 - gaze camera, 2 - gaze mouse cursor
    bool m_idleMode = true;

    List<GameObject> m_BrownHeads = new List<GameObject>();

    #endregion

    #region Properties
    bool IsSpeechTextBoxInFocus
    {
        get { return SpeechBox.SpeechTextFieldName == GUI.GetNameOfFocusedControl(); }
    }

    bool InAcquireSpeechMode
    {
        get { return m_AcquireSpeechState == AcquireSpeechState.On || m_AcquireSpeechState == AcquireSpeechState.InUse; }
    }

    UnitySmartbodyCharacter SelectedCharacter
    {
        get { return m_Characters[m_SelectedCharacter]; }
    }

    SBCharacterController SelectedCharacterController
    {
        get { return SelectedCharacter.GetComponent<SBCharacterController>(); }
    }
    #endregion


    public void TossNPCDomain()
    {
        if (VHUtils.SceneManagerActiveSceneName() == "House")
        {
            string tossmessage = "";
            tossmessage = string.Format("vrSpeech start user0001 user");
            vhmsg.SendVHMsg(tossmessage);
            tossmessage = string.Format("vrSpeech finished-speaking user0001");
            vhmsg.SendVHMsg(tossmessage);
            tossmessage = string.Format("vrSpeech interp user0001 1 1.0 normal INTEROCITOR SEATEC ASTRONOMY TRANSMOGRIFY EXPELIARMUS");
            vhmsg.SendVHMsg(tossmessage);
            tossmessage = string.Format("vrSpeech asr-complete user0001");
            vhmsg.SendVHMsg(tossmessage);
            tossmessage = string.Format("vrSpoke");
            vhmsg.SendVHMsg(tossmessage);
        }
        else
        {
            string tossmessage = "";
            tossmessage = string.Format("vrSpeech start user0001 user");
            vhmsg.SendVHMsg(tossmessage);
            tossmessage = string.Format("vrSpeech finished-speaking user0001");
            vhmsg.SendVHMsg(tossmessage);
            tossmessage = string.Format("vrSpeech interp user0001 1 1.0 normal THEREMIN NOSFERATU THERMOCOUPLE PATRONUS");
            vhmsg.SendVHMsg(tossmessage);
            tossmessage = string.Format("vrSpeech asr-complete user0001");
            vhmsg.SendVHMsg(tossmessage);
            tossmessage = string.Format("vrSpoke");
            vhmsg.SendVHMsg(tossmessage);
        }
    }

    public override void Start()
    {
        Application.targetFrameRate = 60;
        base.Start();        

        m_userDialogText = "";
        m_subtitleText = "";
        DisplaySubtitles = true;
        DisplayUserDialog = true;
        //m_currentColor = new Color();
        //m_currentColor = GameObject.Find("Background").renderer.material.color;

        if (m_IntroCutscene != null)
        {
            m_IntroCutscene.AddOnFinishedCutsceneCallback(IntroCutsceneFinished);
            m_IntroCutscene.AddOnEventFiredCallback(IntroEventFired);
        }

        m_StartingCameraPosition = m_camera.transform.position;
        m_StartingCameraRotation = m_camera.transform.rotation;


        ProcessCommandLineAndConfigSettings();

        GameModeNames = Enum.GetNames(typeof(GameMode));


        if (VHUtils.SceneManagerActiveSceneName() == "Campus")
        {
            if (m_Characters.Length > 0)
            {
                characterNames = new string[m_Characters.Length];
                for (int i = 0; i < m_Characters.Length; i++)
                {
                    if (m_Characters[i].GetComponent<SBCharacterController>() != null)
                    {
                        m_Characters[i].GetComponent<SBCharacterController>().enabled = false;
                    }
                    characterNames[i] = m_Characters[i].name;
                }
            }

            SelectCharacter(0);
        }


        m_SBM = SmartbodyManager.Get();


        {
            Debug.Log("Using Smartbody dll");

            m_SBM.AddCustomCharCreateCB(new SmartbodyManager.OnCustomCharacterCallback(OnCharacterCreate));
            m_SBM.AddCustomCharDeleteCB(new SmartbodyManager.OnCustomCharacterCallback(OnCharacterDelete));
        }


        m_Console.AddCommandCallback("set_loco_char_name", new DebugConsole.ConsoleCallback(HandleConsoleMessage));
        m_Console.AddCommandCallback("play_intro", new DebugConsole.ConsoleCallback(HandleConsoleMessage));
        m_Console.AddCommandCallback("set_tips", new DebugConsole.ConsoleCallback(HandleConsoleMessage));
        m_Console.AddCommandCallback("show_tips", new DebugConsole.ConsoleCallback(HandleConsoleMessage));

        m_MicImagePos = new Rect(0.92f, 0.85f, 0.06f, 0.06f);

        SubscribeVHMsg();

        StartCoroutine(ShowIntro(0));

        {
            var brad = GameObject.Find("Brad");
            if (brad)
            {
                m_chrBradStartPos = brad.transform.position;
                m_chrBradStartRot = brad.transform.rotation;
            }

            var rachel = GameObject.Find("Rachel");
            if (rachel)
            {
                m_chrRachelStartPos = rachel.transform.position;
                m_chrRachelStartRot = rachel.transform.rotation;
            }
        }


#if UNITY_IPHONE || UNITY_ANDROID
        if (!m_Console.DrawConsole) m_Console.ToggleConsole();
        m_showController = true;
#endif

        if (VHUtils.SceneManagerActiveSceneName() == "Customizer")
        {
            //m_disableGUI = true;

            var speechBox = GameObject.Find("SpeechBox");
            if (speechBox)
                speechBox.GetComponent<SpeechBox>().Show = false;

            m_AcquireSpeechState = AcquireSpeechState.Disabled;

            PlayIdleFidgets(false);

            m_currentColor = Color.white;




            //GameObject.Find("screen").renderer.material = null;

            List<string> names = new List<string>();
            cameraChoices = GameObject.FindObjectsOfType(typeof(Camera)) as Camera[];
            Array.Sort(cameraChoices, (a, b) => a.gameObject.name.CompareTo(b.gameObject.name) );
            foreach (Camera camera in cameraChoices)
            {
                if (!(camera.gameObject.name == "Camera01_mediumCt"))
                    camera.GetComponent<SmartbodyPawn>().AddToSmartbody();

                camera.gameObject.SetActive(false);
                names.Add(camera.gameObject.name);

                if (camera.gameObject.name == "Camera01_mediumCt")
                {
                    m_cameraSelectCurrent = names.Count - 1;
                    cameraSelGridInt = names.Count - 1;
                }
            }
            cameraChoices[m_cameraSelectCurrent].gameObject.SetActive(true);

            cameraChoicesStrings = names.ToArray();
            cameraSelectionHeight = cameraChoicesStrings.Length * 29;
        }
        else if (VHUtils.SceneManagerActiveSceneName() == "Campus")
        {
            UnSelectCharacters();
        }


        //Find all the brown heads for hooking up the skin shader
        string[] brownPrefabNames = { "ChrBrownRocPrefab" };
        for (int i = 0; i < brownPrefabNames.Length; i++)
        {
            GameObject brownPrefab = GameObject.Find(brownPrefabNames[i]);

            if (brownPrefab == null)
            {
                Debug.LogError(brownPrefabNames[i] + " doesn't exist in the scene, so his head couldn't be found");
            }
            else
            {
                UnitySmartbodyCharacter sbcomponent = brownPrefab.GetComponent<UnitySmartbodyCharacter>();
                if (sbcomponent)
                {
                    sbcomponent.SetChannelCallback(ChrBrownRocChannelCallback);
                }

                GameObject brownHead = VHUtils.FindChild(brownPrefab, "ChrBrownRoc/CharacterRoot/Mesh/SkinnedMesh/MshRef/Head");
                if (brownHead == null)
                {
                    Debug.LogError("Couldn't find " + brownPrefabNames[i] + "'s head");
                }
                else
                {
                    m_BrownHeads.Add(brownHead);
                }
            }
        }

#if false
        // Transparent window example
        [DllImport("user32.dll")]
        static extern int GetForegroundWindow();
        [DllImport("user32.dll", EntryPoint="MoveWindow")]
        static extern int  MoveWindow (int hwnd, int x, int y,int nWidth,int nHeight,int bRepaint );
        [DllImport("user32.dll", EntryPoint="SetWindowLongA")]
        static extern int  SetWindowLong (int hwnd, int nIndex,int dwNewLong);
        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(int hWnd, int nCmdShow);
        [DllImport("user32.dll", EntryPoint="SetLayeredWindowAttributes")]
        static extern int  SetLayeredWindowAttributes (int hwnd, int crKey,byte bAlpha, int dwFlags );

        int handle = GetForegroundWindow();
        SetWindowLong(handle, -20, 524288); // GWL_EXSTYLE=-20 , WS_EX_LAYERED=524288=&h80000
        SetLayeredWindowAttributes(handle, 0, 128, 2); // handle,color key = 0 >> black, % of transparency, LWA_ALPHA=1
#endif
    }

    void IntroCutsceneFinished(Cutscene cutscene)
    {
        StartCoroutine(IntroCutsceneFinishedCoroutine(cutscene, m_DelayTimeAfterCutsceneFinishes));
    }

    void IntroEventFired(Cutscene cutscene, CutsceneEvent ce)
    {
        if (ce.FunctionName == "Express")
        {
            m_subtitleText = "";
        }
    }

    IEnumerator IntroCutsceneFinishedCoroutine(Cutscene cutscene, float delay)
    {
        yield return new WaitForSeconds(delay);
        CleanupIntroSequence();
    }

    void SubscribeVHMsg()
    {
        VHMsgBase vhmsg = VHMsgBase.Get();
        vhmsg.SubscribeMessage("vrAllCall");
        vhmsg.SubscribeMessage("vrKillComponent");
        vhmsg.SubscribeMessage("vrExpress");
        vhmsg.SubscribeMessage("vrSpoke");
        vhmsg.SubscribeMessage("CommAPI");
        vhmsg.SubscribeMessage("acquireSpeech");
        vhmsg.SubscribeMessage("PlaySound");
        vhmsg.SubscribeMessage("StopSound");
        vhmsg.SubscribeMessage("renderer");
        vhmsg.SubscribeMessage("render_text_overlay");
        vhmsg.SubscribeMessage("vht_get_characters");
        vhmsg.SubscribeMessage("renderer_record");
        vhmsg.SubscribeMessage("renderer_gui");
        vhmsg.SubscribeMessage("sbm");

        vhmsg.AddMessageEventHandler(new VHMsgBase.MessageEventHandler(VHMsg_MessageEvent));

        vhmsg.SendVHMsg("vrComponent renderer");

        if (m_AcquireSpeechState != AcquireSpeechState.Disabled)
        {
            vhmsg.SendVHMsg("acquireSpeech start");
        }

        {
            if (!string.IsNullOrEmpty(m_SeqFile))
                vhmsg.SendVHMsg("sbm seq " + m_SeqFile);

            if (!string.IsNullOrEmpty(m_PyFile))
                vhmsg.SendVHMsg("sbm pythonscript " + m_PyFile);
        }
    }

    IEnumerator ShowIntro(float delay)
    {
        yield return new WaitForSeconds(delay);

        bool showIntro = !Application.isEditor && VHUtils.HasCommandLineArgument("intro");

        if (showIntro && VHUtils.SceneManagerActiveSceneName() == "Campus")
        {
            m_AcquireSpeechState = AcquireSpeechState.Disabled;

            vhmsg.SendVHMsg("nvbg_set_option Brad saliency_idle_gaze false");
            vhmsg.SendVHMsg("nvbg_set_option Rachel saliency_idle_gaze false");

            m_IntroCutscene.Play();
            m_bIntroSequencePlaying = true;

            StartCoroutine(WaitForCutsceneEnd(m_IntroCutscene.Length));
        }
        else
        {
            if (VHUtils.SceneManagerActiveSceneName() == "Customizer")
            {
                m_AcquireSpeechState = AcquireSpeechState.Disabled;
                PlayIdleFidgets(false);
            }
            else
            {
                m_AcquireSpeechState = AcquireSpeechState.Off;

                for (int i = 0; i < m_SBM.GetSBMCharacterNames().Length; ++i)
                {
                    PlayIdleFidgets(true);
                }
            }
        }        


        TossNPCDomain();
        yield break;
    }

    private void UpdatePerceptionAppState()
    {
        perceptionSelected++;
        perceptionSelected %= perceptionButtonText.Length;

        if (m_PerceptionApplicationState == PerceptionApplicationState.Disabled)
        {
            //vhmsg.SendVHMsg("vrPerceptionApplication", "TOGGLE");
            vhmsg.SendVHMsg("vrPerceptionApplication", "trackHead");
            m_PerceptionApplicationState = PerceptionApplicationState.TrackHead;
        }
        else if (m_PerceptionApplicationState == PerceptionApplicationState.TrackHead)
        {
            vhmsg.SendVHMsg("vrPerceptionApplication", "trackGaze");
            //vhmsg.SendVHMsg("vrPerceptionApplication", "trackHead");
            m_PerceptionApplicationState = PerceptionApplicationState.TrackGaze;
        }
        else if (m_PerceptionApplicationState == PerceptionApplicationState.TrackGaze)
        {
            vhmsg.SendVHMsg("vrPerceptionApplication", "TOGGLE");
            m_PerceptionApplicationState = PerceptionApplicationState.Disabled;
        }
    }

    private void ToggleGazeMode()
    {
        m_gazingMode++;
        m_gazingMode = m_gazingMode % 3;  // skipping mousepawn gaze for tab demo
        if (m_gazingMode == 0)
        {
            m_SBM.PythonCommand(string.Format(@"scene.command('char {0} gazefade out 1')", "*"));
        }

        if (m_gazingMode == 1)
        {
            {
                m_SBM.SBGaze("*", "Camera");
                //m_SBM.SBGaze("*", "Camera", 400, 400, SmartbodyManager.GazeJointRange.EYES_CHEST);
            }
        }

        if (m_gazingMode == 2)
        {
            m_SBM.SBGaze("*", "MousePawn");
        }
    }


    IEnumerator WaitForCutsceneEnd(float cutsceneLength)
    {
        yield return new WaitForSeconds(cutsceneLength);                
    }

    public void PlayIdleFidgets(bool _value)
    {

        
        for (int i = 0; i < m_SBM.GetSBMCharacterNames().Length; ++i)
        {           
            vhmsg.SendVHMsg("nvbg_set_option " + (m_SBM.GetSBMCharacterNames())[i] + " saliency_idle_gaze " + Convert.ToString(_value));
        }


        if (VHUtils.SceneManagerActiveSceneName() == "Customizer")
        {
            vhmsg.SendVHMsg("nvbg_set_option Brad saliency_idle_gaze false");
            vhmsg.SendVHMsg("nvbg_set_option Rachel saliency_idle_gaze false");
        }

    }


    public void Update()
    {
        if (m_SBM)
        {
            m_SBM.m_camPos = m_camera.transform.position;
            m_SBM.m_camRot = m_camera.transform.rotation;
            m_SBM.m_camFovY = m_camera.GetComponent<Camera>().fieldOfView;
            m_SBM.m_camAspect = m_camera.GetComponent<Camera>().aspect;
            m_SBM.m_camZNear = m_camera.GetComponent<Camera>().nearClipPlane;
            m_SBM.m_camZFar = m_camera.GetComponent<Camera>().farClipPlane;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        m_camera.enabled = !m_Console.DrawConsole;

        if (!m_Console.DrawConsole && !IsSpeechTextBoxInFocus) // they aren't typing in a box
        {
            
            if (Input.GetKeyDown(KeyCode.Slash))
            {
                if (m_IntroCutscene)
                {
                    PlayIdleFidgets(false);

                    m_IntroCutscene.Play();
                }

                StartCoroutine(WaitForCutsceneEnd(m_IntroCutscene.Length));
            }


            // kill intro
            if (Input.GetKeyDown(KeyCode.Alpha1) && m_bIntroSequencePlaying)
            {
                StopAllCoroutines();
                CleanupIntroSequence();
            }
   
            //Go forward and backward through 'slides'
            if (Input.GetKeyDown(KeyCode.RightArrow) ||Input.GetKeyDown(KeyCode.Alpha3))
            {
                m_currentSlide = (m_currentSlide + 1) % m_slides.Length;
                SlidesScreen.GetComponent<Renderer>().material.mainTexture = m_slides[m_currentSlide];
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Alpha2))
            {
                m_currentSlide = m_currentSlide == 0 ? m_slides.Length - 1 : m_currentSlide - 1;
                SlidesScreen.GetComponent<Renderer>().material.mainTexture = m_slides[m_currentSlide];
            }
            
            //Trigger TAB initial presentation cutscene
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                Cutscene tabIntroCutscene = GameObject.Find("tab01_ArnoIntro").GetComponent<Cutscene>();
                Debug.Log("tab01_ArnoIntro");
                tabIntroCutscene.Play();
            }
            
            //Trigger Brad's theater cutscene
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                Cutscene tabBradTheaterCutscene = GameObject.Find("tab02_BradTheater").GetComponent<Cutscene>();
                Debug.Log("tab02_BradTheater");
                tabBradTheaterCutscene.Play();
            }
            
            //Trigger camera move to front of theater
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                Cutscene tabTheaterTransitionCutscene = GameObject.Find("tab02b_TransitionToFront").GetComponent<Cutscene>();
                Debug.Log("tab02b_TransitionToFront");
                tabTheaterTransitionCutscene.Play();
            }
            
            //Trigger the campus tour cutscene
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                Cutscene tabCampusTourCutscene = GameObject.Find("tabMasterTour").GetComponent<Cutscene>();
                Debug.Log("tabMasterTour");
                tabCampusTourCutscene.Play();
            }
            
            //Trigger Rachel's interaction
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                Cutscene tabRachelCutscene = GameObject.Find("tab04_RachelInterview").GetComponent<Cutscene>();
                Debug.Log("tab04_RachelInterview");
                tabRachelCutscene.Play();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                m_showController = !m_showController;
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                m_showCustomizerGUI = !m_showCustomizerGUI;
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                SmartbodyManager.Get().PythonCommand(string.Format(@"bml.execBML('{0}', '<blend name=""{1}"" y=""{2}""/>')", "Brad", "ChrMarineStep", -1));
            }

            if (Input.GetKeyUp(KeyCode.G))
            {
                SmartbodyManager.Get().PythonCommand(string.Format(@"bml.execBML('{0}', '<blend name=""{1}"" y=""{2}""/>')", "Brad", "ChrMarineStep", 0));
                SmartbodyManager.Get().PythonCommand(string.Format(@"bml.execBML('{0}', '<blend name=""{1}"" y=""{2}""/>')", "Brad", "PseudoIdle", 0));
            }

            // toggle subtitle text
            if (Input.GetKeyDown(KeyCode.I))
            {
                DisplaySubtitles = !DisplaySubtitles;
            }

            

            // toggle mic input
            if (Input.GetKeyDown(KeyCode.M)
                && m_AcquireSpeechState != AcquireSpeechState.Disabled
                && m_AcquireSpeechState != AcquireSpeechState.InUse)
            {
                SetAcquireSpeechState(m_AcquireSpeechState == AcquireSpeechState.On ? AcquireSpeechState.Off : AcquireSpeechState.On);
            }

            // toggle user dialog text
            if (Input.GetKeyDown(KeyCode.O))
            {
                DisplayUserDialog = !DisplayUserDialog;
            }

            // toggle entire GUI
            if (Input.GetKeyDown(KeyCode.P))
            {
                m_disableGUI = !m_disableGUI;

                var speechBox = GameObject.Find("SpeechBox");

                if (m_disableGUI)
                {
                    if (speechBox)
                        speechBox.GetComponent<SpeechBox>().Show = false;
                }
                else
                {
                    if (speechBox)
                        speechBox.GetComponent<SpeechBox>().Show = true;
                }

            }
            
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                ToggleGazeMode();
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                m_SBM.SBPlayAnim("Brad",          "ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Rachel",        "ChrRachel_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Alexis",        "Alexis_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Carl",          "carl_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Joan",          "Joan_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Justin",        "justin_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Mia",           "Mia_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Monster",       "monster_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Soldier",       "soldier_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Swat",          "soldier_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Vincent",       "soldier_ChrBrad@Idle01_Contemplate01");
                m_SBM.SBPlayAnim("Zombie",        "zombie_hires_ChrBrad@Idle01_Contemplate01");
            }

            // reset camera position
            if (Input.GetKeyDown(KeyCode.X))
            {
                m_camera.transform.position = m_StartingCameraPosition;
                m_camera.transform.rotation = m_StartingCameraRotation;
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                UpdatePerceptionAppState();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                vhmsg.SendVHMsg("vrKillComponent", "ssi_vhmsger");
                vhmsg.SendVHMsg("vrKillComponent", "perception-test-application");
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 2, CharacterDefines.FaceSide.both, 0.6f, 4);
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 45, CharacterDefines.FaceSide.both, 1, 2);
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 100, CharacterDefines.FaceSide.left, 1, 4);
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 100, CharacterDefines.FaceSide.right, 1, 4);
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 110, CharacterDefines.FaceSide.left, 1, 4);
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 110, CharacterDefines.FaceSide.right, 1, 4);
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 120, CharacterDefines.FaceSide.left, 1, 4);
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 120, CharacterDefines.FaceSide.right, 1, 4);
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 50, CharacterDefines.FaceSide.both, 0.6f, 4);
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 50, CharacterDefines.FaceSide.both, 0.6f, 4);
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 100, CharacterDefines.FaceSide.left, 1, 4);
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 50, CharacterDefines.FaceSide.both, 0.6f, 4);
                m_SBM.SBPlayFAC("ChrBrownRocPrefab", 100, CharacterDefines.FaceSide.right, 1, 4);
            }


            // walk to mouse position
            if (m_walkToMode && Input.GetMouseButtonDown(0))
            {
                Ray ray = m_camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log("Walk to: " + -hit.point.x + " " + hit.point.z);
                    //SmartbodyManager.Get().SBWalkTo("*", string.Format("{0} {1}", -hit.point.x, hit.point.z), false);
                    //SmartbodyManager.Get().SBWalkTo("Brad", string.Format("{0} {1}", -hit.point.x - 1, hit.point.z), false);
                    //SmartbodyManager.Get().SBWalkTo("Rachel", string.Format("{0} {1}", -hit.point.x + 1, hit.point.z), false);
                    //SmartbodyManager.Get().SBWalkTo("Rachel", string.Format("{0} {1}", -hit.point.x + 1, hit.point.z), false);
                    //SmartbodyManager.Get().SBWalkTo("ChrBackovicPrefab", string.Format("{0} {1}", -hit.point.x - 1, hit.point.z), false);
                    //SmartbodyManager.Get().SBWalkTo("ChrBrownRocPrefab", string.Format("{0} {1}", -hit.point.x + 1, hit.point.z), false);
                    //SmartbodyManager.Get().SBWalkTo("ChrBackovicPrefab", string.Format("{0} {1}", -hit.point.x - 1, hit.point.z - 1), true);
                    //SmartbodyManager.Get().SBWalkTo("ChrCrowleyPrefab", string.Format("{0} {1}", -hit.point.x - 1, hit.point.z + 1), true);
                    //SmartbodyManager.Get().SBWalkTo("ChrJohnsonPrefab", string.Format("{0} {1}", -hit.point.x + 1, hit.point.z - 1), true);
                    //SmartbodyManager.Get().SBWalkTo("ChrMcHughPrefab", string.Format("{0} {1}", -hit.point.x + 1, hit.point.z + 1), true);
                    SmartbodyManager.Get().SBWalkTo("Brad",              string.Format("{0} {1}", -hit.point.x - 2, hit.point.z), false);
                    SmartbodyManager.Get().SBWalkTo("Rachel",            string.Format("{0} {1}", -hit.point.x + 2, hit.point.z), false);
                    SmartbodyManager.Get().SBWalkTo("ChrCrowleyPrefab",  string.Format("{0} {1}", -hit.point.x,     hit.point.z - 2), false);
                    SmartbodyManager.Get().SBWalkTo("Ellie",             string.Format("{0} {1}", -hit.point.x,     hit.point.z + 2), false);
                    SmartbodyManager.Get().SBWalkTo("ChrBrownRocPrefab", string.Format("{0} {1}", -hit.point.x,     hit.point.z), false);
                    m_walkToPoint = hit.point;
                }
            }

            if (m_gazingMode == 2)  // gaze mouse cursor
            {
                Ray ray = m_camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    GameObject mousePawn = GameObject.Find("MousePawn");
                    mousePawn.transform.position = hit.point;
                }
                else
                {
                    GameObject mousePawn = GameObject.Find("MousePawn");
                    mousePawn.transform.position = m_camera.GetComponent<Camera>().transform.position;
                }
            }

            if (m_CharacterController)
                m_CharacterController.enabled = m_bLocomotionEnabled;
        }

        if (m_AcquireSpeechState == AcquireSpeechState.On
            || m_AcquireSpeechState == AcquireSpeechState.InUse)
        {
            if (Input.GetMouseButtonDown(0)) // 0 == mouse left click
            {
                vhmsg.SendVHMsg("acquireSpeech startUtterance mic");
                m_AcquireSpeechState = AcquireSpeechState.InUse;
            }
            else if (Input.GetMouseButtonUp(0)) // 0 == mouse left click
            {
                vhmsg.SendVHMsg("acquireSpeech stopUtterance mic");
                m_AcquireSpeechState = AcquireSpeechState.On;
            }
        }


        // lock the screen cursor if they are looking around or using their mic
        FreeMouseLook mouseLook = Camera.main ? Camera.main.GetComponent<FreeMouseLook>() : null;
        bool cameraRotationOn = mouseLook ? mouseLook.CameraRotationOn : false;
        Cursor.lockState = (InAcquireSpeechMode || cameraRotationOn) ? CursorLockMode.Locked : CursorLockMode.None;


        if (VHUtils.SceneManagerActiveSceneName() == "Customizer")
        {
            m_currentColor = GameObject.Find("ColorPicker").GetComponent<ColorPicker>().setColor;
            //Debug.LogError(m_currentColor.r + " " + m_currentColor.g + " " + m_currentColor.b + " " + m_currentColor.a);
            GameObject.Find("screen").GetComponent<Renderer>().material.color = m_currentColor;
        }


        UpdateBrownFace();
    }


    // background select
    Vector2 m_scrollPosition;
    int selGridInt = 0;
    int backgroundSelectionHeight = 0;
    string [] backgroundFiles = new string [] {};
    // character select
    Vector2 m_characterScrollPosition;
    static string [] characterChoices = new string [] { "Brad", "Harmony", "JustinIct", "Pedro", "Rachel", "Rio", "Utah" };
    int characterSelectionHeight = characterChoices.Length * 29;
    int characterSelGridInt = 0;
    string m_characterSelectCurrent = "Brad";
    // camera select
    Vector2 m_cameraScrollPosition;
    Camera [] cameraChoices;
    string [] cameraChoicesStrings;
    int cameraSelectionHeight;
    int cameraSelGridInt = 0;
    int m_cameraSelectCurrent = 0;

    public GameObject SlidesScreen;
    public Texture2D [] m_slides;
    int m_currentSlide = 0;


     void SwapTexture(MonoBehaviour behaviour, WWW www)
    {
        behaviour.StartCoroutine(SwapTexture(www));
    }

     
     IEnumerator SwapTexture(WWW www)
     {
         yield return www;
         if (m_CachedBgTexture != null)
         {
             Destroy(m_CachedBgTexture); m_CachedBgTexture = null;
         }
         m_CachedBgTexture = new Texture2D(4, 4);
         www.LoadImageIntoTexture(m_CachedBgTexture); GameObject.Find("screen").GetComponent<Renderer>().material.mainTexture = m_CachedBgTexture;
     }

    

    public override void OnGUI()
    {
        if (m_disableGUI)
            return;


        base.OnGUI();


        if (m_AcquireSpeechState != AcquireSpeechState.Disabled)
        {
            if (m_MicrophoneImages[(int)m_AcquireSpeechState])
                VHIMGUI.DrawTexture(m_MicImagePos, m_MicrophoneImages[(int)m_AcquireSpeechState]);
        }


        if (VHUtils.SceneManagerActiveSceneName() == "Customizer")
        {
            if (m_showCustomizerGUI)
            {
                GUILayout.BeginArea(new Rect(10, 10, 200, 600));
                GUILayout.BeginVertical();

                // background select
                m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, false, true, GUILayout.Height(100), GUILayout.MaxHeight(Math.Max(100, backgroundSelectionHeight)));
                selGridInt = GUILayout.SelectionGrid(selGridInt, backgroundFiles, 1);
                GUILayout.EndScrollView();

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Set"))
                {
                    string path = "Backgrounds/" + backgroundFiles[selGridInt];

                    VHFile.LoadStreamingAssetsAsync(path);

                    WWW www = VHFile.LoadStreamingAssetsAsync(path);
                    SwapTexture(this, www);
                }

                if (GUILayout.Button("Refresh"))
                {
                    string path = "Backgrounds";
                    List<string> files = new List<string>();
                    files.AddRange(VHFile.GetStreamingAssetsFiles(path, ".png"));
                    files.AddRange(VHFile.GetStreamingAssetsFiles(path, ".jpg"));
                    files.AddRange(VHFile.GetStreamingAssetsFiles(path, ".bmp"));

                    for (int i = 0; i < files.Count; i++)
                        files[i] = Path.GetFileName(files[i]);

                    backgroundFiles = files.ToArray();

                    backgroundSelectionHeight = backgroundFiles.Length * 29;
                    selGridInt = 0;
                }

                GUILayout.EndHorizontal();

                if (GUILayout.Button("ColorPicker"))
                {
                    GameObject.Find("ColorPicker").GetComponent<ColorPicker>().showPicker = true;                    
                }

                GUILayout.Space(50);

                // character select
                m_characterScrollPosition = GUILayout.BeginScrollView(m_characterScrollPosition, false, true, GUILayout.Height(100), GUILayout.MaxHeight(Math.Max(100, characterSelectionHeight)));
                characterSelGridInt = GUILayout.SelectionGrid(characterSelGridInt, characterChoices, 1);
                GUILayout.EndScrollView();

                if (GUILayout.Button("Set Character"))
                {
                    VHMsgManager.Get().SendVHMsg(string.Format("renderer destroy {0}", m_characterSelectCurrent));
                    VHMsgManager.Get().SendVHMsg(string.Format("renderer create {0} {1}", characterChoices[characterSelGridInt], characterChoices[characterSelGridInt]));
                    m_characterSelectCurrent = characterChoices[characterSelGridInt];
                }

                GUILayout.Space(50);


                // camera select
                m_cameraScrollPosition = GUILayout.BeginScrollView(m_cameraScrollPosition, false, true, GUILayout.Height(100), GUILayout.MaxHeight(Math.Max(100, cameraSelectionHeight)));
                cameraSelGridInt = GUILayout.SelectionGrid(cameraSelGridInt, cameraChoicesStrings, 1);
                GUILayout.EndScrollView();

                if (GUILayout.Button("Set Camera"))
                {
                    cameraChoices[m_cameraSelectCurrent].gameObject.SetActive(false);

                    m_cameraSelectCurrent = cameraSelGridInt;
                    cameraChoices[m_cameraSelectCurrent].gameObject.SetActive(true);
                    SmartbodyManager.Get().SBGaze("*", cameraChoicesStrings[m_cameraSelectCurrent]);
                }

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }


        if (m_showController)
        {
            float buttonX = 0;
            float buttonY = 0;
#if UNITY_IPHONE || UNITY_ANDROID
            float buttonH = 70;
#else
            float buttonH = 20;
#endif
            float buttonW = 140;

            GUILayout.BeginArea(new Rect(buttonX, buttonY, buttonW, Screen.height));

            GUILayout.BeginVertical();

            for (int i = 0; i < sceneNames.Length; i++)
            {
                if (VHUtils.SceneManagerActiveSceneName() == sceneNames[i])
                {
                    continue;
                }

                if (GUILayout.Button("Load " + sceneNames[i])) { VHUtils.SceneManagerLoadScene(sceneNames[i]); }
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(testUtteranceButtonText[testUtteranceSelected], GUILayout.Height(buttonH)))
            {
                testUtteranceSelected++;
                testUtteranceSelected = testUtteranceSelected % testUtteranceButtonText.Length;
            }
            if (GUILayout.Button("Test Utt", GUILayout.Height(buttonH)))  { m_SBM.SBPlayAudio(testUtteranceCharacter[testUtteranceSelected], testUtteranceName[testUtteranceSelected], testUtteranceText[testUtteranceSelected]); MobilePlayAudio(testUtteranceName[testUtteranceSelected]); }
            GUILayout.EndHorizontal();

            if (GUILayout.Button(testTtsVoices[testTtsSelected], GUILayout.Height(buttonH)))
            {
                testTtsSelected++;
                testTtsSelected = testTtsSelected % testTtsVoices.Length;

                string message = string.Format("sbm set character {0} voicebackup remote {1}", "Brad", testTtsVoices[testTtsSelected]);
                vhmsg.SendVHMsg(message);
            }

            m_walkToMode = GUILayout.Toggle(m_walkToMode, "WalkToMode");

            m_SBM.m_displayLogMessages = GUILayout.Toggle(m_SBM.m_displayLogMessages, "SBMLog");
            m_displayVhmsgLog = GUILayout.Toggle(m_displayVhmsgLog, "VHMsgLog");
            m_timeSlider = GUILayout.HorizontalSlider(m_timeSlider, 0.01f, 3);
            GUILayout.Label(string.Format("Time: {0}", m_timeSlider));

#if !UNITY_WEBPLAYER
            if (GUILayout.Button("Launch Pocketsphinx"))
            {
				System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
				startInfo.FileName = Application.dataPath + "/../" + "../../bin/launch-scripts/run-toolkit-asr-server-TABGFY13.bat";
				startInfo.Arguments = "Pocketphinx";
				startInfo.WorkingDirectory = Application.dataPath + "/../" + "../../bin/launch-scripts";
				System.Diagnostics.Process.Start(startInfo);

            }
			
			if (GUILayout.Button("Launch Acquirespeech"))
			{
				System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
				startInfo.FileName = Application.dataPath + "/../" + "../../bin/launch-scripts/run-toolkit-acquirespeech.bat";
				startInfo.Arguments = "PocketSphinx";
				startInfo.WorkingDirectory = Application.dataPath + "/../" + "../../bin/launch-scripts";
				System.Diagnostics.Process.Start(startInfo);
			}

            if (GUILayout.Button("Launch NPCEditor"))
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = Application.dataPath + "/../" + "../../bin/launch-scripts/run-toolkit-npceditor-vhbuilder.bat";
                startInfo.Arguments = "../../data/classifier/racheltab.plist";
                startInfo.WorkingDirectory = Application.dataPath + "/../" + "../../bin/launch-scripts";
                System.Diagnostics.Process.Start(startInfo);
            }

            if (GUILayout.Button("Launch NVBG"))
            {
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = Application.dataPath + "/../" + "../../bin/launch-scripts/run-toolkit-NVBG-C#-all.bat";
                startInfo.WorkingDirectory = Application.dataPath + "/../" + "../../bin/launch-scripts";
                System.Diagnostics.Process.Start(startInfo);
            }
#endif

            if (GUILayout.Button(perceptionButtonText[perceptionSelected]))
            {
                UpdatePerceptionAppState();
            }

            if (GUILayout.Button("Stop Walking"))
            {
                string message = string.Format(@"bml.execBML('{0}', '<locomotion enable=""{1}"" />')", "*", "false");
                SmartbodyManager.Get().PythonCommand(message);
            }

            string gazeMode = "";
            if (m_gazingMode == 0)       gazeMode = "(Off)";
            else if (m_gazingMode == 1)  gazeMode = "(Camera)";
            else if (m_gazingMode == 2)  gazeMode = "(Mouse)";

            if (GUILayout.Button(string.Format("Toggle Gaze {0}", gazeMode)))
            {
                ToggleGazeMode();
            }


            if (GUILayout.Button(string.Format("Turn Idles {0}", m_idleMode ? "Off" : "On")))
            {
                m_idleMode = !m_idleMode;

                string onoff = m_idleMode ? "true" : "false";
                VHMsgManager.Get().SendVHMsg(string.Format(@"nvbg_set_option {0} saliency_glance {1}", "Brad", onoff));
                VHMsgManager.Get().SendVHMsg(string.Format(@"nvbg_set_option {0} saliency_glance {1}", "Rachel", onoff));
                VHMsgManager.Get().SendVHMsg(string.Format(@"nvbg_set_option {0} saliency_idle_gaze {1}", "Brad", onoff));
                VHMsgManager.Get().SendVHMsg(string.Format(@"nvbg_set_option {0} saliency_idle_gaze {1}", "Rachel", onoff));
            }

            if (GUILayout.Button("Reset", GUILayout.Height(buttonH)))
            {
                m_SBM.SBTransform("Brad", m_chrBradStartPos, m_chrBradStartRot);
                m_SBM.SBTransform("Rachel", m_chrRachelStartPos, m_chrRachelStartRot);
            }

            if (VHUtils.SceneManagerActiveSceneName() == "Campus")
            {
                GUILayout.Label("Game Mode");
                GameMode prevMode = m_GameMode;
                m_GameMode = (GameMode)GUILayout.Toolbar((int)m_GameMode, GameModeNames);
                if (m_GameMode != prevMode)
                {
                    SwitchGameMode(m_GameMode, prevMode);
                }

                if (m_GameMode == GameMode.Character)
                {
                    int prevChar = m_SelectedCharacter;
                    m_SelectedCharacter = GUILayout.Toolbar(m_SelectedCharacter, characterNames);
                    if (prevChar != m_SelectedCharacter)
                    {
                        UnSelectCharacter(prevChar);
                        SelectCharacter(m_SelectedCharacter);
                    }
                }
            }
            

            GUILayout.EndVertical();

            GUILayout.EndArea();

            Time.timeScale = m_timeSlider;
        }

        if (m_walkToMode)
        {
            Vector3 screenPoint = m_camera.gameObject.GetComponent<Camera>().WorldToScreenPoint(m_walkToPoint);

            GUI.color = new Color(1, 0, 0, 1);
            float boxH = 10;
            float boxW = 10;
            Rect r = new Rect(screenPoint.x - (boxW / 2), (m_camera.gameObject.GetComponent<Camera>().pixelHeight - screenPoint.y) - (boxH / 2), boxW, boxH);
            GUI.DrawTexture(r, m_whiteTexture);
            GUI.color = Color.white;
        }
    }

    void SwitchGameMode(GameMode newMode, GameMode oldMode)
    {
        m_GameMode = newMode;

        switch (oldMode)
        {
            case GameMode.FreeLook:
                m_camera.gameObject.SetActive(false);
                break;

            case GameMode.Character:
                UnSelectCharacters();
                break;
        }

        switch (newMode)
        {
            case GameMode.FreeLook:
                m_camera.gameObject.SetActive(true);
                break;

            case GameMode.Character:
                SelectCharacter(m_SelectedCharacter);
                break;
        }
    }

    //void DisableCharacters()
    //{
    //    UnSelectCharacters();
    //    for (int i = 0; i < m_Characters.Length; i++)
    //    {
    //        m_Characters[i].GetComponentInChildren<SBCharacterController>().enabled = false;
    //    }
    //}

    void UnSelectCharacters()
    {
        for (int i = 0; i < m_Characters.Length; i++)
        {
            //m_Characters[i].GetComponentInChildren<SBCharacterController>().enabled = false;
            VHUtils.FindChild(m_Characters[i].gameObject, "Camera").GetComponent<Camera>().enabled = false;
            //m_Characters[i].GetComponent<Camera>().enabled = false;
        }
    }

    void UnSelectCharacter(int selection)
    {
        m_Characters[selection].GetComponentInChildren<SBCharacterController>().enabled = false;
        VHUtils.FindChild(m_Characters[selection].gameObject, "Camera").GetComponent<Camera>().enabled = false;
    }

    void SelectCharacter(int selection)
    {
        UnSelectCharacters();

        m_SelectedCharacter = selection;

        SelectedCharacterController.enabled = true;
        VHUtils.FindChild(SelectedCharacter.gameObject, "Camera").GetComponent<Camera>().enabled = true;
        //SelectedCharacter.GetComponentInChildren<Camera>().enabled = true;
    }

    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }

    void VHMsg_MessageEvent(object sender, VHMsgBase.Message message)
    {
        if (m_displayVhmsgLog)
        {
            Debug.Log("VHMsg recvd: " + message.s);
        }

        string [] splitargs = message.s.Split( " ".ToCharArray() );

        if (splitargs.Length > 0)
        {
            if (splitargs[0] == "vrAllCall")
            {
                vhmsg.SendVHMsg("vrComponent renderer");
            }
            else if (splitargs[0] == "vrKillComponent")
            {
                if (splitargs.Length > 1)
                {
                    if (splitargs[1] == "renderer" || splitargs[1] == "all")
                    {
                        if (Application.isEditor)
                        {
#if UNITY_EDITOR
                            UnityEditor.EditorApplication.ExecuteMenuItem( "Edit/Play" );
#endif
                        }
                        else
                        {
                            Application.Quit();
                        }
                    }
                }
            }
            else if (splitargs[0] == "PlaySound")
            {
                string path = splitargs[1].Trim('"');   // PlaySound has double quotes around the sound file.  remove them before continuing.
                path = Path.GetFullPath(path);
                path = path.Replace("\\", "/");
                path = "file://" + path;
                WWW www = new WWW(path);
                VHUtils.PlayWWWSound(this, www, m_SBM.GetCharacterVoice(splitargs[2]), false);
            }
            else if (splitargs[0] == "StopSound")
            {
                //NOTE
                // currently stopping all characters on stopsound
                // needs to be changed to only affect the character in question                
                string[] charNames = m_SBM.GetSBMCharacterNames();
                for (int i = 0; i < charNames.Length; ++i)
                {
                    m_SBM.GetCharacterVoice(charNames[i]).Stop();
                }
            }
            else if (splitargs[0] == "vrExpress")
            {
                m_bFinishedPreviousUtterance = false;
            }
            else if (splitargs[0] == "vrSpoke")
            {
                m_bFinishedPreviousUtterance = true;
                HandlevrSpokeMessage();
            }
            else if (splitargs[0] == "CommAPI")
            {
                // CommAPI setcameraposition <x> <y> <z>
                // CommAPI setcamerarotation <x> <y> <z>

                if (splitargs.Length >= 1)
                {
                    if (splitargs[1] == "setcameraposition")
                    {
                        if (splitargs.Length >= 5)
                        {
                            Vector3 position = VHMath.ConvertStringsToVector(splitargs[2], splitargs[3], splitargs[4]);
                            m_camera.transform.position = position;
                        }
                    }
                    else if (splitargs[1] == "setcamerarotation")
                    {
                        if (splitargs.Length >= 5)
                        {
                            // x,y,z = Orientation in degrees.  (default coord system would match x,y,z to r,h,p
                            Vector3 rotation = VHMath.ConvertStringsToVector(splitargs[2], splitargs[3], splitargs[4]);
                            m_camera.transform.localRotation = Quaternion.Euler(rotation);
                        }
                    }
                }
            }
            else if (splitargs[0] == "renderer")
            {
                if (splitargs.Length >= 1)
                {
                    // "renderer log testing testing"
                    // "renderer console show_tips 1"

                    string function = splitargs[1].ToLower();
                    string[] rendererSplitArgs = new string[splitargs.Length - 2];
                    Array.Copy(splitargs, 2, rendererSplitArgs, 0, splitargs.Length - 2);

                    gameObject.SendMessage(function, rendererSplitArgs);

                }
            }
            else if (splitargs[0] == "sbm")
            {
                ////HACK HACK HACK HACK TO BE REMOVED
                ////HACK HACK HACK HACK TO BE REMOVED
                ////HACK HACK HACK HACK TO BE REMOVED
                if (splitargs.Length > 1)
                {
                    if (splitargs[1].Equals("vrSpoke"))
                    {
                        if (VHUtils.SceneManagerActiveSceneName() == "Campus")
                        {
                            if (!m_IntroCutscene.HasStartedPlaying)
                            {
                                m_subtitleText = "";
                                m_userDialogText = "";
                            }
                        }
                        else
                        {
                            m_subtitleText = "";
                            m_userDialogText = "";
                        }
                    }
                }
            }
            else if (splitargs[0].Equals("renderer_record"))
            {
            }
            else if (splitargs[0].Equals("render_text_overlay"))
            {
                if (splitargs.Length >= 1)
                {
                    if (splitargs[1].Equals("disable"))
                    {
                        DisplaySubtitles = false;
                        DisplayUserDialog = false;
                    }
                    if (splitargs[1].Equals("enable"))
                    {
                        m_subtitleText = "";
                        m_userDialogText = "";
                        DisplaySubtitles = true;
                        DisplayUserDialog = true;
                    }
                }
            }
            else if (splitargs[0].Equals("renderer_gui"))
            {
                if (splitargs.Length >= 1)
                {
                    var speechBox = GameObject.Find("SpeechBox");

                    if (splitargs[1].Equals("True"))
                    {
                        m_AcquireSpeechState = AcquireSpeechState.Off;
                        if (speechBox)
                            speechBox.GetComponent<SpeechBox>().Show = true;
                    }
                    else
                    {
                        if (speechBox)
                            speechBox.GetComponent<SpeechBox>().Show = false;

                        m_AcquireSpeechState = AcquireSpeechState.Disabled;
                    }
                }
            }


            else if (splitargs[0].Equals("vht_get_characters"))
            {
                string[] retval = m_SBM.GetSBMCharacterNames();
                string charNames = "";
                for (int i = 0; i < retval.Length; ++i)
                {
                    charNames += retval[i] + " ";
                }

                vhmsg.SendVHMsg("VHBuilder character_names " + charNames);
            }
        }
    }

    void HandlevrSpokeMessage()
    {
        m_userDialogText = "";

        if (!m_bIntroSequencePlaying)
        {
            m_subtitleText = "";
        }
    }

    void OnCharacterCreate(UnitySmartbodyCharacter character)
    {
        Debug.Log(string.Format("Character '{0}' created", character.SBMCharacterName));
    }

    void OnCharacterDelete(UnitySmartbodyCharacter character)
    {
        Debug.Log(string.Format("Character '{0}' deleted", character.SBMCharacterName));
    }

    void ProcessCommandLineAndConfigSettings()
    {
        m_SeqFile = m_ConfigFile.GetSetting("general", "DefaultSeqFile");
        if (!string.IsNullOrEmpty(m_SeqFile))
            Debug.Log("m_SeqFile: " + m_SeqFile);

        m_PyFile = m_ConfigFile.GetSetting("general", "DefaultPyFile");
        if (!string.IsNullOrEmpty(m_PyFile))
            Debug.Log("m_PyFile: " + m_PyFile);

        if (m_ConfigFile.SettingExists("general", "CameraMoveSpeed"))
        {
            m_camera.movementSpeed = float.Parse(m_ConfigFile.GetSetting("general", "CameraMoveSpeed"));
        }
        if (m_ConfigFile.SettingExists("general", "CameraRotateSpeed"))
        {
            m_camera.sensitivityX = m_camera.sensitivityY = float.Parse(m_ConfigFile.GetSetting("general", "CameraRotateSpeed"));
        }
        if (m_ConfigFile.SettingExists("general", "CameraSecondaryMoveSpeed"))
        {
            m_camera.secondaryMovementSpeed = float.Parse(m_ConfigFile.GetSetting("general", "CameraSecondaryMoveSpeed"));
        }
        if (m_ConfigFile.SettingExists("general", "CameraFrustumNear"))
        {
            m_camera.GetComponent<Camera>().nearClipPlane = float.Parse(m_ConfigFile.GetSetting("general", "CameraFrustumNear"));
        }
        if (m_ConfigFile.SettingExists("general", "CameraFrustumFar"))
        {
            m_camera.GetComponent<Camera>().farClipPlane = float.Parse(m_ConfigFile.GetSetting("general", "CameraFrustumFar"));
        }
        
        m_bStartInAcquireSpeechMode = bool.Parse(m_ConfigFile.GetSetting("general", "StartInAcquireSpeechMode"));
        m_AcquireSpeechState = m_bStartInAcquireSpeechMode ? AcquireSpeechState.On : AcquireSpeechState.Off;

        // setup resolution
        // resolution 640 x 480
        string resolution = VHUtils.GetCommandLineArgumentValue("resolution");
        string fullscreen = VHUtils.GetCommandLineArgumentValue("fullscreen");

        bool full = false;
        bool.TryParse(fullscreen, out full);
        Screen.fullScreen = full;

        if (!string.IsNullOrEmpty(resolution))
        {
            int screenWidth = Screen.width;
            int screenHeight = Screen.height;
            string[] widthHeightStrings = resolution.Split('x');
            if (widthHeightStrings.Length == 2 && int.TryParse(widthHeightStrings[0], out screenWidth)
                && int.TryParse(widthHeightStrings[1], out screenHeight))
            {
                SetResolution(screenWidth, screenHeight, Screen.fullScreen);
            }
        }
    }

    protected void log( string [] args )
    {
        if (args.Length > 0)
        {
            string argsString = String.Join(" ", args);
            Debug.Log(argsString);
        }
    }

    protected void console( string [] args )
    {
        if (args.Length > 0)
        {
            string argsString = String.Join(" ", args);
            HandleConsoleMessage(argsString, m_Console);
        }
    }

    protected void color(string[] args)
    {
        if (args.Length > 2)
        {
            //Debug.LogError(args[0] + args[1] + args[2]);
            int r = Convert.ToInt32(args[0]);
            int g = Convert.ToInt32(args[1]);
            int b = Convert.ToInt32(args[2]);
            
            m_currentColor = new Color(((float)r/255), ((float)g/255), ((float)b/255));
            if (VHUtils.SceneManagerActiveSceneName() == "Customizer")
            {
                //m_currentColor = GameObject.Find("ColorPicker").GetComponent<ColorPicker>().setColor;
                //GameObject.Find("Background").renderer.material.color = m_currentColor;

                GameObject.Find("ColorPicker").GetComponent<ColorPicker>().setColor = m_currentColor;
            }

             //GameObject.Find("Background").renderer.material.color = m_currentColor;            
        }
    }




    protected void customizer(string[] args)
    {
        if (args.Length > 4)
        {
            string characterName = args[0];
            string displayName = args[1];

            int r = Convert.ToInt32(args[2]);
            int g = Convert.ToInt32(args[3]);
            int b = Convert.ToInt32(args[4]);

            UnitySmartbodyCharacter sbChar = m_SBM.GetCharacterByName(characterName);
            if (sbChar != null)
            {
                MaterialCustomizer matCustomizer = sbChar.GetComponent<MaterialCustomizer>();
                if (matCustomizer != null)
                {
                    matCustomizer.SetColor(displayName, new Color(((float)r / 255), ((float)g / 255), ((float)b / 255)));
                }
            }
            m_currentColor = new Color(((float)r / 255), ((float)g / 255), ((float)b / 255));

        }
        else if (args.Length > 1)
        {
            string characterName = args[0];
            float value = float.Parse(args[1]);

            UnitySmartbodyCharacter sbChar = m_SBM.GetCharacterByName(characterName);
            if (sbChar != null)
            {
                MaterialCustomizer matCustomizer = sbChar.GetComponent<MaterialCustomizer>();
                if (matCustomizer != null)
                {
                    matCustomizer.SetFloat("Skin", 1 - value);
                }
            }
        }
    }



    protected void background( string [] args )
    {
        if (args.Length > 0)
        {
            if (args[0] == "file")
            {
                // renderer background file background.png

                if (args.Length > 1)
                {                             
                    string background = "";

                    int i = 0;
                    for (i = 1; i < args.Length -1; ++i)
                    {
                        background += args[i] + " ";
                    }

                    background += args[i];                                         

                    string path = "Backgrounds/" + background;

                    VHFile.LoadStreamingAssetsAsync(path);

                    WWW www = VHFile.LoadStreamingAssetsAsync(path);
                    SwapTexture(this, www);
                }
            }
        }
    }



    protected void codec(string[] args)
    {
        if (args.Length > 0)
        {
            string codec = "";

            int i = 0;
            for (i = 0; i < args.Length -1; ++i)
            {
                codec += args[i] + " ";
            }

            codec += args[i];


            Debug.Log(codec);
            SetVideoCodec(codec);
        }
    }


    public void SetVideoCodec(string _codecName)
    {
    }


    IEnumerator GazeAtCamera()
    {
        yield return new WaitForSeconds(0.3f);
        //SmartbodyManager.Get().SBGaze("*", cameraChoicesStrings[m_cameraSelectCurrent], 500);
        string message = string.Format(@"sbm bml char * <gaze target=""{0}"" sbm:joint-range=""HEAD EYES NECK"" sbm:joint-speed=""{1}""/>", cameraChoicesStrings[m_cameraSelectCurrent], 500);
        vhmsg.SendVHMsg(message);
    }


    protected void setcamera( string [] args )
    {
        if (args.Length > 0)
        {
            if (args[0] == "set")
            {
                // renderer setcamera set Camera2

                if (args.Length > 1)
                {
                    string camera = args[1];

                    cameraChoices[m_cameraSelectCurrent].gameObject.SetActive(false);

                    for (int i = 0; i < cameraChoices.Length; i++)
                    {
                        if (cameraChoices[i].name == camera)
                        {
                            m_cameraSelectCurrent = i;
                            break;
                        }
                    }

                    cameraChoices[m_cameraSelectCurrent].gameObject.SetActive(true);
                    if (m_forceGazeOnSetCamera)
                        StartCoroutine(GazeAtCamera());
                }
            }
            else if (args[0] == "force_gaze")
            {
                if (args.Length > 1)
                {
                    if (Convert.ToBoolean(args[1]))
                    {
                        m_forceGazeOnSetCamera = true;
                    }
                    else
                    {
                        m_forceGazeOnSetCamera = false;
                    }
                }
            }
        }
    }

    protected override void HandleConsoleMessage(string commandEntered, DebugConsole console)
    {
        base.HandleConsoleMessage(commandEntered, console);

        Vector2 vec2Data = Vector2.zero;
        if (commandEntered.IndexOf("vhmsg") != -1)
        {
            string opCode = string.Empty;
            string args = string.Empty;
            if (console.ParseVHMSG(commandEntered, ref opCode, ref args))
            {
                vhmsg.SendVHMsg(opCode, args);
            }
            else
            {
                console.AddText(commandEntered + " requires an opcode string and can have an optional argument string");
            }
        }
        else if (commandEntered.IndexOf("set_loco_char_name") != -1)
        {
            //m_locoCharacterName = commandEntered.Replace("set_loco_char_name", "");
        }
        else if (commandEntered.IndexOf("set_resolution") != -1)
        {
            if (console.ParseVector2(commandEntered, ref vec2Data))
            {
                SetResolution((int)vec2Data.x, (int)vec2Data.y, Screen.fullScreen);
            }
        }
        else if (commandEntered.IndexOf("play_intro") != -1)
        {
            StopAllCoroutines();
            m_IntroCutscene.Play();
            m_bIntroSequencePlaying = true;
        }
    }

    void SetResolution(int width, int height, bool fullScreen)
    {
        Screen.SetResolution(width, height, fullScreen);
    }

    string ParseSpeechText(string text)
    {
       int endOfSpeechIndex = text.IndexOf("</speech>");
       if (endOfSpeechIndex == -1)
       {
          // there is no speech text
          return null;
       }

       int startOfSpeechIndex = text.LastIndexOf('>', endOfSpeechIndex);
       if (startOfSpeechIndex == -1)
       {
          // broken xml tags
          return null;
       }

       return text.Substring(startOfSpeechIndex + 1, endOfSpeechIndex - startOfSpeechIndex - 1);
    }


    void MoveCharacter(string character, string direction, float fSpeed, float fLrps, float fFadeOutTime)
    {
        string command = string.Format("sbm test loco char {0} {1} spd {2} rps {3} time {4}",
            character, direction, fSpeed, fLrps, fFadeOutTime);
        vhmsg.SendVHMsg(command);
    }

    void IntroSequenceSetup()
    {
        m_bIntroSequencePlaying = true;
        m_SpeechBox.enabled = false;
        m_bLocomotionEnabled = false;
        m_bFinishedPreviousUtterance = true;

        if (InAcquireSpeechMode)
        {
            vhmsg.SendVHMsg("acquireSpeech stopSession");
        }
        else
        {
            // get acquire speech to the recorder tab with start, but then disable it because we don't want to interrupt the intro
            vhmsg.SendVHMsg("acquireSpeech startSession");
            vhmsg.SendVHMsg("acquireSpeech stopSession");
        }

        m_AcquireSpeechState = AcquireSpeechState.Disabled;
    }

    void CleanupIntroSequence()
    {
        m_subtitleText = "";
        m_bIntroSequencePlaying = false;
        m_SpeechBox.enabled = true;

        m_bLocomotionEnabled = true;

        SetAcquireSpeechState(m_bStartInAcquireSpeechMode ? AcquireSpeechState.On : AcquireSpeechState.Off);
    }

    void SetAcquireSpeechState(AcquireSpeechState state)
    {
        m_AcquireSpeechState = state;
        vhmsg.SendVHMsg("acquireSpeech " + (m_AcquireSpeechState == AcquireSpeechState.On ? "startSession" : "stopSession"));
        //sbm.DisplayUserDialog = m_AcquireSpeechState == AcquireSpeechState.On;
        DisplayUserDialog = m_AcquireSpeechState == AcquireSpeechState.On;
    }

    public void ToggleAxisLines()
    {
        GameObject axisLines = GameObject.Find("AxisLines");
        if (axisLines)
        {
            if (axisLines.transform.childCount > 0)
            {
                Transform[] allChildren = axisLines.GetComponentsInChildren<Transform>(true);

                if (axisLines.transform.GetChild(0).gameObject.activeSelf)
                {
                    foreach (Transform t in allChildren)
                    {
                        if (t == axisLines.transform)
                            continue;

                        t.gameObject.SetActive(false);
                    }
                }
                else
                {
                    foreach (Transform t in allChildren)
                    {
                        if (t == axisLines.transform)
                            continue;

                        t.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    IEnumerator WaitForPreviousUtteranceToFinish()
    {
        while (!m_bFinishedPreviousUtterance)
        {
            yield return new WaitForEndOfFrame();
        }

        // reset the variable and add a bit of a delay so that he doesn't keep talking without pausing
        m_bFinishedPreviousUtterance = false;
        yield return new WaitForSeconds(1.0f);
    }

    void MakeBradTalk(string charName, string externalSoundId, string text)
    {
        vhmsg.SendVHMsg(String.Format("vrExpress {1} user 1303332588320-{0}-1 <?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>"
            + "<act><participant id=\"{1}\" role=\"actor\" /><fml><turn start=\"take\" end=\"give\" /><affect type=\"neutral\" "
            + "target=\"addressee\"></affect><culture type=\"neutral\"></culture><personality type=\"neutral\"></personality></fml>"
            + "<bml><speech id=\"sp1\" ref=\"{2}\" type=\"application/ssml+xml\">{3}</speech></bml></act>", m_BradTalkId, charName, externalSoundId, text));
        m_BradTalkId += 3;
    }

    void MobilePlayAudio(string audioFile)
    {
        // Play the audio directly because VHMsg isn't enabled on mobile.  So, we can't receive the PlaySound message

        if (Application.platform == RuntimePlatform.Android ||
            Application.platform == RuntimePlatform.IPhonePlayer)
        {
            string s = "Sounds/" + audioFile + ".wav";
            var www = VHFile.LoadStreamingAssetsAsync(s);
            VHUtils.PlayWWWSound(this, www, m_SBM.GetCharacterVoice("Brad"), false);
        }
    }

    void UpdateBrownFace()
    {
        for (int i = 0; i < m_BrownHeads.Count; i++)
        {
            //Debug.Log("UpdateBrownFace() - " + SmartbodyManager.Get().SBGetAuValue("ChrBrownRocPrefab", "au_45_left"));

            /*
            SetGLShaderParam(m_BrownHeads[i], 1,  SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_45_left"));
            SetGLShaderParam(m_BrownHeads[i], 2,  SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_45_right"));
            SetGLShaderParam(m_BrownHeads[i], 3,  SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_2_left"));
            SetGLShaderParam(m_BrownHeads[i], 4,  SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_2_right"));
            SetGLShaderParam(m_BrownHeads[i], 5,  SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_100_left"));
            SetGLShaderParam(m_BrownHeads[i], 6,  SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_100_right"));
            SetGLShaderParam(m_BrownHeads[i], 7,  SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_110_left"));
            SetGLShaderParam(m_BrownHeads[i], 8,  SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_110_right"));
            SetGLShaderParam(m_BrownHeads[i], 9,  SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_120_left"));
            SetGLShaderParam(m_BrownHeads[i], 10, SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_120_right"));
            SetGLShaderParam(m_BrownHeads[i], 11, SBHelpers.SBGetAuValue("ChrBrownRocPrefab", "au_50"));
            */
        }
    }

    private void SetGLShaderParam(GameObject obj, int shaderNum, float weight)
    {
        //Debug.Log("weight: " + weight);
        //GameObject roc = m_sbm.GetCharacterBySBMName("ChrBrownRoc").gameObject;
        //GameObject head = Utils.FindChild(roc, "CharacterRoot/Mesh/SkinnedMesh/MshRef/Head");

        // set all to 0
        /*head.renderer.material.SetFloat("_Weight1",  0);
        head.renderer.material.SetFloat("_Weight2",  0);
        head.renderer.material.SetFloat("_Weight3",  0);
        head.renderer.material.SetFloat("_Weight4",  0);
        head.renderer.material.SetFloat("_Weight5",  0);
        head.renderer.material.SetFloat("_Weight6",  0);
        head.renderer.material.SetFloat("_Weight7",  0);
        head.renderer.material.SetFloat("_Weight8",  0);
        head.renderer.material.SetFloat("_Weight9",  0);
        head.renderer.material.SetFloat("_Weight10", 0);
        head.renderer.material.SetFloat("_Weight11", 0);
        head.renderer.material.SetFloat("_Weight12", 0);*/

        // set selected to value
        //if (shaderNum != 0)
        {
            string shaderName = string.Format("_Weight{0}", shaderNum);
            obj.GetComponent<Renderer>().material.SetFloat(shaderName, weight);
        }
    }

    private void ChrBrownRocChannelCallback(UnitySmartbodyCharacter character, string channelName, float value)
    {
        int shaderNum = -1;

        if (channelName == "au_45_left")
            shaderNum = 1;
        else if (channelName == "au_45_right")
            shaderNum = 2;
        else if (channelName == "au_2_left")
            shaderNum = 3;
        else if (channelName == "au_2_right")
            shaderNum = 4;
        else if (channelName == "au_100_left")
            shaderNum = 5;
        else if (channelName == "au_100_right")
            shaderNum = 6;
        else if (channelName == "au_110_left")
            shaderNum = 7;
        else if (channelName == "au_110_right")
            shaderNum = 8;
        else if (channelName == "au_120_left")
            shaderNum = 9;
        else if (channelName == "au_120_right")
            shaderNum = 10;
        else if (channelName == "au_50")
            shaderNum = 11;

        if (shaderNum != -1)
        {
            for (int i = 0; i < m_BrownHeads.Count; i++)
            {
                //Debug.Log("ChrBrownRocChannelCallback() - " + character.SBMCharacterName + " " + channelName + " " + value);

                SetGLShaderParam(m_BrownHeads[i], shaderNum, value);
            }
        }
    }

    public void ChangeSlide(string slideName)
    {
        Texture2D foundSlide = null;

        foreach (Texture2D slide in m_slides)
        {
            if (slide.name == slideName)
                foundSlide = slide;
        }

        if (foundSlide)
            SlidesScreen.GetComponent<Renderer>().material.mainTexture = foundSlide;
    }
}
