using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System;

/// <summary>
/// Toolkit character creation window
/// </summary>
public class VHWizard : EditorWindow
{
#if !UNITY_WEBPLAYER
    static VHWizard ThisWindow;    

    public static VHMsg.Client vhmsg;

    public Vector2 m_questionPosition;
    public Vector2 m_answerPosition;


    string m_newQuestion = "";
    string m_newAnswer = "";    
    string m_characterName = "ChrBrad";

    

    static int m_questionNumber = 0;
    static int m_answerNumber = 0;

    bool m_firstAnswer = true;
    bool m_showCreateCharacter = true;
    //bool m_showTTSOption = false;
    bool m_showNPCOptions = true;
    bool m_showNVBGOptions = true;
    bool m_isSetup = true;
    bool m_setupToolsLaunched = false;
    bool m_NPCSaved = false;

    DateTime m_currentTime;
    DateTime m_previousTime;

    
    int m_prefabIndex;
    int m_postureIndex;
    int m_skeletonIndex;
    int m_faceIndex;
    int m_audioIndex;
    int m_backupVoiceIndex;
    int m_gazeLimit;
    int m_saccadeType;
    int m_ruleInputFile;
    int m_saliencyMapFile = 0;
    int m_NPCwaitTime = 3;
    //int m_selGridInt = 5;       //SETTING TO 5 TO MAKE SURE NONE OF THE GRID BUTTONS ARE SELECTED ON START


    List<string> m_questionList;
    List<string> m_answerList;
    private RectOffset bdr;

    bool m_micRecording = false;

    
    //string[] m_selStrings = new string[] { "Setup Mode", "Run Mode"};

    

    enum CharName
    {
        CHRBRAD,
        CHRRACHAEL
    }

   

    string[] m_charNames = {"ChrBrad", "ChrRachael" };
    string[] m_postureNames = { "ChrBrad@Idle01", "ChrBrad@Idle02","ChrBrad@Idle03" };
    string[] m_skeletonNames = { "ChrBrad.sk", "ChrRachel.sk"};
    string[] m_faceNames = { "Brad", "Rachel" };
    //string[] m_audioTypes = { "remote", "audiofile" };
    //string[] m_backupVoiceNames = { "Festival_voice_rab_diphone", "MicrosoftAnna" };
    //string[] m_gazeLimitTypes = { "EYES", "EYES NECK", "EYES CHEST", "EYES BACK", "NECK CHEST", "NECK BACK", "CHEST BACK" };
    //string[] m_saccadeTypes = { "Talk", "Listen", "Think" };
    string[] m_ruleInputFileNames = {"rule_input_ChrBrad.xml", "rule_input_brad.xml", "rule_input_ChrRachel.xml" };
    string[] m_saliencyMaps = { "saliency_map_init_brad.xml", "saliency_map_init_rachel.xml"};
    
    

    //bool m_charCreateGroup = false;   

    //string m_audioDirectory = "";   

    Rect m_Area = new Rect(5, 5, 800, 2000);    

    // Use this for initialization
    [MenuItem("VH/VHWizard")]
    static void Init()
    {
        ThisWindow = (VHWizard)EditorWindow.GetWindow(typeof(VHWizard));
        ThisWindow.SetupExampleQuestions();       

        SetupVHMsgCallback();
    }

    public void SetupExampleQuestions()
    {
        m_questionList = new List<string>();
        m_answerList = new List<string>();       
    }

    public static void SendVHMsg(string message)
    {
        vhmsg.SendMessage(message);
    }

    public static void SetupVHMsgCallback()
    {
        try
        {
            vhmsg = new VHMsg.Client();
            vhmsg.OpenConnection();
            vhmsg.MessageEvent += new VHMsg.Client.MessageEventHandler(MessageAction);
            vhmsg.SubscribeMessage("wizard");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e.ToString());
        }
    }




    private static void MessageAction(object sender, VHMsg.Message args)
    {


        string[] splitargs = args.s.Split(" ".ToCharArray());

        if (splitargs[0].Equals("wizard"))
        {
            if (splitargs[1].Equals("audiofile_created"))
            {
                if (splitargs.Length > 2)
                    try
                    {
                        vhmsg.SendMessage("NPCEditor <script target=\"user\">document.getModel().getAnswers().getUtterances().get(" + m_answerNumber + ").setID(\"" + splitargs[2] + "\");</script>");
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError(e.ToString());
                    }
            }

        }
    }



    void Update()
    {        
    }




    void DrawControlSection()
    {
       
        //m_selGridInt = GUILayout.SelectionGrid(m_selGridInt, m_selStrings, 2,GUILayout.Width(200));

        if (m_isSetup)
        {
            if (GUILayout.Button("Launch Setup Tools", GUILayout.Width(200)))
            {

                if (!m_setupToolsLaunched)
                {
                    
                    try
                    {
                        Process npc = new Process();
                        npc.StartInfo.FileName = Application.dataPath + "/../../../tools/launch-scripts/run-toolkit-npceditor-wizard.bat";
                        npc.Start();
                        m_previousTime = DateTime.Now;

                        Process speechRecorder = new Process();
                        speechRecorder.StartInfo.FileName = Application.dataPath + "/../../../tools/launch-scripts/run-toolkit-speechrecorder-wizard.bat";
                        speechRecorder.Start();

                        Process pocketsphinx = new Process();
                        pocketsphinx.StartInfo.FileName = Application.dataPath + "/../../../tools/launch-scripts/run-toolkit-pocketsphinx-sonic-server.bat";
                        pocketsphinx.Start();

                        Process acqSpeech = new Process();
                        acqSpeech.StartInfo.FileName = Application.dataPath + "/../../../tools/launch-scripts/run-toolkit-acquirespeech-wizard.bat";
                        acqSpeech.Start();
                    }
                    catch (UnityException e)
                    {
                        UnityEngine.Debug.LogError(e.ToString());
                    }

                    m_setupToolsLaunched = true;

                }               
            }
        }
        else 
        {
            if(GUILayout.Button("Previous Screen",GUILayout.Width(200)))
            {
                m_isSetup = true;
                SendVHMsg("vrKillComponent nvb");
                Process speechRecorder = new Process();
                speechRecorder.StartInfo.FileName = Application.dataPath + "/../../../tools/launch-scripts/run-toolkit-speechrecorder-wizard.bat";
                speechRecorder.Start();
            }
        }            
    }

    
    void OnGUI()
    {       
        

        GUILayout.BeginArea(m_Area);
        GUILayout.Space(10);

        DrawControlSection();


        //IF NPC HAS BEEN LAUNCHED, THEN SAVE AS NEW PLIST
        if (m_setupToolsLaunched)
        {
            m_currentTime = DateTime.Now;
            TimeSpan diff = m_currentTime - m_previousTime;
            if ((diff.TotalSeconds > m_NPCwaitTime) && (!m_NPCSaved))
            {
                SendVHMsg("NPCEditor <script target=\"user\">URL url = new File(\"wizard_current.plist\").toURI().toURL();document.saveToURLOfTypeForSaveOperation(url, document.getApplication().fileTypeForURL(url), com.leuski.af.Document.SaveOperation.kSaveAs);</script>");
                m_NPCSaved = true;
            }
        }


        GUILayout.Label("___________________________________________________________________________________________________________________", EditorStyles.boldLabel);

        if (m_isSetup)
        {
            m_showCreateCharacter = EditorGUILayout.Foldout(m_showCreateCharacter, "Create SB Character");
            if (m_showCreateCharacter)
            {
                DrawCreateCharacterTab();
            }
            GUILayout.Space(30);
        }        


        // NVBG Tab
        if (m_isSetup)
        {
            m_showNVBGOptions = EditorGUILayout.Foldout(m_showNVBGOptions, "Non-Verbal Behavior");
            if (m_showNVBGOptions)
            {
                DrawNVBGTab();
            }
            GUILayout.Space(30);
        }
        


        // NPCEditor Tab
        if (m_isSetup)
        {
            m_showNPCOptions = EditorGUILayout.Foldout(m_showNPCOptions, "Dialogue Manager");
            if (m_showNPCOptions)
            {
                DrawNPCTab();
            }
            GUILayout.Space(10);
        }


        GUILayout.Label("___________________________________________________________________________________________________________________", EditorStyles.boldLabel);

        if (!m_isSetup)
        {

            if (GUILayout.Button("Create Character", GUILayout.Width(200)))
            {
                SendVHMsg("nvbg_create_character " + m_characterName + "");
                SendVHMsg("nvbg_set_option " + m_characterName + " rule_input_file " + m_ruleInputFileNames[m_ruleInputFile]);
                SendVHMsg("nvbg_set_option " + m_characterName + " saliency_map " + m_saliencyMaps[m_saliencyMapFile]);
                string text = "NPCEditor <script target=\"user\">document.startTrainingAll();</script>";
                SendVHMsg(text);
            }
        }
        else
        {
            if (GUILayout.Button("Run Mode", GUILayout.Width(200)))
            {
                m_isSetup = false;
                SendVHMsg("vrKillComponent VHTSpeechRecorder");

                Process parser = new Process();
                //UnityEngine.Debug.LogError(Application.dataPath + "/../../../bin/logger/run.bat");
                parser.StartInfo.FileName = Application.dataPath + "/../../../tools/launch-scripts/run-toolkit-nvb-parser.bat";
                parser.Start();

                Process nvbg = new Process();
                //UnityEngine.Debug.LogError(Application.dataPath + "/../../../bin/logger/run.bat");
                nvbg.StartInfo.FileName = Application.dataPath + "/../../../tools/launch-scripts/run-toolkit-nvbg-wizard.bat";
                nvbg.Start();
            }
        }
        GUILayout.Label("___________________________________________________________________________________________________________________", EditorStyles.boldLabel);

        GUILayout.Space(10);


        if (GUILayout.Button("CloseAllApps", GUILayout.Width(200)))
        {
            SendVHMsg("vrKillComponent npceditor");
            SendVHMsg("vrKillComponent nvb");
            SendVHMsg("vrKillComponent VHTSpeechRecorder");
            SendVHMsg("vrKillComponent asr-server");
            SendVHMsg("vrKillComponent asr");
            m_setupToolsLaunched = false;
            m_NPCSaved = false;
        }


        GUILayout.EndArea();
    }

            
       

    


    void DrawCreateCharacterTab()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Enter Character Name");
        m_characterName = GUILayout.TextField(m_characterName, 25, GUILayout.Width(200));

        GUILayout.Space(10);
        m_prefabIndex = EditorGUILayout.Popup("Prefab", m_prefabIndex, m_charNames, GUILayout.Width(300));

        GUILayout.Space(10);

        m_skeletonIndex = EditorGUILayout.Popup("Skeleton", m_skeletonIndex, m_skeletonNames, GUILayout.Width(300));

        GUILayout.Space(10);

        m_postureIndex = EditorGUILayout.Popup("Posture", m_postureIndex, m_postureNames, GUILayout.Width(300));

        GUILayout.Space(10);

        m_faceIndex = EditorGUILayout.Popup("FaceDefinition", m_faceIndex, m_faceNames, GUILayout.Width(300));

        GUILayout.Space(10);

        

        GUILayout.Space(10);
        //EditorGUILayout.EndToggleGroup();       

        GUILayout.Label("___________________________________________________________________________________________________________________", EditorStyles.boldLabel);
    }

    void DrawNPCTab()
    {
 

        GUILayout.Space(20);


        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(120));
            {
                if (GUILayout.Button("Next", GUILayout.Width(70)))
                {
                    if (m_questionNumber == m_questionList.Count - 1)
                        m_questionNumber = 0;
                    else
                        m_questionNumber++;
                }
                if (GUILayout.Button("Previous", GUILayout.Width(70)))
                {
                    if (m_questionNumber == 0)
                        m_questionNumber = m_questionList.Count - 1;
                    else
                        m_questionNumber--;
                }
                //if (GUILayout.Button("Remove", GUILayout.Width(70)))
                //{
                //    m_questionList.Remove(m_questionList[m_questionNumber]);
                //    if (m_questionNumber != 0)
                //        m_questionNumber--;
                //}
            }
            EditorGUILayout.EndVertical();


            m_questionPosition = GUILayout.BeginScrollView(m_questionPosition, GUILayout.Width(150), GUILayout.Height(100));
            for (int i = 0; i < m_questionList.Count; ++i)
            {
                if (i == m_questionNumber)
                    GUILayout.Label(m_questionList[i], EditorStyles.whiteLargeLabel);
                else
                    GUILayout.Label(m_questionList[i]);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(50);

            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(120));
            {
                if (GUILayout.Button("Next", GUILayout.Width(70)))
                {
                    if (m_answerNumber == m_answerList.Count - 1)
                        m_answerNumber = 0;
                    else
                        m_answerNumber++;
                }
                if (GUILayout.Button("Previous", GUILayout.Width(70)))
                {
                    if (m_answerNumber == 0)
                        m_answerNumber = m_answerList.Count - 1;
                    else
                        m_answerNumber--;
                }
                //if (GUILayout.Button("Remove", GUILayout.Width(70)))
                //{
                //    m_answerList.Remove(m_answerList[m_answerNumber]);
                //    if (m_answerNumber != 0)
                //        m_answerNumber--;
                //}
            }
            EditorGUILayout.EndVertical();


            m_answerPosition = GUILayout.BeginScrollView(m_answerPosition, GUILayout.Width(150), GUILayout.Height(100));
            for (int i = 0; i < m_answerList.Count; ++i)
            {

                if (i == m_answerNumber)
                    GUILayout.Label(m_answerList[i], EditorStyles.whiteLargeLabel);
                else
                    GUILayout.Label(m_answerList[i]);
            }
            GUILayout.EndScrollView();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();


        if (GUILayout.Button("Clear", GUILayout.Width(70)))
        {
            m_questionList.Clear();
            string text = "NPCEditor <script target=\"user\">document.getModel().getQuestions().getUtterances().clear();</script>";
            SendVHMsg(text);
        }

        GUILayout.Space(250);

        if (GUILayout.Button("Clear", GUILayout.Width(70)))
        {
            m_answerList.Clear();
            string text = "NPCEditor <script target=\"user\">document.getModel().getAnswers().getUtterances().clear();</script>";
            SendVHMsg(text);
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);


        EditorGUILayout.BeginHorizontal();
        {

            m_newQuestion = GUILayout.TextField(m_newQuestion, 1000, GUILayout.Width(200));

            GUILayout.Space(120);

            m_newAnswer = GUILayout.TextField(m_newAnswer, 1000, GUILayout.Width(200));
        }

        EditorGUILayout.EndHorizontal();


        EditorGUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("Add Question", GUILayout.Width(100)))
            {
                m_questionList.Add(m_newQuestion);
                string text = "NPCEditor <script target=\"user\">edu.usc.ict.npc.editor.model.Person domain = document.getModel().getDefaultSpeaker(); edu.usc.ict.npc.editor.model.EditorUtterance eu = new edu.usc.ict.npc.editor.model.EditorUtterance(\""+m_newQuestion+"\", document.getModel().getQuestions().makeUniqueID(domain), domain, new Date()); document.getManagedObjectContext().insertObject(eu); document.getModel().getQuestions().getUtterances().add(eu);</script>";
                SendVHMsg(text);
            }

            GUILayout.Space(220);

            if (GUILayout.Button("Add Answer", GUILayout.Width(100)))
            {
                if (m_firstAnswer)
                {
                    string text1 = "NPCEditor <script target=\"user\">edu.usc.ict.dialog.model.Category c	= document.getModel().getCategoryWithID(\"speaker\"); edu.usc.ict.npc.editor.model.EditorToken.insertTokenIntoObjectContextAndToCategory(document.getModel().getManagedObjectContext(), c, \"" + m_characterName + "\", \"" + m_characterName + "\");</script>";
                    SendVHMsg(text1);
                    m_firstAnswer = false;
                }

                m_answerList.Add(m_newAnswer);
                string text = "NPCEditor <script target=\"user\">edu.usc.ict.npc.editor.model.Person domain1 = document.getModel().getSpeakers().get(0); edu.usc.ict.npc.editor.model.EditorUtterance eu = new edu.usc.ict.npc.editor.model.EditorUtterance(\"" + m_newAnswer + "\", document.getModel().getAnswers().makeUniqueID(domain1), domain1, new Date());eu.addAnnotation(document.getModel().getCategoryWithID(\"speaker\").tokenWithID(\"" + m_characterName + "\")); document.getManagedObjectContext().insertObject(eu); document.getModel().getAnswers().getUtterances().add(eu);</script>";
                SendVHMsg(text);
            }
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        {                        

            GUILayout.Space(325);
            if (GUILayout.RepeatButton("Record With Mic", GUILayout.Width(250)))
            {
                if (!m_micRecording && Event.current.type == EventType.Repaint)
                {
                    SendVHMsg("wizard_text " + m_answerList[m_answerNumber]);
                    SendVHMsg("acquireSpeech startUtterance mic");
                }
                m_micRecording = true;
            }
            else
            {
                if (m_micRecording)
                    SendVHMsg("acquireSpeech stopUtterance mic");
                m_micRecording = false;
            }

        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Link Question to Answer", GUILayout.Width(200)))
        {
            string text = "NPCEditor <script target=\"user\">document.getModel().setLinkValue(" + m_questionNumber + "," + m_answerNumber + ",6);</script>";
            SendVHMsg(text);
        }


        GUILayout.Label("___________________________________________________________________________________________________________________", EditorStyles.boldLabel);
        
    }

    void DrawNVBGTab()
    {                
        GUILayout.Space(20);                

        m_ruleInputFile = EditorGUILayout.Popup("Behavior File", m_ruleInputFile, m_ruleInputFileNames, GUILayout.Width(400));
        GUILayout.Space(10);
        m_saliencyMapFile = EditorGUILayout.Popup("Saliency Map", m_saliencyMapFile, m_saliencyMaps, GUILayout.Width(400));        

        GUILayout.Space(10);       

        GUILayout.Label("___________________________________________________________________________________________________________________", EditorStyles.boldLabel);
    }

    void OnDestroy()
    {
        vhmsg.CloseConnection();
    }




#endif
    
}
