using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;

public class Main : VHMain
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
        TrackGaze,
        TrackAddressee
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
    public Texture2D m_whiteTexture;
    public Cutscene m_IntroCutscene;
    Color m_currentColor;
    public GameObject m_LoadingScreenWhiteBg;
    public float m_DelayTimeAfterCutsceneFinishes = 2;
    public UnitySmartbodyCharacter[] m_Characters;

    // oculus variables
    public GameObject oculusNormalCamera;
    public GameObject oculusCamera;
    public GameObject oculusCharacterController;
    public GameObject oculusCameraCam;
    public GameObject oculusCharacterControllerCam;

    public MaterialCustomizer[] m_MaterialCustomizers;
    private bool m_forceGazeOnSetCamera = false;

    Vector3 m_StartingCameraPosition;
    Quaternion m_StartingCameraRotation;

    enum ControllerMenus             { NOMENU,       SCENE,   SMARTBODY,   MOTION,   LINEUP,   CUSTOMIZE,   VIDEO,   CONFIG,   LENGTH };
    string [] m_controllerMenuText = { "debug menu", "scene", "smartbody", "motion", "lineup", "customize", "Video", "config", };
    ControllerMenus m_controllerMenuSelected = ControllerMenus.NOMENU;

    AcquireSpeechState m_AcquireSpeechState = AcquireSpeechState.Disabled;
    PerceptionApplicationState m_PerceptionApplicationState = PerceptionApplicationState.Disabled;
    GameMode m_GameMode = GameMode.FreeLook;

    int m_BradTalkId = 128;  // for acquire speech

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

    string [] testUtteranceButtonText = { "1", "2", "Tts", "Tts2", "V2a", "V2b" };
    string[] GameModeNames;
    int testUtteranceSelected = 0;
    string [] testUtteranceCharacter = { "Brad", "Rachel", "Brad", "Rachel", "*", "*" };
    string [] testUtteranceName = { "brad_byte", "rachel_usc", "speech_womanTTS", "speech_womanTTS", "z_viseme_test2", "z_viseme_test3" };
    string [] testUtteranceText = { "", "", "If the system cannot find my regular voice, it defaults back to the Windows standard voice. Depending on your version of Windows that can be a womans voice. Dont I sound delightful?", "If the system cannot find my regular voice, it defaults back to the Windows standard voice. Depending on your version of Windows that can be a womans voice. Dont I sound delightful?", "", "" };  // the TTS text
    string [] testTtsVoices = { "Festival_voice_cmu_us_jmk_arctic_clunits", "Festival_voice_cmu_us_clb_arctic_clunits", "Festival_voice_rab_diphone", "Festival_voice_kal_diphone", "Festival_voice_ked_diphone", "Microsoft|Anna", "Microsoft|David|Desktop", "Microsoft|Zira|Desktop", "Cerevoice_star", "Cerevoice_katherine" };
    string[] perceptionButtonText = { "PerceptionApp OFF", "Track Head", "Track Gaze", "Track Addressee"};
    string[] sceneNames = { "Campus", "House", "LineUp", "Customizer", "CampusEmpty", "OculusRiftTest", "CampusTacQ", "CampusTAB" };
    string[] characterNames;
    int testTtsSelected = 0;
    int m_SelectedCharacter;

    int perceptionSelected = 0;

    Vector3    m_chrBradStartPos;
    Quaternion m_chrBradStartRot;
    Vector3    m_chrRachelStartPos;
    Quaternion m_chrRachelStartRot;

    int m_gazingMode = 1;  // 0 - off, 1 - gaze camera, 2 - gaze mouse cursor
    bool m_idleMode = true;

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

    // lineup characters
    string [] m_lineupCharacters = { "Brad", "Rachel", "Harmony", "JustinIct", "Pedro", "Rio", "Utah", "Vincent", "Alexis", "Carl", "Joan", "Justin", "Mia", "Monster", "Soldier", "Swat", "Zombie", "Cabrillo", "ConnorNavy", "DavisArmy", "Foster", "Garza", "Miles" };
    bool [] m_lineupCharacterFlags;  // whether or not they are spawned
    Vector3 [] m_lineupPositions;
    Quaternion [] m_lineupRotations;
    UnityEngine.Object [] m_lineupGameObjects;  // which position the character is spawned into
    Vector2 m_motionListScrollPosition;
    string m_motionsFilter = "";

    DebugOnScreenLog m_debugOnScreenLog;

    // customize debug menu
    int m_currentCharacterNameIndex = 0;
    int m_currentMaterialTypeIndex = 0;
    int m_currentMaterialIndex = 0;

    int m_motionPrefixesCurrentIndex = 0;

    UIEventsInGame m_uiEventsInGame;

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


    public override void Start()
    {
        Application.targetFrameRate = 60;
        base.Start();

        m_userDialogText = "";
        m_subtitleText = "";
        DisplaySubtitles = true;
        DisplayUserDialog = true;

        if (m_IntroCutscene != null)
        {
            m_IntroCutscene.AddOnFinishedCutsceneCallback(IntroCutsceneFinished);
            m_IntroCutscene.AddOnEventFiredCallback(IntroEventFired);
        }

        m_StartingCameraPosition = m_camera.transform.position;
        m_StartingCameraRotation = m_camera.transform.rotation;


        ProcessCommandLineAndConfigSettings();

        GameModeNames = Enum.GetNames(typeof(GameMode));


        if (VHUtils.SceneManagerActiveSceneName() == "Campus" || VHUtils.SceneManagerActiveSceneName() == "CampusOSX")
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

            if (m_Characters.Length > 0)
            {
                SelectCharacter(0);
            }
        }


        m_SBM = SmartbodyManager.Get();


        m_Console.AddCommandCallback("set_loco_char_name", new DebugConsole.ConsoleCallback(HandleConsoleMessage));
        m_Console.AddCommandCallback("play_intro", new DebugConsole.ConsoleCallback(HandleConsoleMessage));
        m_Console.AddCommandCallback("set_tips", new DebugConsole.ConsoleCallback(HandleConsoleMessage));
        m_Console.AddCommandCallback("show_tips", new DebugConsole.ConsoleCallback(HandleConsoleMessage));


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


        if (VHGlobals.m_showDebugInfo)
            GameObject.FindObjectOfType<DebugInfo>().NextMode();

        if (VHGlobals.m_showDebugConsole)
            if (!m_Console.DrawConsole) m_Console.ToggleConsole();

        m_debugOnScreenLog = GameObject.FindObjectOfType<DebugOnScreenLog>();

        if (VHGlobals.m_launchedFromLauncher)
        {
            if (m_debugOnScreenLog)
                m_debugOnScreenLog.gameObject.SetActive(false);
        }


#if UNITY_IPHONE || UNITY_ANDROID
        if (!m_Console.DrawConsole) m_Console.ToggleConsole();
        m_showController = true;
#endif


        GameObject canvasOnScreenDisplayPrefab = GameObject.Find("Canvas_OnScreenDisplayPrefab");
        m_uiEventsInGame = VHUtils.FindChildRecursive(canvasOnScreenDisplayPrefab, "UIEventsInGame").GetComponent<UIEventsInGame>();


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

            background("file Gradient01_UscShield.jpg".Split());
        }

        if (VHUtils.SceneManagerActiveSceneName() == "Campus" || VHUtils.SceneManagerActiveSceneName() == "CampusOSX")
        {
            UnSelectCharacters();
        }

        m_lineupCharacterFlags = new bool [m_lineupCharacters.Length];
        m_lineupPositions = new Vector3 [m_lineupCharacters.Length];
        m_lineupRotations = new Quaternion [m_lineupCharacters.Length];
        m_lineupGameObjects = new UnityEngine.Object [m_lineupCharacters.Length];

        for (int i = 0; i < m_lineupCharacters.Length; i++)
        {
            // look for spawn points to populate the positions, otherwise, put the points on a line
            GameObject spawnPoint = GameObject.Find("SpawnPoint" + i);
            if (spawnPoint)
            {
                m_lineupPositions[i] = spawnPoint.transform.position;
                m_lineupRotations[i] = spawnPoint.transform.rotation;
            }
            else
            {
                float distance = 0.8f;
                m_lineupPositions[i].x = distance * ((i + 1) / 2);
                m_lineupPositions[i].x *= (i % 2 == 0 ? 1 : -1);
            }
        }

        UnitySmartbodyCharacter[] characters = (UnitySmartbodyCharacter[])FindObjectsOfType(typeof(UnitySmartbodyCharacter));
        for (int i = 0; i < characters.Length; i++)
        {
            m_lineupGameObjects[i] = characters[i].gameObject;
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


    public void Update()
    {
        if (m_SBM)
        {
            Camera cameraComponent = m_camera.GetComponent<Camera>();
            m_SBM.m_camPos = m_camera.transform.position;
            m_SBM.m_camRot = m_camera.transform.rotation;
            m_SBM.m_camFovY = cameraComponent.fieldOfView;
            m_SBM.m_camAspect = cameraComponent.aspect;
            m_SBM.m_camZNear = cameraComponent.nearClipPlane;
            m_SBM.m_camZFar = cameraComponent.farClipPlane;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            m_uiEventsInGame.UITogglePauseMenu();
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

            // toggle camera b/w oculus and normal camera
            // for now only for OculusRiftTest Level
            if (Input.GetKeyDown(KeyCode.N))
            {
                if (VHUtils.SceneManagerActiveSceneName() == "OculusRiftTest")
                {
                    if (oculusNormalCamera.activeSelf)
                    {
                        oculusNormalCamera.SetActive(false);
                        oculusCamera.SetActive(true);
                        oculusCharacterController.SetActive(false);
                        if (m_gazingMode == 1)
                        {
                            m_SBM.SBGaze("*", oculusNormalCamera.gameObject.name);
                        }
                    }
                    else if (oculusCamera.activeSelf)
                    {
                        oculusNormalCamera.SetActive(false);
                        oculusCamera.SetActive(false);
                        oculusCharacterController.SetActive(true);
                        if (m_gazingMode == 1)
                        {
                            m_SBM.SBGaze("*", oculusCameraCam.gameObject.name);
                        }
                    }
                    else if (oculusCharacterController.activeSelf)
                    {
                        oculusCharacterController.SetActive(false);
                        oculusNormalCamera.SetActive(true);
                        oculusCamera.SetActive(false);
                        if (m_gazingMode == 1)
                        {
                            m_SBM.SBGaze("*", oculusCharacterControllerCam.gameObject.name);
                        }
                    }
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

            if (Input.GetKeyDown(KeyCode.X))  // reset camera position
            {
                m_camera.transform.position = m_StartingCameraPosition;
                m_camera.transform.rotation = m_StartingCameraRotation;
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                UpdatePerceptionAppState();
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                GameObject.FindObjectOfType<DebugInfo>().NextMode();
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                vhmsg.SendVHMsg("vrKillComponent", "ssi_vhmsger");
                vhmsg.SendVHMsg("vrKillComponent", "perception-test-application");
            }

            if (m_walkToMode && Input.GetMouseButtonDown(0))  // walk to mouse position
            {
                Ray ray = m_camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    Debug.Log("Walk to: " + -hit.point.x + " " + hit.point.z);
                    //SmartbodyManager.Get().SBWalkTo("*", string.Format("{0} {1}", -hit.point.x, hit.point.z), false);
                    SmartbodyManager.Get().SBWalkTo("Brad", string.Format("{0} {1}", -hit.point.x - 1, hit.point.z), false);
                    SmartbodyManager.Get().SBWalkTo("Rachel", string.Format("{0} {1}", -hit.point.x + 1, hit.point.z), false);
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

        if (m_uiEventsInGame.UIPauseMenuIsOn())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (InAcquireSpeechMode || m_camera.CameraRotationOn)
        {
            // lock the screen cursor if they are looking around or using their mic
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (VHUtils.SceneManagerActiveSceneName() == "Customizer")
        {
            m_currentColor = GameObject.Find("ColorPicker").GetComponent<ColorPicker>().setColor;
            //Debug.LogError(m_currentColor.r + " " + m_currentColor.g + " " + m_currentColor.b + " " + m_currentColor.a);
            GameObject.Find("screen").GetComponent<Renderer>().material.color = m_currentColor;
        }


        switch (m_AcquireSpeechState)
        {
            case AcquireSpeechState.Disabled:
            case AcquireSpeechState.Off:
                {
                    if (!m_uiEventsInGame.UIMicrophoneIsDisabled())
                    {
                        m_uiEventsInGame.UIMicrophoneSetDisabled();
                    }
                }
                break;

            case AcquireSpeechState.On:
                {
                    if (!m_uiEventsInGame.UIMicrophoneIsOn())
                    {
                        m_uiEventsInGame.UIMicrophoneSetOn();
                    }
                }
                break;

            case AcquireSpeechState.InUse:
                {
                    if (!m_uiEventsInGame.UIMicrophoneIsInUse())
                    {
                        m_uiEventsInGame.UIMicrophoneSetInUse();
                    }
                }
                break;
        }
    }


    public override void OnGUI()
    {
        if (m_disableGUI)
            return;

        base.OnGUI();

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
            float spaceHeight = 20;

            if (m_controllerMenuSelected == ControllerMenus.MOTION)
                buttonW = 280;

            GUILayout.BeginArea(new Rect(buttonX, buttonY, buttonW, Screen.height));

            GUILayout.BeginVertical();


            GUILayout.BeginHorizontal();

            if (GUILayout.Button("<", GUILayout.Height(buttonH)))
            {
                if (m_controllerMenuSelected == 0)
                    m_controllerMenuSelected = (ControllerMenus)(m_controllerMenuText.Length - 1);
                else
                    m_controllerMenuSelected--;
            }

            if (GUILayout.Button(m_controllerMenuText[(int)m_controllerMenuSelected], GUILayout.Height(buttonH)))
            {
                m_controllerMenuSelected++;
                m_controllerMenuSelected = (ControllerMenus)((int)m_controllerMenuSelected % m_controllerMenuText.Length);
            }

            if (GUILayout.Button(">", GUILayout.Height(buttonH)))
            {
                m_controllerMenuSelected++;
                m_controllerMenuSelected = (ControllerMenus)((int)m_controllerMenuSelected % m_controllerMenuText.Length);
            }

            GUILayout.EndHorizontal();


            GUILayout.Space(spaceHeight);

            if (m_controllerMenuSelected == ControllerMenus.NOMENU)
            {
            }
            else if (m_controllerMenuSelected == ControllerMenus.SCENE)
            {
                for (int i = 0; i < sceneNames.Length; i++)
                {
                    if (VHUtils.SceneManagerActiveSceneName() == sceneNames[i])
                        continue;

                    if (GUILayout.Button("Load " + sceneNames[i])) { VHUtils.SceneManagerLoadScene(sceneNames[i]); }
                }

                GUILayout.Space(spaceHeight);

                if (VHUtils.SceneManagerActiveSceneName() == "Campus" || VHUtils.SceneManagerActiveSceneName() == "CampusOSX")
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
            }
            else if (m_controllerMenuSelected == ControllerMenus.SMARTBODY)
            {
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

                if (GUILayout.Button(perceptionButtonText[perceptionSelected]))
                {
                    UpdatePerceptionAppState();
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
            }
            else if (m_controllerMenuSelected == ControllerMenus.MOTION)
            {
                List<string> motions = m_SBM.GetLoadedMotions();

                List<string> prefixes = new List<string>();
                prefixes.Add("All");

                for (int i = 0; i < motions.Count; i++)
                {
                    string motion = motions[i];

                    int underscore = motion.IndexOf('_');

                    if (underscore < 0)
                        continue;

                    int atsign = motion.IndexOf('@');

                    if (atsign < 0 ||
                        atsign + 1 >= motion.Length)
                        continue;

                    if (Char.IsDigit(motion[atsign + 1]))
                        continue;   // assume this is a facial action unit

                    if (motion.Contains("face_neutral"))
                        continue;   // ignore the face_neutral pose that matches this pattern

                    string motionPrefix = motion.Remove(underscore);
                    if (!prefixes.Contains(motionPrefix))
                    {
                        prefixes.Add(motionPrefix);
                    }
                }

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("<", GUILayout.Height(buttonH)))
                {
                    m_motionPrefixesCurrentIndex = VHMath.DecrementWithRollover(m_motionPrefixesCurrentIndex, prefixes.Count);
                }

                if (GUILayout.Button(prefixes[m_motionPrefixesCurrentIndex], GUILayout.Height(buttonH)))
                {
                    m_motionPrefixesCurrentIndex = VHMath.IncrementWithRollover(m_motionPrefixesCurrentIndex, prefixes.Count);
                }

                if (GUILayout.Button(">", GUILayout.Height(buttonH)))
                {
                    m_motionPrefixesCurrentIndex = VHMath.IncrementWithRollover(m_motionPrefixesCurrentIndex, prefixes.Count);
                }

                GUILayout.EndHorizontal();


                m_motionsFilter = GUILayout.TextField(m_motionsFilter);

                m_motionListScrollPosition = GUILayout.BeginScrollView(m_motionListScrollPosition);

                for (int i = 0; i < motions.Count; i++)
                {
                    string motion = motions[i];

                    if (prefixes[m_motionPrefixesCurrentIndex] != "All" && !motion.Contains(prefixes[m_motionPrefixesCurrentIndex]))
                        continue;

                    if (string.IsNullOrEmpty(m_motionsFilter) || motion.ToLower().Contains(m_motionsFilter.ToLower()))
                    {
                        TextAnchor prev = GUI.skin.button.alignment;
                        GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                        if (GUILayout.Button(motion, GUILayout.Height(buttonH)))
                        {
                            m_SBM.SBPlayAnim("*", motion);
                        }
                        GUI.skin.button.alignment = prev;
                    }
                }

                GUILayout.EndScrollView();

#if false
                m_spsMotionsScrollView = GUILayout.BeginScrollView(m_spsMotionsScrollView);

                foreach (string button in m_spsMotions)
                {
                    if (!button.Contains(m_spsMotionPrefixes[m_spsMotionsCurrent]))
                        continue;

                    if (GUILayout.Button(button))
                    {
                        m_sbm.SBPlayAnim("ChrJavierPrefab", button);
                    }
                }

                GUILayout.EndScrollView();
#endif
            }
            else if (m_controllerMenuSelected == ControllerMenus.LINEUP)
            {
                for (int i = 0; i < m_lineupCharacters.Length; i++)
                {
                    bool newLineupToggle = GUILayout.Toggle(m_lineupCharacterFlags[i], m_lineupCharacters[i], "Button");
                    if (newLineupToggle != m_lineupCharacterFlags[i])
                    {
                        LineupToggleCharacter(i);
                    }
                }

                GUILayout.Space(spaceHeight);

                if (GUILayout.Button("All Characters"))
                {
                    for (int i = 0; i < m_lineupCharacters.Length; i++)
                    {
                        LineupToggleCharacter(i);
                    }
                }
            }
            else if (m_controllerMenuSelected == ControllerMenus.CUSTOMIZE)
            {
                /*
                - able to change solid color to any rgb value
                   - brad shirt, pants
                   - rachel shirt, jacket, pants

                - able to change to a substance plaid/design material
                  - adjust colors and sliders of different settings on the material.  limits set by us.
                  - color/slider presets to use as recommendations.  created by us
                  - brad shirt
                  - rachel shirt, jacket
                  - 2 or 3 different material choices each

                - hair color and skin color to any rgb value.  Method unchanged from previous customizer (overlaying the rgb color on existing texture).  Do we want to do more here?  Maybe create presets?

                ChrBradShirt_Plaid2_Mat
                ChrRachel_ShirtFlowered_Mat 
                ChrRachel_SweaterPattern_Mat (has 2 variations too)

                There are also three materials for Brad’s skin (head and hands):
                ChrBradHead_Mat
                ChrBradHeadPale_Mat
                ChrBradHeadTan_Mat
                */


                GUILayout.Label("Character:");

                string [] characterNames = m_SBM.GetSBMCharacterNames();
                if (m_currentCharacterNameIndex >= characterNames.Length)
                {
                    m_currentCharacterNameIndex = 0;
                }

                if (characterNames.Length > 0)
                {
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("<"))
                    {
                        if (m_currentCharacterNameIndex == 0)
                            m_currentCharacterNameIndex = characterNames.Length - 1;
                        else
                            m_currentCharacterNameIndex--;
                    }

                    if (GUILayout.Button(characterNames[m_currentCharacterNameIndex]))
                    {
                        m_currentCharacterNameIndex++;
                        m_currentCharacterNameIndex = m_currentCharacterNameIndex % characterNames.Length;
                    }

                    if (GUILayout.Button(">"))
                    {
                        m_currentCharacterNameIndex++;
                        m_currentCharacterNameIndex = m_currentCharacterNameIndex % characterNames.Length;
                    }

                    GUILayout.EndHorizontal();


                    UnitySmartbodyCharacter sbCharacter = m_SBM.GetCharacterByName(characterNames[m_currentCharacterNameIndex]);
                    MaterialCustomizer materialCustomizer = sbCharacter.GetComponent<MaterialCustomizer>();

                    if (m_currentMaterialTypeIndex >= materialCustomizer.m_materialDataV2.Count)
                    {
                        m_currentMaterialTypeIndex = 0;
                    }

                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("<"))
                    {
                        if (m_currentMaterialTypeIndex == 0)
                            m_currentMaterialTypeIndex = materialCustomizer.m_materialDataV2.Count - 1;
                        else
                            m_currentMaterialTypeIndex--;
                    }

                    if (GUILayout.Button(materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_targetName))
                    {
                        m_currentMaterialTypeIndex++;
                        m_currentMaterialTypeIndex = m_currentMaterialTypeIndex % materialCustomizer.m_materialDataV2.Count;
                    }

                    if (GUILayout.Button(">"))
                    {
                        m_currentMaterialTypeIndex++;
                        m_currentMaterialTypeIndex = m_currentMaterialTypeIndex % materialCustomizer.m_materialDataV2.Count;
                    }

                    GUILayout.EndHorizontal();


                    if (m_currentMaterialIndex >= materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData.Count)
                    {
                        m_currentMaterialIndex = 0;
                    }

                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("<"))
                    {
                        if (m_currentMaterialIndex == 0)
                            m_currentMaterialIndex = materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData.Count - 1;
                        else
                            m_currentMaterialIndex--;

                        VHMsgManager.Get().SendVHMsg(string.Format(@"renderer customizer v2 {0} {1} settype {2}", characterNames[m_currentCharacterNameIndex], materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_targetName, materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData[m_currentMaterialIndex].m_materialName));
                    }

                    if (GUILayout.Button(materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData[m_currentMaterialIndex].m_materialName))
                    {
                        m_currentMaterialIndex++;
                        m_currentMaterialIndex = m_currentMaterialIndex % materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData.Count;

                        VHMsgManager.Get().SendVHMsg(string.Format(@"renderer customizer v2 {0} {1} settype {2}", characterNames[m_currentCharacterNameIndex], materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_targetName, materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData[m_currentMaterialIndex].m_materialName));
                    }

                    if (GUILayout.Button(">"))
                    {
                        m_currentMaterialIndex++;
                        m_currentMaterialIndex = m_currentMaterialIndex % materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData.Count;

                        VHMsgManager.Get().SendVHMsg(string.Format(@"renderer customizer v2 {0} {1} settype {2}", characterNames[m_currentCharacterNameIndex], materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_targetName, materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData[m_currentMaterialIndex].m_materialName));
                    }

                    GUILayout.EndHorizontal();


                    foreach (var param in m_materialParametersNew)
                    {
                        if (param.m_characterName == characterNames[m_currentCharacterNameIndex])
                        {
                            if (param.m_targetName == materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_targetName)
                            {
                                if (param.m_typeName == materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData[m_currentMaterialIndex].m_materialName)
                                {
                                    GUILayout.BeginHorizontal();

                                    GUILayout.Label(param.m_paramName, GUILayout.Width(60));
                                    GUILayout.Label(string.Format("{0:f2}", param.m_value), GUILayout.Width(35));
                                    float newValue = GUILayout.HorizontalSlider(param.m_value, 0.0f, 1.0f);
                                    if (newValue != param.m_value)
                                    {
                                        param.m_value = newValue;

                                        VHMsgManager.Get().SendVHMsg(string.Format(@"renderer customizer v2 {0} {1} setparam {2} {3}", param.m_characterName, param.m_targetName, param.m_paramName, param.m_value));
                                    }

                                    GUILayout.EndHorizontal();
                                }
                            }
                        }
                    }

                    List<string> m_presetList = new List<string>();

                    foreach (var param in m_materialParametersNewPresets)
                    {
                        if (param.m_characterName == characterNames[m_currentCharacterNameIndex])
                        {
                            if (param.m_targetName == materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_targetName)
                            {
                                if (param.m_typeName == materialCustomizer.m_materialDataV2[m_currentMaterialTypeIndex].m_materialData[m_currentMaterialIndex].m_materialName)
                                {
                                    string keyName = param.m_typeName + "-" + param.m_presetName;

                                    if (!m_presetList.Contains(keyName))
                                    {
                                        m_presetList.Add(keyName);

                                        if (GUILayout.Button(param.m_presetName))
                                        {
                                            // renderer customizer v2 brad shirt color1 255 255 255 255   // rgba
                                            // renderer customizer v2 brad shirt Color1.r 0.1

                                            foreach (var param2 in m_materialParametersNewPresets)
                                            {
                                                if (param2.m_characterName == param.m_characterName &&
                                                    param2.m_targetName == param.m_targetName &&
                                                    param2.m_typeName == param.m_typeName &&
                                                    param2.m_presetName == param.m_presetName)
                                                {
                                                    VHMsgManager.Get().SendVHMsg(string.Format(@"renderer customizer v2 {0} {1} setparam {2} {3}", param2.m_characterName, param2.m_targetName, param2.m_paramName, param2.m_value));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    GUILayout.Label("(if solid)");
                    GUILayout.Label("Shirt Color:");
                    GUILayout.Button("<<Color Picker>>");
                    GUILayout.Button("Preset 1");
                    GUILayout.Button("Preset 2");
                    GUILayout.Button("Preset 3");

                    GUILayout.Label("(if design1)");   // ChrBradShirtPlaid2_Mat
                    GUILayout.Label("Shirt Parameters:");
                    GUILayout.Label("Tiling");
                    GUILayout.Label("Color1");
                    GUILayout.Label("Color2");
                    GUILayout.Label("Color3");
                    GUILayout.Label("Color4");

                    GUILayout.Label("Pants Type:");
                    GUILayout.Label("<<  Solid/Design1/Design2  >>");
                    GUILayout.Label("...");

                    GUILayout.Label("Skin Color");
                    GUILayout.Label("<<  Choice1/Choice2/Choice3  >>");

                    GUILayout.Label("Hair Color");
                    GUILayout.Label("<<  Choice1/Choice2/Choice3  >>");
                }
            }
            else if (m_controllerMenuSelected == ControllerMenus.VIDEO)
            {
                if (VHUtils.SceneManagerActiveSceneName() == "Customizer")
                {
                    if (GUILayout.Button("Record"))
                    {
                        vhmsg.SendVHMsg("renderer_record start");
                    }

                    if (GUILayout.Button("Stop"))
                    {
                        vhmsg.SendVHMsg("renderer_record stop");
                    }
                }
            }
            else if (m_controllerMenuSelected == ControllerMenus.CONFIG)
            {
                GUILayout.Label("Quality:");

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("<", GUILayout.Width(buttonW * 0.16f)))
                {
                    QualitySettings.SetQualityLevel(VHMath.Clamp(QualitySettings.GetQualityLevel() - 1, 0, QualitySettings.names.Length - 1));
                }

                GUILayout.Button(string.Format("{0}", QualitySettings.names[QualitySettings.GetQualityLevel()]), GUILayout.Width(buttonW * 0.6f));

                if (GUILayout.Button(">", GUILayout.Width(buttonW * 0.16f)))
                {
                    QualitySettings.SetQualityLevel(VHMath.Clamp(QualitySettings.GetQualityLevel() + 1, 0, QualitySettings.names.Length - 1));
                }

                GUILayout.EndHorizontal();

                GUILayout.Space(spaceHeight);

                if (GUILayout.Button("Toggle Stats"))
                {
                    GameObject.FindObjectOfType<DebugInfo>().NextMode();
                }

                if (GUILayout.Button("Toggle Console"))
                {
                    m_Console.ToggleConsole();
                }

                if (m_SBM) m_SBM.m_displayLogMessages = GUILayout.Toggle(m_SBM.m_displayLogMessages, "SBMLog");
                m_displayVhmsgLog = GUILayout.Toggle(m_displayVhmsgLog, "VHMsgLog");
                m_timeSlider = GUILayout.HorizontalSlider(m_timeSlider, 0.01f, 3);
                GUILayout.Label(string.Format("Time: {0}", m_timeSlider));

                if (GUILayout.Button("Reset", GUILayout.Height(buttonH)))
                {
                    m_SBM.SBTransform("Brad", m_chrBradStartPos, m_chrBradStartRot);
                    m_SBM.SBTransform("Rachel", m_chrRachelStartPos, m_chrRachelStartRot);
                }
            }


            GUILayout.Space(spaceHeight);


            // switch between cameras
            // for now, only for OculusRiftTest Level
            if (VHUtils.SceneManagerActiveSceneName() == "OculusRiftTest")
            {
                if (oculusNormalCamera.activeSelf)
                {
                    if (GUILayout.Button("Normal View"))
                    {
                        oculusNormalCamera.SetActive(false);
                        oculusCamera.SetActive(true);
                        oculusCharacterController.SetActive(false);
                        if (m_gazingMode == 1)
                        {
                            m_SBM.SBGaze("*", oculusNormalCamera.gameObject.name);
                        }
                    }
                }

                else if (oculusCamera.activeSelf)
                {
                    if (GUILayout.Button("Oculus View"))
                    {
                        oculusNormalCamera.SetActive(false);
                        oculusCamera.SetActive(false);
                        oculusCharacterController.SetActive(true);
                        if (m_gazingMode == 1)
                        {
                            m_SBM.SBGaze("*", oculusCameraCam.gameObject.name);
                        }
                    }
                }

                else if (oculusCharacterController.activeSelf)
                {
                    if (GUILayout.Button("Oculus Controller View"))
                    {
                        oculusNormalCamera.SetActive(true);
                        oculusCamera.SetActive(false);
                        oculusCharacterController.SetActive(false);
                        if (m_gazingMode == 1)
                        {
                            m_SBM.SBGaze("*", oculusCharacterControllerCam.gameObject.name);
                        }
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


    public override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
    }


    void HandlevrSpokeMessage()
    {
        m_userDialogText = "";

        if (!m_bIntroSequencePlaying)
        {
            m_subtitleText = "";
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
                Screen.SetResolution((int)vec2Data.x, (int)vec2Data.y, Screen.fullScreen);
            }
        }
        else if (commandEntered.IndexOf("play_intro") != -1)
        {
            StopAllCoroutines();
            m_IntroCutscene.Play();
            m_bIntroSequencePlaying = true;
        }
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

                string utteranceId = Path.GetFileNameWithoutExtension(path);
                AudioSpeechFile[] speechFiles = FindObjectsOfType<AudioSpeechFile>();
                bool found = false;
                for (int i = 0; i < speechFiles.Length; i++)
                {
                    if (Path.GetFileNameWithoutExtension(speechFiles[i].m_LipSyncInfo.name) == utteranceId)
                    {
                        found = true;
                        AudioSource charVoiceSource = m_SBM.GetCharacterVoice(splitargs[2]);
                        charVoiceSource.clip = speechFiles[i].m_AudioClip;
                        charVoiceSource.Play();
                        break;
                    }
                }

                if (!found)
                {
                    if (path.StartsWith("//"))  // network path
                        path = "file://" + path;
                    else  // assume absolute path
                        path = "file:///" + path;

                    WWW www = new WWW(path);
                    VHUtils.PlayWWWSound(this, www, m_SBM.GetCharacterVoice(splitargs[2]), false);
                }
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

                    if (rendererSplitArgs.Length == 0)
                    {
                        SendMessage(function); // this isn't setup for a lot of the messages that come in
                    }
                    else
                    {
                        SendMessage(function, rendererSplitArgs); // this isn't setup for a lot of the messages that come in
                    }
                    //Debug.Log(string.Format("renderer messeage received {0} {1}", function, rendererSplitArgs[0]));
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
            else if (splitargs[0].Equals("vht_get_motions"))
            {
                SmartbodyMotionSet [] motionSets = GameObject.FindObjectsOfType<SmartbodyMotionSet>();
                foreach (var motionSet in motionSets)
                {
                    foreach (var motion in motionSet.m_MotionsList)
                    {
                        vhmsg.SendVHMsg("VHBuilder motion_name " + motion.name);
                    }
                }
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
                GameObject.Find("ColorPicker").GetComponent<ColorPicker>().setColor = m_currentColor;
            }
        }
    }


    struct MaterialParameters
    {
        public float value;
        public float min;
        public float max;
    }


    struct MaterialParametersGroup
    {
        public Dictionary<string, MaterialParameters> m_parameters;  // "Color1.r", 0.3, 0.0, 1.0
    }


    struct MaterialTypeParameters
    {
        public string m_name;  // shirt, pants, hair
        public Dictionary<string, MaterialParametersGroup> m_parameters;  // design1
        public Dictionary<string, MaterialParametersGroup> m_presets;     // design1
    }

    struct AllMaterialParameters
    {
        public string m_characterName;
        public List<MaterialTypeParameters> m_materialTypes;
    }

    // try again
    public class AllMaterialParametersNew
    {
        public string m_characterName;  // brad
        public string m_targetName;     // shirt
        public string m_typeName;       // solid
        public string m_presetName;     // Preset 1
        public string m_paramName;      // Color1.r
        public float m_value;           // 1.0
        public float m_min;
        public float m_max;
    };

    List<AllMaterialParametersNew> m_materialParametersNew = new List<AllMaterialParametersNew> {
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_paramName = "Color.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_paramName = "Color.g", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_paramName = "Color.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },

        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_1.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_1.g", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_1.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_2.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_2.g", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_2.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_3.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_3.g", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_3.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_4.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_4.g", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_paramName = "Color_4.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },
    };

    List<AllMaterialParametersNew> m_materialParametersNewPresets = new List<AllMaterialParametersNew> {
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_presetName = "Preset1", m_paramName = "Color.r", m_value = 1.0f, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_presetName = "Preset1", m_paramName = "Color.g", m_value = 0.0f, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_presetName = "Preset1", m_paramName = "Color.b", m_value = 0.0f, m_min = 0.0f, m_max = 1.0f },

        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_presetName = "Preset2", m_paramName = "Color.r", m_value = 0.0f, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_presetName = "Preset2", m_paramName = "Color.g", m_value = 1.0f, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_presetName = "Preset2", m_paramName = "Color.b", m_value = 0.0f, m_min = 0.0f, m_max = 1.0f },

        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_presetName = "Preset3", m_paramName = "Color.r", m_value = 0.0f, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_presetName = "Preset3", m_paramName = "Color.g", m_value = 0.0f, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "solid", m_presetName = "Preset3", m_paramName = "Color.b", m_value = 1.0f, m_min = 0.0f, m_max = 1.0f },

        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_1.r", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_1.g", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_1.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_2.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_2.g", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_2.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_3.r", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_3.g", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_3.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_4.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_4.g", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset1", m_paramName = "Color_4.b", m_value = 1, m_min = 0.0f, m_max = 1.0f },

        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_1.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_1.g", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_1.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_2.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_2.g", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_2.b", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_3.r", m_value = 0, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_3.g", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_3.b", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_4.r", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_4.g", m_value = 1, m_min = 0.0f, m_max = 1.0f },
        new AllMaterialParametersNew() { m_characterName = "Brad(Clone)", m_targetName = "shirt", m_typeName = "design1", m_presetName = "Preset2", m_paramName = "Color_4.b", m_value = 0, m_min = 0.0f, m_max = 1.0f },
    };

    /*
    new Cat(){ Name = "Sylvester", Age=8 },
    new Cat(){ Name = "Whiskers", Age=2 },
    new Cat(){ Name = "Sasha", Age=14 }
    */
    /*
    public static VitaInterviewQuestionData [] m_vitaInterviewQuestionData = {
        new VitaInterviewQuestionData("Maria_Hostile_01",      "Greeting",      "Let's get started. What made you want to apply for this job?"),
        new VitaInterviewQuestionData("Maria_Hostile_03",      "Situational",   "I'm very particular about the way I want things to be done and I need to be able to trust whoever I hire to do the job exactly that way without looking over their shoulder. Are you able to complete tasks with little supervision?"),
        new VitaInterviewQuestionData("Maria_Hostile_04",      "Strengths",     "You don't seem very sure about that. Well, let's try this. What do you think is your greatest strength?"),
    */

    protected void customizer(string[] args)
    {
        // m_builder.SendMessage("renderer customizer "+ CharacterComboBox.Items[CharacterComboBox.SelectedIndex] + " " + value);
        // m_builder.SendMessage("renderer customizer " + CharacterComboBox.Items[CharacterComboBox.SelectedIndex] + " " + ShirtPantsComboBox.Items[ShirtPantsComboBox.SelectedIndex] + " " + colorDialog.Color.R + " " + colorDialog.Color.G + " " + colorDialog.Color.B);
        //m_charCustomizer.SendMessage("renderer customizer " + CogCharacterListBox.Items[CogCharacterListBox.SelectedIndex] + " Shirt " + values[0] + " " + values[1] + " " + values[2]);
        //m_charCustomizer.SendMessage("renderer customizer " + CogCharacterListBox.Items[CogCharacterListBox.SelectedIndex] + " Pants " + values[0] + " " + values[1] + " " + values[2]);
        //m_charCustomizer.SendMessage("renderer customizer " + CogCharacterListBox.Items[CogCharacterListBox.SelectedIndex] + " Hair " + values[0] + " " + values[1] + " " + values[2]);
        //m_charCustomizer.SendMessage("renderer customizer " + CogCharacterListBox.Items[CogCharacterListBox.SelectedIndex] + " " + children[i].InnerText);

        if (args.Length >= 3 && args[0] == "v2")
        {
            // renderer customizer v2 brad shirt|pants|hair|skin settype solid|design1
            // renderer customizer v2 brad shirt setparam color1 255 255 255 255   // rgba
            // renderer customizer v2 brad shirt setparam Color1.r 0.2   // float
            // renderer customizer v2 brad shirt tilingx|tilingy 0.6   // 0..1

            string characterName = args[1];
            string pieceToModify = args[2];
            string command = args[3];

            if (command == "settype" && args.Length >= 4)
            {
                string materialName = args[4];

                UnitySmartbodyCharacter sbCharacter = m_SBM.GetCharacterByName(characterName);
                MaterialCustomizer materialCustomizer = sbCharacter.GetComponent<MaterialCustomizer>();

                foreach (var materialType in materialCustomizer.m_materialDataV2)
                {
                    if (materialType.m_targetName == pieceToModify)
                    {
                        foreach (var material in materialType.m_materialData)
                        {
                            if (material.m_materialName == materialName)
                            {
                                Debug.Log(string.Format(@"Setting {0} to {1}", materialType.m_target.name, material.m_material.name));

                                materialType.m_target.material = material.m_material;
                                break;
                            }
                        }
                    }
                }

                //ProceduralMaterial material = m_ShirtDesign1;
            }
            else if (command == "setparam" && args.Length >= 4)
            {
                string param = args[4];
                float value = Convert.ToSingle(args[5]);

                UnitySmartbodyCharacter sbCharacter = m_SBM.GetCharacterByName(characterName);
                MaterialCustomizer materialCustomizer = sbCharacter.GetComponent<MaterialCustomizer>();

                foreach (var materialType in materialCustomizer.m_materialDataV2)
                {
                    if (materialType.m_targetName == pieceToModify)
                    {
                        Material targetMaterial = materialType.m_target.materials[materialType.m_targetMaterialIndex];

                        if (targetMaterial is ProceduralMaterial)
                        {
                            ProceduralMaterial material = (ProceduralMaterial)targetMaterial;

                            // check if color eg, Color_1.r
                            string [] split = param.Split('.');
                            string paramName = param;
                            string channel = "";
                            if (split.Length >= 2)
                            {
                                paramName = split[0];
                                channel = split[1];
                            }

                            if (paramName == "Tiling")
                            {
                                // set both Tiling (x/y) params on Diffuse and Normal
                                material.SetTextureScale("_MainTex", new Vector2(value, value));
                                material.SetTextureScale("_BumpMap", new Vector2(value, value));
                                material.RebuildTexturesImmediately();
                            }
                            else
                            {
                                // procedural param that takes a Color value

                                Color color = material.GetProceduralColor(paramName);

                                switch (channel)
                                {
                                    case "r": color.r = value; break;
                                    case "g": color.g = value; break;
                                    case "b": color.b = value; break;
                                    default: break;
                                }

                                //Debug.Log(string.Format(@"Setting {0} {1} {2} to {3}", materialType.m_target.name, paramName, channel, value));

                                material.SetProceduralColor(paramName, color);
                                material.RebuildTexturesImmediately();
                            }
                        }
                        else
                        {
                            // assume eg, Color1.r
                            string [] split = param.Split('.');
                            string channel = "";
                            if (split.Length >= 2)
                                channel = split[1];

                            Color color = targetMaterial.color;

                            switch (channel)
                            {
                                case "r": color.r = value; break;
                                case "g": color.g = value; break;
                                case "b": color.b = value; break;
                                default: break;
                            }

                            //Debug.Log(string.Format(@"Setting color {0} {1} to {2}", materialType.m_target.name, channel, value));

                            targetMaterial.color = color;
                        }
                        break;
                    }
                }
            }
        }
        else if (args.Length > 4)
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
            float value = float.Parse(args[1]) ;

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


    protected void background_fullpath( string [] args )
    {
        if (args.Length > 0)
        {
            if (args[0] == "file")
            {
                // renderer background_fullpath file ../../background.png
                // renderer background_fullpath file c:/temp/background.png

                if (args.Length > 1)
                {
                    string background = args[1];

                    string path = background;
                    path = Path.GetFullPath(path);
                    path = path.Replace(@"\", "/");

                    if (path.StartsWith("//"))  // network path
                        path = "file://" + path;
                    else  // assume absolute path
                        path = "file:///" + path;

                    Debug.LogFormat("background_fullpath() - {0}", path);

                    WWW www = new WWW(path);
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
                    {
                        StartCoroutine(GazeAtCamera());
                    }
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


    void destroyallcharacters()
    {
        UnitySmartbodyCharacter[] characters = (UnitySmartbodyCharacter[])FindObjectsOfType(typeof(UnitySmartbodyCharacter));
        foreach (UnitySmartbodyCharacter character in characters)
        {
            DestroyGameObject(character.SBMCharacterName);
        }

        Array.Clear(m_lineupGameObjects, 0, m_lineupGameObjects.Length);
    }


    void ProcessCommandLineAndConfigSettings()
    {
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
        vhmsg.SubscribeMessage("vht_get_motions");
        vhmsg.SubscribeMessage("renderer_record");
        vhmsg.SubscribeMessage("renderer_gui");
        vhmsg.SubscribeMessage("sbm");

        vhmsg.AddMessageEventHandler(new VHMsgBase.MessageEventHandler(VHMsg_MessageEvent));

        vhmsg.SendVHMsg("vrComponent renderer");

        if (m_AcquireSpeechState != AcquireSpeechState.Disabled)
        {
            vhmsg.SendVHMsg("acquireSpeech start");
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


    void UnSelectCharacters()
    {
        for (int i = 0; i < m_Characters.Length; i++)
        {
            VHUtils.FindChild(m_Characters[i].gameObject, "Camera").GetComponent<Camera>().enabled = false;
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
    }


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


    void LineupToggleCharacter(int characterIndex)
    {
        m_lineupCharacterFlags[characterIndex] = !m_lineupCharacterFlags[characterIndex];

        if (m_lineupCharacterFlags[characterIndex])
        {
            for (int i = 0; i < m_lineupCharacters.Length; i++)
            {
                if (m_lineupGameObjects[i] == null)
                {
                    Vector3 position = m_lineupPositions[i];
                    Quaternion rotation = m_lineupRotations[i];

                    UnityEngine.Object character = Instantiate(Resources.Load(m_lineupCharacters[characterIndex]), position, rotation);

                    m_lineupGameObjects[i] = character;
                    break;
                }
            }
        }
        else
        {
            GameObject character = GameObject.Find(m_lineupCharacters[characterIndex] + "(Clone)");
            for (int i = 0; i < m_lineupCharacters.Length; i++)
            {
                if (m_lineupGameObjects[i] == character)
                {
                    Destroy(m_lineupGameObjects[i]);
                    m_lineupGameObjects[i] = null;
                    break;
                }
            }
        }
    }


    void createcharacter(string [] nameAndObjectType)
    {
        string name = nameAndObjectType[0];
        string objectType = nameAndObjectType[1];

        for (int i = 0; i < m_lineupCharacters.Length; i++)
        {
            if (m_lineupGameObjects[i] == null)
            {
                Vector3 position = m_lineupPositions[i];
                Quaternion rotation = m_lineupRotations[i];

                UnityEngine.Object character = Instantiate(Resources.Load(objectType), position, rotation);
                character.name = FindUniqueName(name);

                m_lineupGameObjects[i] = character;
                break;
            }
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


    IEnumerator ShowIntro(float delay)
    {
        yield return new WaitForSeconds(delay);

        bool showIntro = VHGlobals.m_showIntro;

        if (showIntro && VHUtils.SceneManagerActiveSceneName() == "Campus")
        {
            m_debugOnScreenLog.gameObject.SetActive(false);

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


    void UpdatePerceptionAppState()
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
            vhmsg.SendVHMsg("vrPerceptionApplication", "trackAddressee");
            m_PerceptionApplicationState = PerceptionApplicationState.TrackAddressee;
        }
        else if (m_PerceptionApplicationState == PerceptionApplicationState.TrackAddressee)
        {
            vhmsg.SendVHMsg("vrPerceptionApplication", "TOGGLE");
            m_PerceptionApplicationState = PerceptionApplicationState.Disabled;
        }
    }


    void ToggleGazeMode()
    {
        m_gazingMode++;
        m_gazingMode = m_gazingMode % 3;  // skipping mousepawn gaze for tab demo
        if (m_gazingMode == 0)
        {
            m_SBM.PythonCommand(string.Format(@"scene.command('char {0} gazefade out 1')", "*"));
        }

        if (m_gazingMode == 1)
        {
            if (VHUtils.SceneManagerActiveSceneName().Equals("OculusRiftTest"))
            {
                if (oculusNormalCamera.activeSelf)
                {
                    m_SBM.SBGaze("*", oculusNormalCamera.gameObject.name);
                }
                else if (oculusCamera.activeSelf)
                {
                    m_SBM.SBGaze("*", oculusCameraCam.gameObject.name);
                }
                else if (oculusCharacterController.activeSelf)
                {
                    m_SBM.SBGaze("*", oculusCharacterControllerCam.gameObject.name);
                }
            }
            else
            {
                m_SBM.SBGaze("*", "Camera", 400, 400, CharacterDefines.GazeJointRange.HEAD_EYES);
                //m_SBM.SBGaze("*", "Camera");
            }
        }

        if (m_gazingMode == 2)
        {
            m_SBM.SBGaze("*", "MousePawn", 1200, 1200, CharacterDefines.GazeJointRange.HEAD_EYES);
            //m_SBM.SBGaze("*", "MousePawn");
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
            string utteranceId = Path.GetFileNameWithoutExtension(audioFile);
            AudioSpeechFile[] speechFiles = FindObjectsOfType<AudioSpeechFile>();
            for (int i = 0; i < speechFiles.Length; i++)
            {
                if (Path.GetFileNameWithoutExtension(speechFiles[i].m_LipSyncInfo.name) == utteranceId)
                {
                    AudioSource charVoiceSource = m_SBM.GetCharacterVoice("Brad");
                    charVoiceSource.clip = speechFiles[i].m_AudioClip;
                    charVoiceSource.Play();
                    break;
                }
            }
        }
    }


    void TossNPCDomain()
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
}
