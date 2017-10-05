using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections;

public class SmartbodyManager : ICharacterController
{
    public delegate void OnCustomCharacterCallback(UnitySmartbodyCharacter character);

    #region Constants
    const float TextHeight = 22;
    const string Attach = "-attach";
    const string PythonCmd = "python";
    const string ICTSettingsNode = "IctUnitySettings";

    public class SkmMetaData
    {
        public float Length = -1;
        public Dictionary<string, float> SyncPoints = new Dictionary<string, float>();
    }
    #endregion

    #region DataMembers
    public bool m_UseReleaseMode = true;
    public bool m_displayLogMessages = true;
    public bool m_logToFile = true;
    public bool m_debugQuickLoadNoMotions = false;
    public bool m_showDeprecatedCommands = false;

    public delegate string BmlReplyAppSpecificCallback(string characterName, string requestId, string utteranceId);
    public BmlReplyAppSpecificCallback m_bmlReplyAppSpecificCallback;

    // these cam variables are for the debugger camera (sbmonitor)
    [NonSerialized]
    public Vector3 m_camPos = Vector3.zero;
    [NonSerialized]
    public Quaternion m_camRot = Quaternion.identity;
    [NonSerialized]
    public double m_camFovY = 45;
    [NonSerialized]
    public double m_camAspect = 1.5;
    [NonSerialized]
    public double m_camZNear = 0.1;
    [NonSerialized]
    public double m_camZFar = 1000;

    // singleton
    static SmartbodyManager g_smartbodyManager;

    // list of characters currently polling smart body
    protected List<UnitySmartbodyCharacter> m_characterList = new List<UnitySmartbodyCharacter>();
    protected IntPtr m_ID = new IntPtr(-1);
    protected List<SmartbodyPawn> m_pawns = new List<SmartbodyPawn>();

    protected Dictionary<string, SmartbodyFaceDefinition> m_faceDefinitions = new Dictionary<string, SmartbodyFaceDefinition>();
    protected Dictionary<string, GestureMapDefinition> m_gestureMapDefinitions = new Dictionary<string, GestureMapDefinition>();
    protected List<string> m_loadedSkeletons = new List<string>();
    protected List<string> m_loadedMotions = new List<string>();
    protected Dictionary<string, SmartbodyJointMap> m_jointMaps = new Dictionary<string, SmartbodyJointMap>();
    protected List<string> m_retargetPairs = new List<string>();
    List<ICharacter> m_CharactersToUpload = new List<ICharacter>();

    protected bool m_bReceiveBoneUpdates = true;

    protected OnCustomCharacterCallback m_CustomCreateCBs;
    protected OnCustomCharacterCallback m_CustomDeleteCBs;

    protected bool m_startCalled = false;
    protected bool m_updateCalled = false;

    protected float m_positionScaleHack = 1.0f;

    protected StringBuilder m_updateReturnString1 = new StringBuilder(1024);
    protected StringBuilder m_updateReturnString2 = new StringBuilder(256);
    protected StringBuilder m_updateReturnString3 = new StringBuilder(256);

    // cache the names from the CreateDiphone() call to help with manually converting the python generated code
    string m_diphonePrevFromPhoneme = "";
    string m_diphonePrevToPhoneme = "";
    string m_diphonePrevName = "";

    #endregion

    #region Properties
    public bool ReceiveBoneUpdates
    {
        get { return m_bReceiveBoneUpdates; }
        set { m_bReceiveBoneUpdates = value; }
    }

    public float PositionScaleHack { get { return m_positionScaleHack; } }
    #endregion

    #region Functions
    public static SmartbodyManager Get()
    {
        //Debug.Log("SmartbodyManager.Get()");
        if (g_smartbodyManager == null)
        {
            g_smartbodyManager = UnityEngine.Object.FindObjectOfType(typeof(SmartbodyManager)) as SmartbodyManager;
        }

        return g_smartbodyManager;
    }

    public static void ResetGet()
    {
        // this function will reset the global singleton to null, so that when Get() is called again, the scene is searched again for the gameobject
        // this is helpful when switching between SmartbodyManager and SmartbodyManagerBoneBus.
        g_smartbodyManager = null;
    }

    void Awake()
    {
        StreamingAssetsExtract.ExtractStreamingAssets();

        SmartbodyExternals.LoadLibraries(m_UseReleaseMode);
    }

    public virtual void Start()
    {
        if (m_startCalled)
            return;

        //Debug.Log("SmartbodyManager.Start()");

        m_ID = SmartbodyExternals.CreateSBM(m_UseReleaseMode);

        //Debug.Log("m_sbmID = " + m_ID);

        if (m_ID == new IntPtr(-1))
        {
            Debug.LogError("Failed to CreateSBM()");
        }

        InitConsole();

        string pythonLibPath = "../smartbody/Python27/Lib";
        if (SmartbodyExternals.Init(m_ID, pythonLibPath, m_logToFile))
        {
            Debug.Log("Smartbody successfully init");

            //PythonCommand("scene.command('vhmsglog on')");

            SubscribeVHMsg();

            if (m_showDeprecatedCommands)
            {
                PythonCommand("scene.setBoolAttribute('warnDeprecatedCommands', True)");
            }

            SmartbodyExternals.SBDebuggerServer_SetID(m_ID, "unity");
            SmartbodyExternals.SBDebuggerServer_SetRendererIsRightHanded(m_ID, false);
        }
        else
        {
            Debug.LogError("Smartbody failed to init");
        }


        SmartbodyInit initSettings = GetComponent<SmartbodyInit>();
        if (initSettings != null)
        {
            Init(initSettings);
        }
        else
        {
            Debug.LogWarning("SmartbodyManager.Start() - No SmartbodyInit script attached.  You need to attach a SmartbodyInit script to this gameobject so that it will initialize properly");
        }

        m_startCalled = true;
    }


    protected virtual void Update()
    {
        //Debug.Log("SmartbodyManager.Update() - " + Time.time);

        SmartbodyExternals.SBDebuggerServer_SetCameraValues(m_ID, m_camPos.x / m_positionScaleHack, m_camPos.y / m_positionScaleHack,
                                            m_camPos.z / m_positionScaleHack, m_camRot.x, m_camRot.y, m_camRot.z,
                                            m_camRot.w, m_camFovY, m_camAspect, m_camZNear, m_camZFar);

        if (!m_updateCalled)
        {
            SmartbodyExternals.Update(m_ID, Time.time);  // update smartbody with current time.  Only need to do this rarely, like on scene init.  perhaps again after long running times.
            m_updateCalled = true;
        }
        else
        {
            SmartbodyExternals.UpdateUsingDelta(m_ID, Time.deltaTime);   // use delta time version of Update(), to prevent floating point imprecision over time.
        }


        float weight = 0;
        float blendTime = 0;
        int logMessageType = 0;

        m_updateReturnString1.Length = 0;
        m_updateReturnString2.Length = 0;
        while (SmartbodyExternals.IsCharacterCreated(m_ID, m_updateReturnString1, m_updateReturnString1.Capacity, m_updateReturnString2, m_updateReturnString2.Capacity))
        {
            OnCharacterCreate(m_ID, m_updateReturnString1.ToString(), m_updateReturnString2.ToString());
            m_updateReturnString1.Length = 0;
            m_updateReturnString2.Length = 0;
        }

        m_updateReturnString1.Length = 0;
        while (SmartbodyExternals.IsCharacterDeleted(m_ID, m_updateReturnString1, m_updateReturnString1.Capacity))
        {
            OnCharacterDelete(m_ID, m_updateReturnString1.ToString());
            m_updateReturnString1.Length = 0;
        }

        m_updateReturnString1.Length = 0;
        while (SmartbodyExternals.IsCharacterChanged(m_ID, m_updateReturnString1, m_updateReturnString1.Capacity))
        {
            OnCharacterChange(m_ID, m_updateReturnString1.ToString());
            m_updateReturnString1.Length = 0;
        }

        m_updateReturnString1.Length = 0;
        m_updateReturnString2.Length = 0;
        while (SmartbodyExternals.IsVisemeSet(m_ID, m_updateReturnString1, m_updateReturnString1.Capacity, m_updateReturnString2, m_updateReturnString2.Capacity, ref weight, ref blendTime))
        {
            OnViseme(m_ID, m_updateReturnString1.ToString(), m_updateReturnString2.ToString(), weight, blendTime);
            m_updateReturnString1.Length = 0;
            m_updateReturnString2.Length = 0;
        }

        m_updateReturnString1.Length = 0;
        m_updateReturnString2.Length = 0;
        weight = 0;
        while (SmartbodyExternals.IsChannelSet(m_ID, m_updateReturnString1, m_updateReturnString1.Capacity, m_updateReturnString2, m_updateReturnString2.Capacity, ref weight))
        {
            OnChannel(m_ID, m_updateReturnString1.ToString(), m_updateReturnString2.ToString(), weight);
            m_updateReturnString1.Length = 0;
            m_updateReturnString2.Length = 0;
            weight = 0;
        }

        m_updateReturnString1.Length = 0;
        logMessageType = 0;
        while (SmartbodyExternals.IsLogMessageWaiting(m_ID, m_updateReturnString1, m_updateReturnString1.Capacity, ref logMessageType))
        {
            OnLogMessage(m_updateReturnString1.ToString(), logMessageType);
            m_updateReturnString1.Length = 0;
            logMessageType = 0;
        }

        m_updateReturnString1.Length = 0;
        m_updateReturnString2.Length = 0;
        m_updateReturnString3.Length = 0;
        while (SmartbodyExternals.IsBmlRequestWaiting(m_ID, m_updateReturnString1, m_updateReturnString1.Capacity, m_updateReturnString2, m_updateReturnString2.Capacity, m_updateReturnString3, m_updateReturnString3.Capacity))
        {
            SendBmlReply(m_updateReturnString1.ToString(), m_updateReturnString2.ToString(), m_updateReturnString3.ToString());
            m_updateReturnString1.Length = 0;
            m_updateReturnString2.Length = 0;
            m_updateReturnString3.Length = 0;
        }
    }


    protected virtual void LateUpdate()
    {
        if (VHUtils.IsEditor())
        {
            if (!ReceiveBoneUpdates)
                return;
        }

        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;
        for (int i = 0; i < m_characterList.Count; i++)
        {
            if (!GetUnityCharacterData(m_characterList[i].SBMCharacterName, ref m_characterList[i].m_CharacterData))
            {
                //continue;
            }

            // character position
            pos.x = -m_characterList[i].m_CharacterData.m_Character.x;
            pos.y = m_characterList[i].m_CharacterData.m_Character.y;
            pos.z = m_characterList[i].m_CharacterData.m_Character.z;

            // character orientation
            rot.x = m_characterList[i].m_CharacterData.m_Character.rx;
            rot.y = -m_characterList[i].m_CharacterData.m_Character.ry;
            rot.z = -m_characterList[i].m_CharacterData.m_Character.rz;
            rot.w = m_characterList[i].m_CharacterData.m_Character.rw;

            OnSetCharacterPosition(m_characterList[i], pos);
            OnSetCharacterRotation(m_characterList[i], rot);

            m_characterList[i].OnBoneTransformations(m_positionScaleHack);

            //SmartbodyCharacterWrapper
            //SmartbodyExternals.ReleaseCharacter(ref m_characterList[i].m_CharacterData.m_Character);
        }
    }


    protected virtual void OnApplicationQuit()
    {
        //Debug.Log("SmartbodyManager.OnApplicationQuit()");
    }


    protected void OnDestroy()
    {
        //Debug.Log("SmartbodyManager.OnDestroy()");

        Shutdown();
    }


    public void Init(SmartbodyInit initSettings)
    {
        m_faceDefinitions.Clear();
        m_gestureMapDefinitions.Clear();
        m_loadedSkeletons.Clear();
        m_loadedMotions.Clear();
        m_jointMaps.Clear();
        m_retargetPairs.Clear();

        // If initialPySeqPath is null, we set a default.  We don't set the default in the
        // SmartbodyInit class because we can't call GetExternalAssetsPath() in a default assignment.
        // It can only be called in Awake() or Start()
        string pySeqPath;
        if (string.IsNullOrEmpty(initSettings.initialPySeqPath))
            pySeqPath = VHFile.GetExternalAssetsPath() + "SB";
        else
            pySeqPath = initSettings.initialPySeqPath;

        if (string.IsNullOrEmpty(initSettings.audioPath))
        {
            initSettings.audioPath = VHFile.GetExternalAssetsPath() + "Sounds";
        }

        PythonCommand(string.Format(@"scene.addAssetPath('seq', '{0}')", pySeqPath));

        PythonCommand(string.Format(@"scene.setDoubleAttribute('scale', {0})", initSettings.scale));

        m_positionScaleHack = initSettings.positionScaleHack;

        if (!string.IsNullOrEmpty(initSettings.mediaPath))
        {
            PythonCommand(string.Format(@"scene.setMediaPath('{0}')", initSettings.mediaPath));
        }


        LoadAssetPaths(initSettings.assetPaths);


        // locomotion/steering currently only working on some platforms  (can't find pprAI lib)
        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.IPhonePlayer)
        {
            PythonCommand(string.Format(@"scene.getSteerManager().setDoubleAttribute('gridDatabaseOptions.gridSizeX', {0})", initSettings.steerGridSizeX));
            PythonCommand(string.Format(@"scene.getSteerManager().setDoubleAttribute('gridDatabaseOptions.gridSizeZ', {0})", initSettings.steerGridSizeZ));
            PythonCommand(string.Format(@"scene.getSteerManager().setIntAttribute('gridDatabaseOptions.maxItemsPerGridCell', {0})", initSettings.steerMaxItemsPerGridCell));

            // Toggle the steering manager
            PythonCommand(string.Format(@"scene.getSteerManager().setEnable(False)"));
            PythonCommand(string.Format(@"scene.getSteerManager().setEnable(True)"));
        }


        SmartbodyDiphoneDefault.Init(this);


        for (int i = 0; i < initSettings.initialCommands.Count; i++)
        {
            if (!String.IsNullOrEmpty(initSettings.initialCommands[i]))
            {
                PythonCommand(initSettings.initialCommands[i]);
            }
        }

        initSettings.TriggerPostLoadEvent();
    }


    public virtual void CreateDiphone(string fromPhoneme, string toPhoneme, string name)
    {
        // cache the names to help with manually converting the python generated code
        m_diphonePrevFromPhoneme = fromPhoneme;
        m_diphonePrevToPhoneme = toPhoneme;
        m_diphonePrevName = name;

        SmartbodyExternals.SBDiphoneManager_CreateDiphone(m_ID, fromPhoneme, toPhoneme, name);
    }


    public virtual void AddDiphoneKey(string viseme, double time, double weight)
    {
        // made function parameter double to help with manually converting the python generated code.  converting here.
        float timeF = (float)time;
        float weightF = (float)weight;

        SmartbodyExternals.SBDiphone_AddKey(m_ID, m_diphonePrevFromPhoneme, m_diphonePrevToPhoneme, m_diphonePrevName, viseme, timeF, weightF);
    }


    public virtual void AddFaceDefinition(SmartbodyFaceDefinition face)
    {
        // don't add the face definition if it's already added.
        // TODO - remove m_faceDefinition once we're able to ask smartbody directly
        if (m_faceDefinitions.ContainsKey(face.definitionName))
        {
            return;
        }

        string message;

        // _default_ is already created by smartbody at startup
        if (face.definitionName != "_default_")
        {
            message = string.Format(@"scene.createFaceDefinition('{0}')", face.definitionName);
            PythonCommand(message);
        }

        message = string.Format(@"scene.getFaceDefinition('{0}').setFaceNeutral('{1}')", face.definitionName, face.neutral);
        PythonCommand(message);

        foreach (var au in face.actionUnits)
        {
            message = string.Format(@"scene.getFaceDefinition('{0}').setAU({1}, '{2}', '{3}')", face.definitionName, au.au, au.side, au.name);
            PythonCommand(message);
        }

        foreach (var viseme in face.visemes)
        {
            message = string.Format(@"scene.getFaceDefinition('{0}').setViseme('{1}', '{2}')", face.definitionName, viseme.Key, viseme.Value);
            PythonCommand(message);
        }

        m_faceDefinitions.Add(face.definitionName, face);
    }


    public virtual void AddGestureMapDefinition(GestureMapDefinition gestureMap)
    {
        // don't add the gestureMap definition if it's already added.
        // TODO - remove m_gestureMapDefinitions once we're able to ask smartbody directly
        if (m_gestureMapDefinitions.ContainsKey(gestureMap.gestureMapName))
        {
            return;
        }

        string message;

        message = string.Format(@"scene.getGestureMapManager().createGestureMap('{0}')", gestureMap.gestureMapName);
        PythonCommand(message);

        foreach (var map in gestureMap.gestureMaps)
        {
            message = string.Format(@"scene.getGestureMapManager().getGestureMap('{0}').addGestureMapping('{1}', '{2}', '{3}', '{4}', '{5}', '{6}')", gestureMap.gestureMapName, map.animName, map.lexeme, map.type, map.hand, map.style, map.parentPosture);
            PythonCommand(message);
        }

        m_gestureMapDefinitions.Add(gestureMap.gestureMapName, gestureMap);
    }


    public void AddJointMap(SmartbodyJointMap jointMap)
    {
        if (!jointMap.enabled)
            return;

        // don't add the joint map if it's already added.
        // TODO - remove once we're able to ask smartbody directly
        if (IsJointMapLoaded(jointMap))
        {
            return;
        }

        PythonCommand(string.Format("scene.getJointMapManager().createJointMap('{0}')", jointMap.mapName));

        foreach (var joint in jointMap.mappings)
        {
            PythonCommand(string.Format("scene.getJointMapManager().getJointMap('{0}').setMapping('{1}', '{2}')", jointMap.mapName, joint.Key, joint.Value));
        }

        m_jointMaps.Add(jointMap.mapName, jointMap);
    }


    public bool IsJointMapLoaded(SmartbodyJointMap jointMap)
    {
        // TODO - replace once we're able to ask smartbody directly
        if (m_jointMaps.ContainsKey(jointMap.mapName))
            return true;
        else
            return false;
    }


    public void ApplySkeletonToJointMap(SmartbodyJointMap jointMap, string skeletonName)
    {
        if (!jointMap.enabled)
            return;

        if (!IsJointMapLoaded(jointMap))
        {
            Debug.LogError(string.Format("ApplySkeletonToJointMap() - ERROR - jointMap not loaded - {0}", jointMap.mapName));
            return;
        }

        PythonCommand(string.Format(@"scene.getJointMapManager().getJointMap('{0}').applySkeleton(scene.getSkeleton('{1}'))", jointMap.mapName, skeletonName));
    }


    public void CreateRetargetPair(string sourceSkeletonName, string destSkeletonName)
    {
        // set up online retargeting

        if (sourceSkeletonName == destSkeletonName)
            return;

        // TODO - replace once we're able to ask smartbody directly
        string retargetPairStringKey = sourceSkeletonName + "-" + destSkeletonName;
        if (m_retargetPairs.Contains(retargetPairStringKey))
            return;

        SmartbodyManager sbm = SmartbodyManager.Get();

        sbm.PythonCommand(string.Format(@"endJoints = StringVec()"));
        sbm.PythonCommand(string.Format(@"endJoints.append('l_ankle')"));
        sbm.PythonCommand(string.Format(@"endJoints.append('l_forefoot')"));
        sbm.PythonCommand(string.Format(@"endJoints.append('l_toe')"));
        sbm.PythonCommand(string.Format(@"endJoints.append('l_wrist')"));
        sbm.PythonCommand(string.Format(@"endJoints.append('r_ankle')"));
        sbm.PythonCommand(string.Format(@"endJoints.append('r_forefoot')"));
        sbm.PythonCommand(string.Format(@"endJoints.append('r_toe')"));
        sbm.PythonCommand(string.Format(@"endJoints.append('r_wrist')"));

        sbm.PythonCommand(string.Format(@"relativeJoints = StringVec()"));
        sbm.PythonCommand(string.Format(@"relativeJoints.append('spine1')"));
        sbm.PythonCommand(string.Format(@"relativeJoints.append('spine2')"));
        sbm.PythonCommand(string.Format(@"relativeJoints.append('spine3')"));
        sbm.PythonCommand(string.Format(@"relativeJoints.append('spine4')"));
        sbm.PythonCommand(string.Format(@"relativeJoints.append('spine5')"));
        sbm.PythonCommand(string.Format(@"relativeJoints.append('r_sternoclavicular')"));
        sbm.PythonCommand(string.Format(@"relativeJoints.append('l_sternoclavicular')"));
        sbm.PythonCommand(string.Format(@"relativeJoints.append('r_acromioclavicular')"));
        sbm.PythonCommand(string.Format(@"relativeJoints.append('l_acromioclavicular')"));

        // TODO: Check to make sure it hasn't already been created
        sbm.PythonCommand(string.Format(@"scene.getRetargetManager().createRetarget('{0}', '{1}')", sourceSkeletonName, destSkeletonName));
        sbm.PythonCommand(string.Format(@"scene.getRetargetManager().getRetarget('{0}', '{1}').initRetarget(endJoints, relativeJoints)", sourceSkeletonName, destSkeletonName));

        m_retargetPairs.Add(retargetPairStringKey);
    }


    bool IsIctSkeleton(Transform rootJoint)
    {
        if (rootJoint == null)
        {
            return false;
        }

        Transform curr = rootJoint;
        while (curr)
        {
            if (curr.GetComponent<UnitySmartbodyCharacter>() != null)
            {
                return VHUtils.FindChildRecursive(curr.gameObject, ICTSettingsNode) != null;
            }

            curr = curr.parent;
        }

        return false;
    }

    public void CreateSkeleton(string skeletonName, Transform root, bool loadAllChannels)
    {
        CreateSkeleton(skeletonName, root, loadAllChannels, null);
    }

    public void CreateSkeleton(string skeletonName, Transform root, bool loadAllChannels, List<string> blendShapes)
    {
        if (string.IsNullOrEmpty(skeletonName))
        {
            Debug.LogError(string.Format("CreateSkeleton failed because no skeletonName was given"));
            return;
        }

        if (m_loadedSkeletons.Contains(skeletonName) || !m_loadedSkeletons.TrueForAll(s => s.IndexOf(skeletonName) == -1))
        {
            //Debug.LogWarning(string.Format("Skeleton {0} is already loaded", skeletonName));
            return;
        }

        if (root == null)
        {
            Debug.LogError(string.Format("CreateSkeleton failed because root is null"));
            return;
        }

        PythonCommand(string.Format(@"scene.getAssetManager().addSkeletonDefinition('{0}')", skeletonName));

        Stack<Transform> joints = new Stack<Transform>();
        joints.Push(root);

        while (joints.Count > 0)
        {
            Transform currJoint = joints.Pop();
            string parent = currJoint == root ? string.Empty : currJoint.parent.name;
            SmartbodyAttributes attributes = currJoint.GetComponent<SmartbodyAttributes>();
            bool isStaticJoint = false;

            // we do this check so that retargetting works without any rotation bug and
            // also so that characters without any smartbody attributes still correctly animate
            if (IsIctSkeleton(root))
            {
                if (attributes == null || !attributes.HasChannels)
                    isStaticJoint = true;
            }
            else
            {
                if (attributes != null && !attributes.HasChannels)
                    isStaticJoint = true;
            }

            if (loadAllChannels)
                isStaticJoint = false;

            AddJoint(skeletonName, currJoint.name, parent, currJoint.localPosition, currJoint.localRotation, isStaticJoint);

            for (int i = 0; i < currJoint.childCount; i++)
            {
                joints.Push(currJoint.GetChild(i));
            }
        }

        // add joints for blend shapes
        if (blendShapes != null)
        {
            for (int i = 0; i < blendShapes.Count; i++)
            {
                AddJoint(skeletonName, blendShapes[i], root.name, Vector3.zero, Quaternion.identity, false);
            }
        }

        PythonCommand(string.Format(@"scene.getAssetManager().getSkeleton('{0}').update()", skeletonName));

        m_loadedSkeletons.Add(skeletonName);

        Debug.Log(string.Format("Created skeleton: {0}", skeletonName));
    }


    void AddJoint(string skeletonName, string jointName, string parentName, Vector3 offset, Quaternion preRot, bool isStatic)
    {
        string pythonFunctionName = isStatic ? "createStaticJoint" : "createJoint";
        if (string.IsNullOrEmpty(parentName))
        {
            PythonCommand(string.Format(@"scene.getAssetManager().getSkeleton('{0}').{1}('{2}', None)", skeletonName, pythonFunctionName, jointName));
        }
        else
        {
            PythonCommand(string.Format(@"scene.getAssetManager().getSkeleton('{0}').{1}('{2}', scene.getAssetManager().getSkeleton('{3}').getJointByName('{4}'))", skeletonName, pythonFunctionName, jointName, skeletonName, parentName));
        }

        PythonCommand(string.Format(@"scene.getAssetManager().getSkeleton('{0}').getJointByName('{1}').setOffset(SrVec({2}, {3}, {4}))", skeletonName, jointName, -offset.x, offset.y, offset.z));
        PythonCommand(string.Format(@"scene.getAssetManager().getSkeleton('{0}').getJointByName('{1}').setPrerotation(SrQuat({2}, {3}, {4}, {5}))", skeletonName, jointName, preRot.w, preRot.x, -preRot.y, -preRot.z));
    }


    public virtual bool IsSkeletonLoaded(string skeletonName)
    {
        // replace with smartbody function, once we're able to ask directly
        if (m_loadedSkeletons.Contains(skeletonName) || !m_loadedSkeletons.TrueForAll(s => s.IndexOf(skeletonName) == -1))
            return true;
        else
            return false;
    }


    public virtual bool CreateMotion(string motionName)
    {
        if (m_loadedMotions.Contains(motionName))
        {
            // motion already loaded
            return false;
        }

        m_loadedMotions.Add(motionName);
        PythonCommand(string.Format(@"scene.createMotion('{0}')", motionName));
        return true;
    }


    public virtual string GetMotionChannelMappingJointName(string jointName, string skeletonMap)
    {
        string ret = jointName;

        if (!string.IsNullOrEmpty(skeletonMap))
        {
            StringBuilder mappedName = new StringBuilder(256);
            SmartbodyExternals.SBJointMap_GetMapTarget(m_ID, skeletonMap, jointName, mappedName, 256);
            string mappedNameString = mappedName.ToString();
            if (!string.IsNullOrEmpty(mappedNameString))
            {
                ret = mappedNameString;
            }
        }

        return ret;
    }


    public virtual void AddMotionChannel(string motionName, string channelName)
    {
        AddMotionChannel(motionName, channelName, null);
    }


    public virtual void AddMotionChannel(string motionName, string channelName, string skeletonMap)
    {
        string jointName = channelName.Split(' ')[0].Trim();
        string channel = channelName.Split(' ')[1].Trim();

        jointName = UnitySmartbodyCharacter.FixJointName(jointName);

        string mappedJointName = GetMotionChannelMappingJointName(jointName, skeletonMap);  // will return jointName if mapping isn't found

        SmartbodyExternals.SBMotion_AddChannel(m_ID, motionName, mappedJointName, channel);
    }


    public virtual void AddMotionChannels(string motionName, List<string> channelNames, string skeletonMap)
    {
        IntPtr [] jointNames = new IntPtr [channelNames.Count];
        IntPtr [] channels = new IntPtr [channelNames.Count];

        for (int i = 0; i < channelNames.Count; i++)
        {
            string jointName = channelNames[i].Split(' ')[0].Trim();
            string channel = channelNames[i].Split(' ')[1].Trim();

            jointName = UnitySmartbodyCharacter.FixJointName(jointName);

            string mappedJointName = GetMotionChannelMappingJointName(jointName, skeletonMap);  // will return jointName if mapping isn't found

            jointNames[i] = SmartbodyExternals.GetStringIntPtr(mappedJointName);
            channels[i] = SmartbodyExternals.GetStringIntPtr(channel);
        }

        SmartbodyExternals.SBMotion_AddChannels(m_ID, motionName, jointNames, channels);
    }


    public virtual void AddMotionFrame(string motionName, float frameTime, float[] frameData)
    {
        IntPtr unmanagedPointer = Marshal.AllocHGlobal(frameData.Length * sizeof(float));
        Marshal.Copy(frameData, 0, unmanagedPointer, frameData.Length);

        SmartbodyExternals.SBMotion_AddFrame(m_ID, motionName, frameTime, unmanagedPointer, frameData.Length);

        Marshal.FreeHGlobal(unmanagedPointer);
    }


    public virtual void AddMotionSyncPoint(string motionName, string syncPointName, float time)
    {
        string syncPointNameSbm = ConvertSkmSyncNameToSbmSyncName(syncPointName);
        SmartbodyExternals.SBMotion_SetSyncPoint(m_ID, motionName, syncPointNameSbm, time);
    }


    public void ApplyMotion(string motionName, string skeletonMap)
    {
        if (string.IsNullOrEmpty(skeletonMap))
        {
            // no skeleton map applied
            return;
        }

        if (string.IsNullOrEmpty(motionName))
        {
            Debug.LogError(string.Format("SetMotionSkeleton failed because of bad parameters. motionName: {0}", motionName));
            return;
        }

        PythonCommand(string.Format(@"scene.getJointMapManager().getJointMap('{0}').applyMotion(scene.getMotion('{1}'))", skeletonMap, motionName));
    }


    public void SetMotionSkeleton(string motionName, string skeletonName)
    {
        if (string.IsNullOrEmpty(motionName) || string.IsNullOrEmpty(skeletonName))
        {
            Debug.LogError(string.Format("SetMotionSkeleton failed because of bad parameters. motionName: {0} skeletonName {1}", motionName, skeletonName));
            return;
        }
        PythonCommand(string.Format(@"scene.getMotion('{0}').setMotionSkeletonName('{1}')", motionName, skeletonName));
    }

    public void SaveMotionToSkm(string motionName)
    {
        PythonCommand(string.Format(@"scene.getMotion('{0}').saveToSkm('{1}.skm')", motionName, motionName));
    }

    public void CreateSkeletons()
    {
        UnitySmartbodyCharacter[] characters = FindObjectsOfType<UnitySmartbodyCharacter>();
        foreach (UnitySmartbodyCharacter character in characters)
        {
            character.CreateSkeleton();
        }
    }

    public void SaveAllMotions(string path)
    {
        // save all loaded motions to disk as .skm's.
        // folder specified by path will be removed along with everything in it
        // note we have to set the working directory to path because if you specify a path in saveToSkm(), the path gets embedded in the motion name
        // also note we save the file without extension, for the same reason.  Then we rename it afterwards.

        if (Directory.Exists(path))
            VHFile.DirectoryWrapper.Delete(path, true);

        Directory.CreateDirectory(path);

        Debug.Log("SaveAllMotions: " + path);

        string currentWorkingDirectory = Directory.GetCurrentDirectory();

        Directory.SetCurrentDirectory(path);

        SmartbodyMotion[] allMotions = FindObjectsOfType<SmartbodyMotion>();
        foreach (SmartbodyMotion motion in allMotions)
        {
            PythonCommand(string.Format(@"scene.getMotion('{0}').saveToSkm('{1}')", motion.MotionName, motion.MotionName));
            if (File.Exists(motion.MotionName))
            {
                VHFile.FileWrapper.Move(motion.MotionName, motion.MotionName + ".skm");
            }
            else
            {
                File.Create(motion.MotionName + ".skm");
            }
        }
        /*foreach (var motion in m_loadedMotions)
        {
            PythonCommand(string.Format(@"scene.getMotion('{0}').saveToSkm('{1}')", motion, motion));
            File.Move(motion, motion + ".skm");
        }*/

        Directory.SetCurrentDirectory(currentWorkingDirectory);
    }


    //public void CreateMotion(TextAsset motionData, bool stream)
    //{
    //    StartCoroutine(CreateMotionCoroutine(motionData, stream, null));
    //}


    //public void CreateMotion(TextAsset motionData, bool stream, string skeletonMap)
    //{
    //    StartCoroutine(CreateMotionCoroutine(motionData, stream, skeletonMap));
    //}


    //IEnumerator CreateMotionCoroutine(TextAsset motionData, bool stream, string skeletonMap)
    //{
    //    DateTime prevDT = DateTime.Now;
    //    PythonCommand(string.Format(@"motion = scene.createMotion(""{0}"")", motionData.name));

    //    string[] fileLines = motionData.text.Split('\n');
    //    string line = "";
    //    bool readingChannels = false;
    //    bool readingFrames = false;
    //    string keyTimeStr = "";

    //    for (int i = 0; i < fileLines.Length; i++)
    //    {
    //        line = fileLines[i];
    //        line = line.Trim();

    //        if (!readingChannels && !readingFrames && line.Contains("channels"))
    //        {
    //            readingChannels = true;
    //        }
    //        else if (!readingChannels && !readingFrames && line.Contains("frames"))
    //        {
    //            readingChannels = false;
    //            readingFrames = true;
    //        }
    //        else if (readingChannels)
    //        {
    //            if (string.IsNullOrEmpty(line))
    //            {
    //                readingChannels = false;
    //            }
    //            else
    //            {
    //                string[] jointAndChannel = line.Split(' ');
    //                if (!string.IsNullOrEmpty(skeletonMap))
    //                {
    //                    //Debug.Log("added channel: " + jointAndChannel[0].Trim());
    //                    string mappedName = PythonCommand<string>(string.Format(@"scene.getJointMapManager().getJointMap(""{0}"").getMapTarget(""{1}"")", skeletonMap, jointAndChannel[0].Trim()));
    //                    if (string.IsNullOrEmpty(mappedName))
    //                    {
    //                        // couldn't find the mapped joint, so just use the regular name instead
    //                        PythonCommand(string.Format(@"motion.addChannel(""{0}"", ""{1}"")", jointAndChannel[0].Trim(), jointAndChannel[1]));
    //                    }
    //                    else
    //                    {
    //                        PythonCommand(string.Format(@"motion.addChannel({0}, ""{1}"")",
    //                            string.Format(@"scene.getJointMapManager().getJointMap(""{0}"").getMapTarget(""{1}"")", skeletonMap, jointAndChannel[0].Trim()), jointAndChannel[1].Trim()));
    //                    }
    //                }
    //                else
    //                {
    //                    PythonCommand(string.Format(@"motion.addChannel(""{0}"", ""{1}"")", jointAndChannel[0].Trim(), jointAndChannel[1]));
    //                }
    //            }
    //        }
    //        else if (readingFrames)
    //        {
    //            int index = line.IndexOf("fr");
    //            if (index == -1 || string.IsNullOrEmpty(line))
    //            {
    //                readingFrames = false;
    //            }
    //            else
    //            {
    //                string frameHeaderInfo = line.Substring(0, index + 1); // kt [time] fr
    //                keyTimeStr = frameHeaderInfo.Split(' ')[1]; //[time]

    //                line = line.Remove(0, index + 3);
    //                string[] channelData = line.Split(' ');

    //                StringBuilder builder = new StringBuilder();
    //                builder.AppendLine("data = FloatVec()");
    //                // parse key frame info
    //                foreach (string data in channelData)
    //                {
    //                    builder.AppendLine(string.Format("data.append({0})", data));
    //                }

    //                PythonCommand(string.Format(@"{0}motion.addFrame({1}, data)", builder.ToString(), keyTimeStr));
    //            }
    //        }
    //        else if (!readingChannels && !readingFrames && line.Contains("time"))
    //        {
    //            string[] syncNameAndTime = line.Split(':');
    //            PythonCommand(string.Format(@"motion.setSyncPoint(""{0}"", {1})", ConvertSkmSyncNameToSbmSyncName(syncNameAndTime[0].Trim()), syncNameAndTime[1].Trim()));
    //        }

    //        if (stream)
    //        {
    //            yield return new WaitForEndOfFrame();
    //        }
    //    }

    //    PythonCommand(string.Format(@"motion.setSyncPoint(""{0}"", {1})","start", 0));
    //    PythonCommand(string.Format(@"motion.setSyncPoint(""{0}"", {1})", "stop", keyTimeStr));

    //    Debug.Log(string.Format("Motion {0} finished loading in {1} seconds", motionData.name, (DateTime.Now - prevDT).TotalSeconds.ToString("f3")));
    //}


    public static string ConvertSkmSyncNameToSbmSyncName(string skmSyncPointName)
    {
        string retVal = "";
        if (skmSyncPointName.IndexOf("emphasis") != -1)
        {
            retVal = "stroke";
        }
        else if (skmSyncPointName.IndexOf("ready") != -1)
        {
            retVal = "ready";
        }
        else if (skmSyncPointName.IndexOf("relax") != -1)
        {
            retVal = "relax";
        }
        else if (skmSyncPointName.IndexOf("strokeStart") != -1)
        {
            retVal = "stroke_start";
        }
        else if (skmSyncPointName.IndexOf("stroke") != -1)
        {
            retVal = "stroke_stop";
        }
        else if (skmSyncPointName.IndexOf("start") != -1)
        {
            retVal = "start";
        }
        else if (skmSyncPointName.IndexOf("stop") != -1)
        {
            retVal = "stop";
        }
        return retVal;
    }


    public void MapSkeletonAndAssetPaths(SmartbodyJointMap jointMap, string skeletonName, List<KeyValuePair<string, string>> assetPaths)
    {
        foreach (var path in assetPaths)
        {
            string[] files = VHFile.GetStreamingAssetsFiles(path.Value, ".skm");
            foreach (var file in files)
            {
                string motionName = Path.GetFileNameWithoutExtension(file);
                PythonCommand(string.Format("scene.getJointMapManager().getJointMap('{0}').applyMotion(scene.getMotion('{1}'))", jointMap.mapName, motionName));
            }
        }
    }


    public virtual void Shutdown()
    {
        if (m_ID == new IntPtr(-1))
        {
            return;
        }

        if (SmartbodyExternals.Shutdown(m_ID))
        {
            Debug.Log("SmartbodyManager successfully shutdown");
        }
        else
        {
            Debug.LogError("SmartbodyManager failed to shutdown");
        }

        SmartbodyExternals.FreeLibraries();

        m_ID = new IntPtr(-1);

        m_startCalled = false;

        //System.Diagnostics.Process p;
        //p.Close();
        //System.Diagnostics.Process.g("vhwrapper");
    }


    public void AddCustomCharCreateCB(OnCustomCharacterCallback cb) { m_CustomCreateCBs += cb; }
    public void RemoveCustomCharCreateCB(OnCustomCharacterCallback cb) { m_CustomCreateCBs -= cb; }
    public void AddCustomCharDeleteCB(OnCustomCharacterCallback cb) { m_CustomDeleteCBs += cb; }
    public void RemoveCustomCharDeleteCB(OnCustomCharacterCallback cb) { m_CustomDeleteCBs -= cb; }
    public void RemoveCustomCallbacks()
    {
        m_CustomCreateCBs = null;
        m_CustomDeleteCBs = null;
    }


    public virtual void LoadAssetPaths(List<KeyValuePair<string, string>> assetPaths)
    {
        // TODO - need this code path for bonebus
#if false
        if (initSettings.assetPaths != null)
        {
            foreach (var pair in initSettings.assetPaths)
            {
                message = string.Format(@"scene.addAssetPath('{0}', '{1}')", pair.Key, pair.Value);
                PythonCommand(message);
            }
        }

        message = string.Format(@"scene.loadAssets()");
        PythonCommand(message);
#endif
        if (m_debugQuickLoadNoMotions)
            return;

        if (assetPaths != null)
        {
            foreach (var pair in assetPaths)
            {
                string skeletonName = pair.Key;
                string pathName = pair.Value;

                // load all .sk's
                {
                    string[] files = VHFile.GetStreamingAssetsFiles(pathName, ".sk");
                    foreach (var file in files)
                    {
                        // don't load the skeleton if it's already added.
                        // TODO - remove once we're able to ask smartbody directly
                        if (m_loadedSkeletons.Contains(file) || m_loadedSkeletons.Contains(Path.GetFileName(file)))
                        {
                            //Debug.LogWarning("skeleton already loaded! " + file);
                            continue;
                        }

                        //Debug.Log("LoadAssetPaths() - Loading '" + file + "'");

                        byte[] skeletonByte = VHFile.LoadStreamingAssetsBytes(file);
                        if (skeletonByte != null)
                        {
                            IntPtr unmanagedPointer = Marshal.AllocHGlobal(skeletonByte.Length);
                            Marshal.Copy(skeletonByte, 0, unmanagedPointer, skeletonByte.Length);

                            SmartbodyExternals.SBAssetManager_LoadSkeleton(m_ID, unmanagedPointer, skeletonByte.Length, Path.GetFileName(file));

                            Marshal.FreeHGlobal(unmanagedPointer);

                            m_loadedSkeletons.Add(file);
                        }
                        else
                        {
                            Debug.Log("LoadSkeleton() fail - " + file);
                        }
                    }
                }

                // load all .skm's
                {
                    string[] files = VHFile.GetStreamingAssetsFiles(pathName, ".skm");
                    foreach (var file in files)
                    {
                        string motionName = Path.GetFileNameWithoutExtension(file);

                        // don't load the motion if it's already added.
                        // TODO - remove once we're able to ask smartbody directly
                        if (m_loadedMotions.Contains(motionName))
                        {
                            continue;
                        }

                        //Debug.Log("LoadAssetPaths() - Loading '" + file + "'");

                        byte[] motionByte = VHFile.LoadStreamingAssetsBytes(file);
                        if (motionByte != null)
                        {
                            IntPtr unmanagedPointer = Marshal.AllocHGlobal(motionByte.Length);
                            Marshal.Copy(motionByte, 0, unmanagedPointer, motionByte.Length);

                            SmartbodyExternals.SBAssetManager_LoadMotion(m_ID, unmanagedPointer, motionByte.Length, Path.GetFileName(file));

                            Marshal.FreeHGlobal(unmanagedPointer);

                            PythonCommand(string.Format(@"scene.getMotion('{0}').setMotionSkeletonName('{1}')", motionName, skeletonName));

                            m_loadedMotions.Add(motionName);

                        }
                        else
                        {
                            Debug.Log("LoadMotion() fail - " + file);
                        }
                    }
                }
            }
        }
    }


    public virtual void CreateCharacter(UnitySmartbodyCharacter unityCharacter)
    {
        // should only be called by UnitySmartbodyCharacter

        UnitySmartbodyCharacter existingCharacter = GetCharacterBySBMName(unityCharacter.SBMCharacterName);
        if (existingCharacter != null)
        {
            Debug.LogError(string.Format("ERROR - SmartbodyManager.CreateCharacter() - character '{0}' already exists.  Smartbody character names must be unique", unityCharacter.SBMCharacterName));
            return;
        }

        SmartbodyExternals.InitCharacter(m_ID, unityCharacter.SBMCharacterName, ref unityCharacter.m_CharacterData.m_Character);
        m_characterList.Add(unityCharacter);

        if (m_CustomCreateCBs != null && unityCharacter != null)
        {
            m_CustomCreateCBs(unityCharacter);
        }
    }


    public void RemoveCharacter(UnitySmartbodyCharacter character)
    {
        // should only be called by UnitySmartbodyCharacter

        //Debug.Log("SmartbodyManager.RemoveCharacter()");

        if (m_CustomDeleteCBs != null)
        {
            m_CustomDeleteCBs(character);
        }

        m_characterList.Remove(character);
    }


    public void AddPawn(SmartbodyPawn pawn)
    {
        // should only be called by SmartbodyPawn

        m_pawns.Add(pawn);
    }


    public void RemovePawn(SmartbodyPawn pawn)
    {
        m_pawns.Remove(pawn);
    }


    public void RefreshInit()
    {
        // this function should only be called if smartbody needs to be re-initialized.
        // this happens if you're using bonebus, because bonebus connects after the world is started up.
        // but might happen in other cases

        SmartbodyInit initSettings = GetComponent<SmartbodyInit>();
        if (initSettings != null)
        {
            // reset motionSets.  These are stored in the character
            UnitySmartbodyCharacter[] sbmCharacters = (UnitySmartbodyCharacter[])Component.FindObjectsOfType(typeof(UnitySmartbodyCharacter));
            if (sbmCharacters != null)
            {
                foreach (var character in sbmCharacters)
                {
                    character.ResetSkeleton();
                    SmartbodyCharacterInit characterInit = character.GetComponent<SmartbodyCharacterInit>();   // ok if it's null
                    if (characterInit)
                    {
                        foreach (var motionSet in characterInit.m_MotionSets)
                        {
                            if (motionSet && motionSet.gameObject.activeSelf)
                            {
                                motionSet.ResetLoadFlag();
                            }
                        }
                    }
                }
            }

            Init(initSettings);
        }
    }


    public void RefreshPawns()
    {
        // this function should only be called if smartbody's representation of the number of pawns is different from Unity's.
        // this happens if you're using bonebus, because bonebus connects after the world is started up.
        // but might happen in other cases

        // remove all pawns first
        PythonCommand(@"scene.command('sbm pawn * remove')");
        m_pawns.Clear();

        SmartbodyPawn[] sbmPawns = (SmartbodyPawn[])Component.FindObjectsOfType(typeof(SmartbodyPawn));
        if (sbmPawns != null)
        {
            for (int i = 0; i < sbmPawns.Length; i++)
            {
                sbmPawns[i].AddToSmartbody();
            }
        }
    }


    public void RefreshCharacters()
    {
        // this function should only be called if smartbody's representation of the number of characters is different from Unity's.
        // this happens if you're using bonebus, because bonebus connects after the world is started up.
        // but might happen in other cases

        // remove all characters first
        PythonCommand(@"scene.removeAllCharacters()");
        m_characterList.Clear();

        UnitySmartbodyCharacter[] sbmCharacters = (UnitySmartbodyCharacter[])Component.FindObjectsOfType(typeof(UnitySmartbodyCharacter));
        if (sbmCharacters != null)
        {
            for (int i = 0; i < sbmCharacters.Length; i++)
            {
                SmartbodyCharacterInit init = sbmCharacters[i].GetComponent<SmartbodyCharacterInit>();
                if (init != null)
                {
                    SmartbodyFaceDefinition face = sbmCharacters[i].GetComponent<SmartbodyFaceDefinition>();   // ok if it's null
                    SmartbodyJointMap jointMap = sbmCharacters[i].GetComponent<SmartbodyJointMap>();   // ok if it's null
                    GestureMapDefinition gestureMap = GetComponent<GestureMapDefinition>();   // ok if it's null
                    sbmCharacters[i].CreateCharacter(init, face, jointMap, gestureMap);
                }
            }
        }
    }


    public void RemoveAllSBObjects()
    {
        PythonCommand(@"scene.removeAllCharacters()");
        PythonCommand(@"scene.removeAllPawns()");
        m_characterList.Clear();
        m_pawns.Clear();
    }


    public void SetTime(float time)
    {
        SmartbodyExternals.Update(m_ID, time);
    }


    public virtual void SetProcessId(string id)
    {
        PythonCommand(string.Format(@"scene.setProcessId('{0}')", id));

        SmartbodyExternals.SBDebuggerServer_SetID(m_ID, id);
    }

    public virtual void ProcessVHMsgs(string opCode, string parsedArgs)
    {
        if (!gameObject.activeSelf)
            return;

        //Debug.Log("SmartbodyManager.ProcessVHMsgs() - " + opCode + " " + parsedArgs);

        SmartbodyExternals.ProcessVHMsgs(m_ID, opCode, parsedArgs);
    }

    protected void InitConsole()
    {
        DebugConsole console = DebugConsole.Get();
        if (console == null)
        {
            Debug.LogWarning("There is no DebugConsole in the scene and SmartbodyManager is trying to use one");
            return;
        }
        console.AddCommandCallback("python", PythonCallback);
    }

    public void PythonCallback(string commandEntered, DebugConsole console)
    {
        if (commandEntered.IndexOf(PythonCmd) != -1)
        {
            string returnType = string.Empty;
            string pythonCommand = string.Empty;
            if (console.ParseVHMSG(commandEntered, ref returnType, ref pythonCommand))
            {
                // so I don't have to call tolower every check
                string ret = returnType.ToLower();

                // if the command has no return type, the number of arguements varies since you don't have to type 'void'
                if (string.IsNullOrEmpty(pythonCommand))
                    PythonCommand(returnType);
                else if (ret == "bool")
                    Debug.Log(PythonCommand<bool>(pythonCommand));
                else if (ret == "int")
                    Debug.Log(PythonCommand<int>(pythonCommand));
                else if (ret == "float")
                    Debug.Log(PythonCommand<float>(pythonCommand));
                else if (ret == "string")
                    Debug.Log(PythonCommand<string>(pythonCommand));
            }
        }
    }

    protected void SubscribeVHMsg()
    {
        VHMsgBase vhmsg = VHMsgBase.Get();
        if (vhmsg == null)
        {
            Debug.LogWarning("There is no VHMsgBase in the scene and SmartbodyManager is trying to use one");
            return;
        }

        vhmsg.AddMessageEventHandler(new VHMsgBase.MessageEventHandler(MessageAction));

        // sbm related vhmsgs
        vhmsg.SubscribeMessage("vrExpress");
        vhmsg.SubscribeMessage("vrSpeak");
        vhmsg.SubscribeMessage("vrSpeech");
        vhmsg.SubscribeMessage("vrSpoke");
        vhmsg.SubscribeMessage("RemoteSpeechReply");
        vhmsg.SubscribeMessage("PlaySound");
        vhmsg.SubscribeMessage("StopSound");
        vhmsg.SubscribeMessage("unity");
        vhmsg.SubscribeMessage("sb");
        vhmsg.SubscribeMessage("sbm");
        vhmsg.SubscribeMessage("sbmdebugger");
        vhmsg.SubscribeMessage("vrPerception");
        vhmsg.SubscribeMessage("vrAgentBML");


        // turn on vhmsg on the smartbody dll side
        VHMsgManager vhmsgManager = (VHMsgManager)vhmsg;
        string vhmsgServer = vhmsgManager.m_Host;
        string vhmsgScope = vhmsgManager.m_Scope;
        string vhmsgPort = vhmsgManager.m_Port;

        if (!string.IsNullOrEmpty(vhmsgServer)) SmartbodyExternals.SBVHMsgManager_SetServer(m_ID, vhmsgServer);
        if (!string.IsNullOrEmpty(vhmsgScope)) SmartbodyExternals.SBVHMsgManager_SetScope(m_ID, vhmsgScope);
        if (!string.IsNullOrEmpty(vhmsgPort)) SmartbodyExternals.SBVHMsgManager_SetPort(m_ID, vhmsgPort);

        SmartbodyExternals.SBVHMsgManager_SetEnable(m_ID, true);
    }

    protected void MessageAction(object sender, VHMsgBase.Message args)
    {
        var split = VHMsgBase.SplitIntoOpArg(args.s);
        ProcessVHMsgs(split.Key, split.Value);
    }

    bool GetUnityCharacterData(string characterName, ref UnitySmartbodyCharacter.UnityCharacterData characterData)
    {
        try
        {
            // pass the character struct to native code and have it filled out
            if (!SmartbodyExternals.GetCharacter(m_ID, characterName, ref characterData.m_Character))
            {
                // something went bad in native code
                Debug.Log("Couldn't update character " + characterName);
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        if (characterData.jx == null || characterData.jx.Length != characterData.m_Character.m_numJoints)
        {
            characterData.jnames = new IntPtr[characterData.m_Character.m_numJoints];
            characterData.jx = new float[characterData.m_Character.m_numJoints];
            characterData.jy = new float[characterData.m_Character.m_numJoints];
            characterData.jz = new float[characterData.m_Character.m_numJoints];
            characterData.jrw = new float[characterData.m_Character.m_numJoints];
            characterData.jrx = new float[characterData.m_Character.m_numJoints];
            characterData.jry = new float[characterData.m_Character.m_numJoints];
            characterData.jrz = new float[characterData.m_Character.m_numJoints];

            characterData.jprevx = new float[characterData.m_Character.m_numJoints];
            characterData.jprevy = new float[characterData.m_Character.m_numJoints];
            characterData.jprevz = new float[characterData.m_Character.m_numJoints];
            characterData.jprevrw = new float[characterData.m_Character.m_numJoints];
            characterData.jprevrx = new float[characterData.m_Character.m_numJoints];
            characterData.jprevry = new float[characterData.m_Character.m_numJoints];
            characterData.jprevrz = new float[characterData.m_Character.m_numJoints];

            characterData.jNextUpdateTime = new float[characterData.m_Character.m_numJoints];
        }

        characterData.jx.CopyTo(characterData.jprevx, 0);
        characterData.jy.CopyTo(characterData.jprevy, 0);
        characterData.jz.CopyTo(characterData.jprevz, 0);
        characterData.jrw.CopyTo(characterData.jprevrw, 0);
        characterData.jrx.CopyTo(characterData.jprevrx, 0);
        characterData.jry.CopyTo(characterData.jprevry, 0);
        characterData.jrz.CopyTo(characterData.jprevrz, 0);

        Marshal.Copy(characterData.m_Character.jname, characterData.jnames, 0, characterData.m_Character.m_numJoints);
        Marshal.Copy(characterData.m_Character.jx, characterData.jx, 0, characterData.m_Character.m_numJoints);
        Marshal.Copy(characterData.m_Character.jy, characterData.jy, 0, characterData.m_Character.m_numJoints);
        Marshal.Copy(characterData.m_Character.jz, characterData.jz, 0, characterData.m_Character.m_numJoints);
        Marshal.Copy(characterData.m_Character.jrw, characterData.jrw, 0, characterData.m_Character.m_numJoints);
        Marshal.Copy(characterData.m_Character.jrx, characterData.jrx, 0, characterData.m_Character.m_numJoints);
        Marshal.Copy(characterData.m_Character.jry, characterData.jry, 0, characterData.m_Character.m_numJoints);
        Marshal.Copy(characterData.m_Character.jrz, characterData.jrz, 0, characterData.m_Character.m_numJoints);

        return true;
    }

    public int OnCharacterCreate(IntPtr sbmID, string name, string objectClass)
    {
        //Debug.Log("OnCharacterCreate() - name: " + name + " objectClass: " + objectClass);

        if (objectClass == "pawn")
        {
            return 0;
        }
        else
        {
            return 0;
        }
    }

    public int OnCharacterDelete(IntPtr sbmID, string name)
    {
        //Debug.Log(string.Format("OnCharacterDelete() - {0}", name));

        return 0;
    }

    public int OnCharacterChange(IntPtr sbmID, string name)
    {
        //Debug.Log(string.Format("OnCharacterChange() - {0}", name));

        return 0;
    }

    public int OnViseme(IntPtr sbmID, string name, string visemeName, float weight, float blendTime)
    {
        return 0;
    }

    protected int OnChannel(IntPtr sbmID, string name, string channelName, float value)
    {
        return 0;
    }

    #region Helper Functions
    public void OnLogMessage(string message, int messageType)
    {
        //    messageType
        //    0 = Unity Normal Print Out
        //    1 = Unity Error Print Out
        //    2 = Unity Warning Print Out

        if (m_displayLogMessages)
        {
            // do some pruning first since the python errors coming from Smartbody are strangely verbose and not helpful
            message = message.TrimEnd(null);

            if (string.IsNullOrEmpty(message) ||
                message == ":" ||
                message == "^" ||
                message == "  File \"" ||
                message == "<string>" ||
                message == "\", line" ||
                message == "1")
                return;

            // attempt to flag the errors
            if (message.Contains("Error") ||
                message.Contains("ERR:") ||
                message.Contains("Exception"))
            {
                messageType = 1;
            }

            string m = "sbm: " + message;

            switch (messageType)
            {
                case 2:
                    Debug.LogWarning(m);
                    break;

                case 1:
                    Debug.LogError("<color=red>" + m + "</color>");
                    break;

                default:
                    Debug.Log(m);
                    break;
            }
        }
    }

    public void SendBmlReply(string characterName, string requestId, string utteranceId)
    {
        // this function has many paths:
        // - if app specific callback is supplied, use that instead of this function
        // - else, look for the AudioSpeechFile gameobject in the scene
        // - else, look for the .bml.txt file on disk
        // - else, look for the .bml file on disk
        // - else, send back error message so that sb can continue processing

        if (m_bmlReplyAppSpecificCallback != null)
        {
            string bmlText = m_bmlReplyAppSpecificCallback(characterName, requestId, utteranceId);
            SendBmlReply(characterName, requestId, utteranceId, bmlText);
        }
        else
        {
            AudioSpeechFile audioFile = FindSpeechAudioFile(utteranceId);

            if (audioFile == null)
            {
                // couldn't find the utterance, so try to load is from disk using our audio path
                string audioPath = VHFile.GetStreamingAssetsPath() + "Sounds";
                SmartbodyInit initSettings = GetComponent<SmartbodyInit>();
                if (initSettings != null)
                {
                    audioPath = initSettings.audioPath;
                }

                string audioFilePath = audioPath + "/" + utteranceId + ".bml.txt";
                string audioFilePathAlt = audioPath + "/" + utteranceId + ".bml";

                if (File.Exists(audioFilePath))
                {
                    string bmlText = VHFile.FileWrapper.ReadAllText(audioFilePath);
                    SendBmlReply(characterName, requestId, utteranceId, bmlText);
                }
                else if (File.Exists(audioFilePathAlt))
                {
                    string bmlText = VHFile.FileWrapper.ReadAllText(audioFilePathAlt);
                    SendBmlReply(characterName, requestId, utteranceId, bmlText);
                }
                else
                {
                    //Debug.Log("Failed to SendBmlReply for utterance: " + utteranceId + " character: " + characterName);
                    string bmlText = string.Format("ERROR - audio file '{0}' not found, trying backup voice", audioFilePath);
                    SendBmlReply(characterName, requestId, utteranceId, bmlText);
                }
            }
            else
            {
                // utterance was found
                string bmlText = audioFile.BmlText;
                SendBmlReply(characterName, requestId, utteranceId, bmlText);
            }
        }
    }

    protected virtual void SendBmlReply(string characterName, string requestId, string utteranceId, string rawBml)
    {
        SmartbodyExternals.SendBmlReply(m_ID, characterName, requestId, utteranceId, rawBml);
    }

    protected AudioSpeechFile FindSpeechAudioFile(string utteranceId)
    {
        AudioSpeechFile retval = null;

        AudioSpeechFile[] speechFiles = FindObjectsOfType<AudioSpeechFile>();
        for (int i = 0; i < speechFiles.Length; i++)
        {
            if (speechFiles[i].m_AudioClip == null)
            {
                Debug.LogWarning("There is no audio clip associated with audio speech file " + speechFiles[i].name);
                continue;
            }

            if (speechFiles[i].m_AudioClip.name == utteranceId)
            {
                retval = speechFiles[i];
                break;
            }
        }

        return retval;
    }

    /// <summary>
    /// Gets a character by the name that it is known by in unity
    /// In the seq file command, "char brad1 init common.sk data/prefabs/brad", data/prefabs/brad would be the character name
    /// </summary>
    /// <param name="name">The smartbody character's prefab name</param>
    /// <returns></returns>
    public UnitySmartbodyCharacter GetCharacterByName(string name)
    {
        for (int i = 0; i < m_characterList.Count; i++)
        {
            if (String.Compare(m_characterList[i].CharacterName, name, true) == 0)
            {
                return m_characterList[i];
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a character by the name that it is known by in smartbodydll or bonebus
    /// In the seq file command, "char brad1 init common.sk data/prefabs/brad", brad1 would be the smartbody name
    /// </summary>
    /// <param name="name">What the character is know by in smartbody</param>
    /// <returns></returns>
    public UnitySmartbodyCharacter GetCharacterBySBMName(string name)
    {
        for (int i = 0; i < m_characterList.Count; i++)
        {
            if (String.Compare(m_characterList[i].SBMCharacterName, name, true) == 0)
            {
                return m_characterList[i];
            }
        }

        return null;
    }

    public UnitySmartbodyCharacter GetCharacterByID(int id)
    {
        for (int i = 0; i < m_characterList.Count; i++)
        {
            if (m_characterList[i].CharacterID == id)
            {
                return m_characterList[i];
            }
        }

        return null;
    }

    public void ToggleDebugFlag(UnitySmartbodyCharacter.DebugFlags flag)
    {
        for (int i = 0; i < m_characterList.Count; i++)
        {
            m_characterList[i].ToggleDebugFlag(flag);
        }
    }

    public static float FindSkmLength(string fullSkmPath)
    {
        SkmMetaData metaData = FindSkmMetaData(fullSkmPath);
        return metaData != null ? metaData.Length : -1;
    }

    public static SkmMetaData FindSkmMetaData(string fullSkmPath)
    {
        SkmMetaData metaData = new SkmMetaData();

        if (string.IsNullOrEmpty(fullSkmPath) || Path.GetExtension(fullSkmPath) != ".skm")
        {
            //Debug.LogError(string.Format("Couldn't located skm file {0}", skmName));
            return metaData;
        }

        string prevLine = "";
        string line = "";
        StreamReader reader = new StreamReader(fullSkmPath);
        try
        {
            while ((line = reader.ReadLine()) != null)
            {
                if (line == "" && prevLine.IndexOf("kt") != -1)
                {
                    prevLine = prevLine.Remove(0, 3);
                    int firstSpace = prevLine.IndexOf(" ");
                    prevLine = prevLine.Substring(0, firstSpace);
                    metaData.Length = float.Parse(prevLine);
                    //break;
                }
                else
                {
                    foreach (string s in CharacterDefines.SyncPointNames)
                    {
                        if (line.IndexOf(s) != -1)
                        {
                            string[] split = line.Split(' ');
                            if (split.Length >= 2)
                            {
                                float time;
                                if (float.TryParse(split[split.Length - 1], out time))
                                {
                                    metaData.SyncPoints.Add(s, time);
                                }
                                else
                                {
                                    Debug.LogError(string.Format("Error when parsing sync point {0} on skm {1} line {2}", s, fullSkmPath, line));
                                }
                            }
                            else
                            {
                                Debug.LogError(string.Format("Error when parsing sync point {0} on skm {1} line {2}", s, fullSkmPath, line));
                            }
                            break;
                        }
                    }
                }
                prevLine = line;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("FindSkmLength exeception on file {0}. Exception: {1}", fullSkmPath, e.Message));
        }
        finally
        {
            if (reader != null)
            {
                reader.Close();
            }
        }

        return metaData;
    }
    #endregion

    protected virtual void OnSetCharacterPosition(UnitySmartbodyCharacter c, Vector3 pos)
    {
        c.transform.localPosition = pos * m_positionScaleHack;
    }

    protected virtual void OnSetCharacterRotation(UnitySmartbodyCharacter c, Quaternion rot)
    {
        c.transform.localRotation = rot;
    }

    /// <summary>
    /// Returns the world position of the specified sbm character's AudioSource componenet
    /// </summary>
    /// <param name="sbmCharName">What the chracter is called inside of sbm</param>
    /// <returns></returns>
    public Vector3 GetCharacterVoicePosition(string sbmCharName)
    {
        AudioSource voice = GetCharacterVoice(sbmCharName);
        return voice != null ? voice.transform.position : Vector3.zero;
    }

    /// <summary>
    /// Returns the characters AudioSource that is placed at their mouth
    /// </summary>
    /// <param name="sbmCharName"></param>
    /// <returns></returns>
    public AudioSource GetCharacterVoice(string sbmCharName)
    {
        UnitySmartbodyCharacter character = GetCharacterBySBMName(sbmCharName);
        if (character == null)
        {
            return null;
        }

        if (character.AudioSource == null)
        {
            Debug.LogError("GetCharacterVoice failed. " + sbmCharName + " doesn't have an AudioSource componenet");
            return null;
        }

        return character.AudioSource;
    }

    public string GetAnimFromGestureData(string sbmCharName, string lexeme, string type)
    {
        UnitySmartbodyCharacter character = GetCharacterBySBMName(sbmCharName);
        if (character == null)
        {
            return "";
        }

        GestureMapDefinition gestureMap = character.GetComponent<GestureMapDefinition>();
        if (gestureMap == null)
        {
            Debug.LogError(sbmCharName + " doesn't have a gesture map");
        }

        return gestureMap.GetAnimation(lexeme, type);
    }

    public string[] GetSBMCharacterNames()
    {
        string[] retval = new string[m_characterList.Count];
        for (int i = 0; i < retval.Length; i++)
        {
            retval[i] = m_characterList[i].SBMCharacterName;
        }
        return retval;
    }

    public string[] GetPawnNames()
    {
        string[] retval = new string[m_pawns.Count];
        for (int i = 0; i < retval.Length; i++)
        {
            retval[i] = m_pawns[i].PawnName;
        }
        return retval;
    }

    private string FormatPythonCommand(string command)
    {
        return command.Insert(0, "ret = ");
    }

    public virtual void PythonCommand(string command)
    {
        SmartbodyExternals.PythonCommandVoid(m_ID, command);
    }

    public virtual T PythonCommand<T>(string command)
    {
        T retVal = default(T);
        Type type = typeof(T);

        if (type == typeof(bool))
        {
            retVal = (T)(object)SmartbodyExternals.PythonCommandBool(m_ID, FormatPythonCommand(command));
        }
        else if (type == typeof(int))
        {
            retVal = (T)(object)SmartbodyExternals.PythonCommandInt(m_ID, FormatPythonCommand(command));
        }
        else if (type == typeof(float))
        {
            retVal = (T)(object)SmartbodyExternals.PythonCommandFloat(m_ID, FormatPythonCommand(command));
        }
        else if (type == typeof(string))
        {
            StringBuilder output = new StringBuilder(256);
            SmartbodyExternals.PythonCommandString(m_ID, FormatPythonCommand(command), output, output.Capacity);
            retVal = (T)(object)output.ToString();
        }
        else
        {
            Debug.LogError("PythonCommand failed. Only types bool, int, float, and string are currently supported."
                + " For void return types, use the other PythonCommand overload");
        }

        return retVal;
    }

    public void QueueCharacterToUpload(ICharacter character)
    {
        if (m_CharactersToUpload.Contains(character))
        {
            return;
        }

        m_CharactersToUpload.Add(character);
    }

    public void UploadCharacterTransforms()
    {
        m_CharactersToUpload.ForEach(sbmChar => UploadCharacterTransform(sbmChar));
        m_CharactersToUpload.Clear();
    }

    void UploadCharacterTransform(ICharacter sbmChar)
    {
        SBTransform(sbmChar.CharacterName, sbmChar.transform);
    }

    public void ForEachCharacter(Action<UnitySmartbodyCharacter> action)
    {
        m_characterList.ForEach(action);
    }



    #endregion

    public void SetCharacterVoice(string characterName, string voiceType, string voiceCode, bool isBackup)
    {
        if (!isBackup)
        {
            PythonCommand(string.Format(@"scene.getCharacter('{0}').setVoice('{1}')", characterName, voiceType));
            PythonCommand(string.Format(@"scene.getCharacter('{0}').setVoiceCode('{1}')", characterName, voiceCode));
        }
        else
        {
            PythonCommand(string.Format(@"scene.getCharacter('{0}').setVoiceBackup('{1}')", characterName, voiceType));
            PythonCommand(string.Format(@"scene.getCharacter('{0}').setVoiceBackupCode('{1}')", characterName, voiceCode));
        }
    }

    public List<string> GetLoadedMotions()
    {
        // TODO - should make a copy of this, to prevent modification.
        // the intent is for this to be read-only.
        return m_loadedMotions;
    }

    public override void SBRunPythonScript(string script)
    {
        // when accessing the .py from the file system isn't possible (android), can try something like the below
        //WWW www = Utils.LoadStreamingAssets(script);
        //sbm.PythonCommand(string.Format(@"scene.command('sbm python {0}')", www.text));

        string command = string.Format(@"scene.run('{0}')", script);
        PythonCommand(command);
    }

    public override void SBMoveCharacter(string character, string direction, float fSpeed, float fLrps, float fFadeOutTime)
    {
        string command = string.Format(@"scene.command('sbm test loco char {0} {1} spd {2} rps {3} time {4}')", character, direction, fSpeed, fLrps, fFadeOutTime);
        PythonCommand(command);
    }

    public override void SBWalkTo(string character, string waypoint, bool isRunning)
    {
        string run = isRunning ? @"manner=""run""" : "";
        string message = string.Format(@"bml.execBML('{0}', '<locomotion target=""{1}"" facing=""{2}"" {3} />')", character, waypoint, waypoint, run);
        PythonCommand(message);
    }

    public override void SBWalkImmediate(string character, string locomotionPrefix, double velocity, double turn, double strafe)
    {
        //<sbm:states mode="schedule" loop="true" name="allLocomotion" x="100" y ="0" z="0"/>
        string message = string.Format(@"bml.execBML('{0}', '<sbm:states mode=""schedule"" loop=""true"" sbm:startnow=""true"" name=""{1}"" x=""{2}"" y =""{3}"" z=""{4}"" />')", character, locomotionPrefix, velocity, turn, strafe);
        PythonCommand(message);
    }

    public override void SBPlayAudio(string character, string audioId)
    {
        string message = string.Format(@"bml.execXML('{0}', '<act><participant id=""{1}"" role=""actor""/><bml><speech id=""sp1"" ref=""{2}"" type=""application/ssml+xml""></speech></bml></act>')", character, character, audioId);
        PythonCommand(message);
    }

    public override void SBPlayAudio(string character, string audioId, string text)
    {
        string message = string.Format(@"bml.execXML('{0}', '<act><participant id=""{1}"" role=""actor""/><bml><speech id=""sp1"" ref=""{2}"" type=""application/ssml+xml"">{3}</speech></bml></act>')", character, character, audioId, text);
        PythonCommand(message);
    }

    public override void SBPlayAudio(string character, AudioClip audioId)
    {
        SBPlayAudio(character, audioId.name);
    }

    public override void SBPlayAudio(string character, AudioClip audioId, string text)
    {
        SBPlayAudio(character, audioId.name, text);
    }

    public override void SBPlayAudio(string character, AudioSpeechFile audioId)
    {
        SBPlayAudio(character, audioId.m_AudioClip.name);
    }

    public override void SBPlayXml(string character, string xml)
    {
        string message = string.Format(@"scene.command('bml char {0} file {1}')", character, xml);
        PythonCommand(message);
    }

    public override void SBPlayXml(string character, AudioSpeechFile xml)
    {
        string message = string.Format(@"bml.execXML('{0}', '{1}')", character, xml.ConvertedXml);
        SmartbodyManager.Get().PythonCommand(message);
    }

    public override void SBTransform(string character, Transform transform)
    {
        SBTransform(character, transform.position, transform.rotation);
    }

    public override void SBTransform(string character, Vector3 pos, Quaternion rot)
    {
        Vector3 position = pos / m_positionScaleHack;
        Vector3 eulerAngles = rot.eulerAngles;

        SBTransform(character, -position.x, position.y, position.z, -eulerAngles.y, eulerAngles.x, -eulerAngles.z);
    }

    public override void SBTransform(string character, double x, double y, double z)
    {
        string message = string.Format(@"scene.command('set character {0} world_offset x {1} y {2} z {3}')", character, x, y, z);
        PythonCommand(message);
    }

    public override void SBTransform(string character, double y, double p)
    {

        string message = string.Format(@"scene.command('set character {0} world_offset y {1} p {2}')", character, y, p);
        PythonCommand(message);
    }

    public override void SBTransform(string character, double x, double y, double z, double h, double p, double r)
    {
        string message = string.Format(@"scene.command('set character {0} world_offset x {1} y {2} z {3} h {4} p {5} r {6}')", character, x, y, z, h, p, r);
        PythonCommand(message);
    }

    public override void SBRotate(string character, double h)
    {
        string message = string.Format(@"scene.command('set character {0} world_offset h {1}')", character, h);
        PythonCommand(message);
    }

    public override void SBPosture(string character, string posture, float startTime)
    {
        string message = string.Format(@"bml.execBML('{0}', '<body posture=""{1}"" start=""{2}""/>')", character, posture, startTime);
        PythonCommand(message);
    }

    public override void SBPlayAnim(string character, string animName)
    {
        string message = string.Format(@"bml.execBML('{0}', '<animation name=""{1}""/>')", character, animName);
        PythonCommand(message);
    }

    public override void SBPlayAnim(string character, string animName, float readyTime, float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime)
    {
        string message = string.Format(@"bml.execBML('{0}', '<animation name=""{1}"" start=""0"" ready=""{2}"" stroke=""{3}"" relax=""{4}""/>')",
            character, animName, readyTime.ToString("f6"), strokeTime.ToString("f6"), relaxTime.ToString("f6"));
        PythonCommand(message);
    }

    public override void SBPlayFAC(string character, int au, CharacterDefines.FaceSide side, float weight, float duration)
    {
        // TODO - add blend in/out time
        // side == "left", "right" or "both"
        string message = string.Format(@"bml.execBML('{0}', '<face type=""facs"" au=""{1}"" side=""{2}"" amount=""{3}"" sbm:duration=""{4}"" />')", character, au, side.ToString(), weight, duration);
        PythonCommand(message);
    }

    public void SBPlayFAC(string character, int au, CharacterDefines.FaceSide side, float weight, float duration, float ready, float relaxTime)
    {
        // side == "left", "right" or "both"
        string message = string.Format(@"bml.execBML('{0}', '<face type=""facs"" au=""{1}"" side=""{2}"" amount=""{3}"" sbm:duration=""{4}"" start=""0"" end=""{4}"" ready=""{5}"" relax=""{6}"" />')",
            character, au, side.ToString(), weight, duration, ready, relaxTime);
        PythonCommand(message);
    }

    public override void SBPlayViseme(string character, string viseme, float weight)
    {
        string message = string.Format(@"scene.command('char {0} viseme {1} {2} {3}')", character, viseme, weight, 0);
        PythonCommand(message);
    }

    public override void SBPlayViseme(string character, string viseme, float weight, float totalTime, float blendTime)
    {
        StartCoroutine(SBPlayVisemeCoroutine(character, viseme, weight, totalTime, blendTime));
    }

    IEnumerator SBPlayVisemeCoroutine(string character, string viseme, float weight, float totalTime, float blendTime)
    {
        // sbm char * viseme W 0 1

        // sbm viseme command is an immediate command, doesn't have a total duration.  So we send one command to go to weight, then another to go to 0

        string message = string.Format(@"scene.command('char {0} viseme {1} {2} {3}')", character, viseme, weight, blendTime);
        PythonCommand(message);

        yield return new WaitForSeconds(totalTime - blendTime);

        message = string.Format(@"scene.command('char {0} viseme {1} {2} {3}')", character, viseme, 0, blendTime);
        PythonCommand(message);
    }

    public override void SBNod(string character, float amount, float repeats, float time)
    {
        string message = string.Format(@"bml.execBML('{0}', '<head amount=""{1}"" repeats=""{2}"" type=""{3}"" start=""{4}"" end=""{5}""/>')", character, amount, repeats, "NOD", 0, time);
        //Debug.Log(message);
        PythonCommand(message);
    }

    public void SBNod(string character, float amount, float repeats, float time, float velocity)
    {
        string message = string.Format(@"bml.execBML('{0}', '<head amount=""{1}"" repeats=""{2}"" type=""{3}"" start=""{4}"" end=""{5}"" velocity=""{6}"" />')",
                                       character, amount, repeats, "NOD", 0, time, velocity);
        PythonCommand(message);
    }

    public override void SBShake(string character, float amount, float repeats, float time)
    {
        string message = string.Format(@"bml.execBML('{0}', '<head amount=""{1}"" repeats=""{2}"" type=""{3}"" start=""{4}"" end=""{5}""/>')", character, amount, repeats, "SHAKE", 0, time);
        PythonCommand(message);
    }

    public void SBShake(string character, float amount, float repeats, float time, float velocity)
    {
        string message = string.Format(@"bml.execBML('{0}', '<head amount=""{1}"" repeats=""{2}"" type=""{3}"" start=""{4}"" end=""{5}"" velocity=""{6}"" />')",
                                       character, amount, repeats, "SHAKE", 0, time, velocity);
        PythonCommand(message);
    }

    public override void SBGaze(string character, string gazeAt)
    {
        string message = string.Format(@"bml.execBML('{0}', '<gaze target=""{1}""/>')", character, gazeAt);
        PythonCommand(message);
    }

    public override void SBGaze(string character, string gazeAt, float neckSpeed)
    {
        string message = string.Format(@"bml.execBML('{0}', '<gaze target=""{1}"" sbm:joint-speed=""{2}""/>')", character, gazeAt, neckSpeed.ToString("f2"));
        PythonCommand(message);
    }

    public override void SBGaze(string character, string gazeAt, float neckSpeed, float eyeSpeed, CharacterDefines.GazeJointRange jointRange)
    {
        string message = string.Format(@"bml.execBML('{0}', '<gaze target=""{1}"" sbm:joint-speed=""{2} {3}"" sbm:joint-range=""{4}""/>')",
            character, gazeAt, neckSpeed.ToString("f2"), eyeSpeed.ToString("f2"), jointRange.ToString().Replace("_", " "));
        PythonCommand(message);
    }

    public override void SBGaze(string character, string gazeAt, string targetBone, CharacterDefines.GazeDirection gazeDirection,
        CharacterDefines.GazeJointRange jointRange, float angle, float headSpeed, float eyeSpeed, float fadeOut, string gazeHandleName, float duration)
    {
        string gazeTargetString = string.Format("{0}", gazeAt);
        if (!string.IsNullOrEmpty(targetBone))
        {
            gazeTargetString += string.Format(":{0}", targetBone);
        }
        if (gazeDirection == CharacterDefines.GazeDirection.NONE)
        {
            // gaze up has a 0 degree angle and NONE is not accepted by smartbody
            gazeDirection = CharacterDefines.GazeDirection.UP;
        }
        string message = string.Format(@"bml.execBML('{0}', '<gaze target=""{1}"" direction=""{2}"" sbm:joint-range=""{3}"" angle=""{4}"" sbm:joint-speed=""{5} {6}"" id=""{7}"" sbm:handle=""{7}"" start=""0""/>')",
            character, gazeTargetString, gazeDirection, jointRange.ToString().Replace("_", " "), angle, headSpeed.ToString("f2"), eyeSpeed.ToString("f2"), gazeHandleName, fadeOut);
        PythonCommand(message);

        //<event message="sbm char ChrRachelPrefab gazefade out 0.8" stroke="mygaze1:start+5.255221" />
        //if (duration > 0) // duration of 0 means gaze infinitely
        //{
        //    message = string.Format(@"bml.execBML('{0}', '<event message=""sbm char {0} gazefade out {1}"" stroke=""{2}:start+{3}""/>')",
        //    character, fadeOut, gazeHandleName, duration);
        //    PythonCommand(message);
        //}
    }

    public override void SBStopGaze(string character)
    {
        SBStopGaze(character, 1);
    }

    public override void SBStopGaze(string character, float fadoutTime)
    {
        string message = string.Format(@"scene.command('char {0}  gazefade out {1}')", character, fadoutTime);
        PythonCommand(message);
    }

    public override void SBSaccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration)
    {
        //<event message="sbm bml char ChrRachelPrefab &lt;saccade finish=&quot;true&quot;/&gt;" stroke="0" type="end" track="Saccade" />
        string message = string.Format(@"bml.execBML('{0}', '<event message=""sbm bml char {0} &lt;saccade mode=&quot;{1}&quot; /&gt;"" type=""{1}"" />')",
            character, type.ToString().ToLower());
        Debug.Log(message);
        PythonCommand(message);
    }

    public override void SBSaccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude)
    {
        string message = string.Format(@"bml.execBML('{0}', '<event message=""sbm bml char {0} &lt;saccade mode=&quot;{2}&quot; sbm:duration=&quot;{3}&quot; angle-limit=&quot;{4}&quot; direction=&quot;{5}&quot; magnitude=&quot;{6}&quot; /&gt;"" type=""{1}"" />')",
            character, finish, type.ToString().ToLower(), duration, angleLimit, direction, magnitude);
        PythonCommand(message);
    }

    public override void SBStopSaccade(string character)
    {
        string message = string.Format(@"bml.execBML('{0}', '<event message=""sbm bml char {0} &lt;saccade finish=&quot;true&quot; /&gt;""/>')", character);
        PythonCommand(message);
    }

    public override void SBStateChange(string character, string state, string mode, string wrapMode, string scheduleMode)
    {
        string message = string.Format(@"bml.execBML('{0}', '<sbm:states name=""{1}"" mode=""{2}"" sbm:wrap-mode=""{3}"" sbm:schedule-mode=""{4}""/>')", character, state, mode, wrapMode, scheduleMode);
        PythonCommand(message);
    }

    public override void SBStateChange(string character, string state, string mode, string wrapMode, string scheduleMode, float x)
    {
        string message = string.Format(@"bml.execBML('{0}', '<sbm:states name=""{1}"" mode=""{2}"" sbm:wrap-mode=""{3}"" sbm:schedule-mode=""{4}"" x=""{5}""/>')", character, state, mode, wrapMode, scheduleMode, x.ToString());
        PythonCommand(message);
    }

    public override void SBStateChange(string character, string state, string mode, string wrapMode, string scheduleMode, float x, float y, float z)
    {
        string message = string.Format(@"bml.execBML('{0}', '<sbm:states name=""{1}"" mode=""{2}"" sbm:wrap-mode=""{3}"" sbm:schedule-mode=""{4}"" x=""{5}"" y=""{6}"" z=""{7}""/>')", character, state, mode, wrapMode, scheduleMode, x.ToString(), y.ToString(), z.ToString());
        PythonCommand(message);
    }

    public override string SBGetCurrentStateName(string character)
    {
        string pyCmd = string.Format(@"scene.getStateManager().getCurrentState('{0}')", character);
        return PythonCommand<string>(pyCmd);
    }

    public override Vector3 SBGetCurrentStateParams(string character)
    {
        Vector3 ret = new Vector3();
        string pyCmd = string.Empty;

        pyCmd = string.Format(@"scene.getStateManager().getCurrentStateParameters('{0}').getData(0)", character);
        ret.x = PythonCommand<float>(pyCmd);

        pyCmd = string.Format(@"scene.getStateManager().getCurrentStateParameters('{0}').getData(1)", character);
        ret.y = PythonCommand<float>(pyCmd);

        pyCmd = string.Format(@"scene.getStateManager().getCurrentStateParameters('{0}').getData(2)", character);
        ret.z = PythonCommand<float>(pyCmd);

        return ret;
    }

    public override bool SBIsStateScheduled(string character, string stateName)
    {
        string pyCmd = string.Format(@"scene.getStateManager().isStateScheduled('{0}', '{1}')", character, stateName);
        return PythonCommand<bool>(pyCmd);
    }

    public override float SBGetAuValue(string character, string auName)
    {
        string pyCmd = string.Format(@"scene.getCharacter('{0}').getSkeleton().getJointByName('{1}').getPosition().getData(0)", character, auName);
        return PythonCommand<float>(pyCmd);
    }

    /// <summary>
    /// Sends a vrExpress message
    /// </summary>
    /// <param name="character">Character.</param>
    /// <param name="uttID">the ref= parameter, meaning the sound id</param>
    /// <param name="expressId">the vrExpress message id.</param>
    /// <param name="text">Text.</param>
    public override void SBExpress(string character, string uttID, string expressId, string text)
    {
        SBExpress(character, uttID, expressId, text, "user");
    }

    /// <summary>
    /// Sends a vrExpress message
    /// </summary>
    /// <param name="character">Character.</param>
    /// <param name="uttID">the ref= parameter, meaning the sound id</param>
    /// <param name="expressId">the vrExpress message id.</param>
    /// <param name="text">Text.</param>
    /// <param name="target">Target.</param>
    public override void SBExpress(string character, string uttID, string expressId, string text, string target)
    {
        string uttIDModified = uttID;
        if (string.IsNullOrEmpty(uttID))
            uttIDModified = DateTime.Now.ToString("yyyyMMddHHmmssffff");

        string expressIdModified = expressId;
        if (string.IsNullOrEmpty(expressId))
            expressIdModified = DateTime.Now.ToString("yyyyMMddHHmmssffff");

        string message = string.Format("vrExpress {0} {4} {2} <?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>"
            + "<act><participant id=\"{0}\" role=\"actor\" /><fml><turn start=\"take\" end=\"give\" /><affect type=\"neutral\" "
            + "target=\"addressee\"></affect><culture type=\"neutral\"></culture><personality type=\"neutral\"></personality></fml>"
            + "<bml><speech id=\"sp1\" ref=\"{1}\" type=\"application/ssml+xml\">{3}</speech></bml></act>", character, uttIDModified, expressIdModified, text, target);
        VHMsgBase.Get().SendVHMsg(message);
    }

    public override void SBGesture(string character, string gestureName)
    {
        string message = string.Format(@"bml.execBML('{0}', '<gesture name=""{1}""/>')", character, gestureName);
        PythonCommand(message);
    }

    public override void SBGesture(string character, string lexeme, string lexemeType, GestureUtils.Handedness hand, GestureUtils.Style style, GestureUtils.Emotion emotion,
            string target, bool additive, string jointRange, float perlinFrequency, float perlinScale, float readyTime, float strokeStartTime,
        float emphasisTime, float strokeTime, float relaxTime)
    {
        const string Replace = " />";
        StringBuilder builder = new StringBuilder(string.Format(@"bml.execBML('{0}', '<gesture lexeme=""{1}"" type=""{2}"" hand=""{3}"" sbm:style=""{4}"" emotion=""{5}"" sbm:additive=""{6}"" sbm:frequency=""{7}"" sbm:scale=""{8}"" />')",
            character, lexeme, lexemeType, hand.ToString(), style.ToString(), emotion.ToString(), additive.ToString(), perlinFrequency, perlinScale));
        if (!string.IsNullOrEmpty(target))
            builder = builder.Replace(Replace, string.Format(@"target=""{0}""{1}", target, Replace));

        if (!string.IsNullOrEmpty(jointRange))
            builder = builder.Replace(Replace, string.Format(@"sbm:joint-range=""{0}""{1}", jointRange, Replace));

        if (readyTime >= 0)
            builder = builder.Replace(Replace, string.Format(@"ready=""{0}""{1}", readyTime.ToString("f3"), Replace));

        if (strokeStartTime >= 0)
            builder = builder.Replace(Replace, string.Format(@"stroke_start=""{0}""{1}", strokeStartTime.ToString("f3"), Replace));

        if (emphasisTime >= 0)
            builder = builder.Replace(Replace, string.Format(@"stroke=""{0}""{1}", emphasisTime.ToString("f3"), Replace));

        if (strokeTime >= 0)
            builder = builder.Replace(Replace, string.Format(@"stroke_end=""{0}""{1}", strokeTime.ToString("f3"), Replace));

        if (relaxTime >= 0)
            builder = builder.Replace(Replace, string.Format(@"relax=""{0}""{1}", relaxTime.ToString("f3"), Replace));

        PythonCommand(builder.ToString());
    }

    public override ICharacter[] GetControlledCharacters ()
    {
        return m_characterList.ToArray();
    }

    public override ICharacter GetCharacter (string character)
    {
        return GetCharacterBySBMName(character);
    }
}
