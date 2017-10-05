using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

public class UnitySmartbodyCharacter : ICharacter
{
    #region Constants
    public enum DebugFlags
    {
        Show_Bones = 1,
        Show_Axes = 1 << 1,
        Show_Eye_Beams = 1 << 2,
    }

    public delegate void ChannelCallback(UnitySmartbodyCharacter character, string channelName, float value);
    #endregion

    #region DataMembers

    // used for GetBoneAndBaseBonePosition().  Instead of passing back a Vector3, which is a struct, and requires a copy, we wrapped the Vector3 with a class, so that it can be passed back by reference.  This function is called so many times, it's worth this optimization
    public class Vector3class
    {
        public Vector3 vector3;
    }

    public class Quaternionclass
    {
        public Quaternion quat;

        public Quaternionclass() { }

        public Quaternionclass(Quaternion _quat)
        {
            quat = _quat;
        }
    }

    // this struct represents the data that is passed to the smartbody dll wrapper and gets filled out.
    // Upon returning from smartbody, the data in this structure is assigned to this character's bones
    public struct UnityCharacterData
    {
        public SmartbodyExternals.SmartbodyCharacterFrameData m_Character;
        public IntPtr [] jnames;
        public float [] jx;
        public float [] jy;
        public float [] jz;
        public float [] jrw;
        public float [] jrx;
        public float [] jry;
        public float [] jrz;

        public float [] jprevx;
        public float [] jprevy;
        public float [] jprevz;
        public float [] jprevrw;
        public float [] jprevrx;
        public float [] jprevry;
        public float [] jprevrz;
        public float [] jNextUpdateTime;
    }
    public UnityCharacterData m_CharacterData = new UnityCharacterData();

    public const string SoundNodeName = "SoundNode";

    protected int m_characterID;
    protected string m_characterType;
    protected string m_sbmCharacterName;
    protected uint m_DebugFlags;
    protected ChannelCallback m_ChannelCB;

    const int NumBones = 120;
    Transform[] m_Bones;// = new Transform[NumBones];
    Vector3class[] m_BaseBonePositions;
    Quaternionclass[] m_BaseBoneRotations;
    Dictionary<string, int> m_BoneLookupTable = new Dictionary<string, int>(NumBones);
    Dictionary<string, List<SkinnedMeshRenderer>> m_BlendShapes = new Dictionary<string, List<SkinnedMeshRenderer>>();
    Dictionary<string, string> m_BlendShapeNameMap = new Dictionary<string, string>();

    // these are fudge factors for legacy projects for adjusting scale.
    // for example, if your incoming smartbody data is in cm, and your level is in feet, you'd call these functions with a parameter of ( 1 / 30.48 )
    // These should be considered hacks because changes in scale will cause issues with smartbody
    float m_characterPositionScaleModifier;
    float m_bonePositionScaleModifier;

    AudioSource m_AudioSource;

    #endregion

    #region Properties

    public bool ShowBones
    {
        get { return VHMath.IsFlagOn(m_DebugFlags, (uint)DebugFlags.Show_Bones); }
    }

    public bool ShowEyeBeams
    {
        get { return VHMath.IsFlagOn(m_DebugFlags, (uint)DebugFlags.Show_Eye_Beams); }
    }

    public bool ShowAxes
    {
        get { return VHMath.IsFlagOn(m_DebugFlags, (uint)DebugFlags.Show_Axes); }
    }

    public int GetNumBones
    {
        get { return m_Bones.Length; }
    }

    public void ToggleDebugFlag(DebugFlags flag)
    {
        VHMath.ToggleFlag(ref m_DebugFlags, (uint)flag);

        if (flag == DebugFlags.Show_Bones)
        {
            // stop showing geometry, show the bones
            ShowGeometry(!VHMath.IsFlagOn(m_DebugFlags, (uint)DebugFlags.Show_Bones));
        }
    }

    public float CharacterPositionScaleModifier
    {
        get { return m_characterPositionScaleModifier; }
        set { m_characterPositionScaleModifier = value; }
    }

    public float BonePositionScaleModifier
    {
        get { return m_bonePositionScaleModifier; }
        set { m_bonePositionScaleModifier = value; }
    }

    public int CharacterID
    {
        get { return m_characterID; }
        set { m_characterID = value; }
    }

    public string CharacterType
    {
        get { return m_characterType; }
        set { m_characterType = value; }
    }

    public override string CharacterName
    {
        get { return SBMCharacterName; }
    }

    public override AudioSource Voice {
        get { return AudioSource; }
    }

    public string SBMCharacterName
    {
        get { return m_sbmCharacterName; }
        set { m_sbmCharacterName = value; }
    }

    public AudioSource AudioSource
    {
        get { return m_AudioSource; }
    }

    public bool IsSpeaking
    {
        get { return m_AudioSource != null && m_AudioSource.isPlaying; }
    }

    public string SkeletonName
    {
        get { return GetComponent<SmartbodyCharacterInit>().skeletonName; }
    }

    public string BoneParentName
    {
        get { return GetComponent<SmartbodyCharacterInit>().unityBoneParent; }
    }
    #endregion

    public void Awake()
    {

    }

    public void Start()
    {
        //Debug.Log("UnitySmartbodyCharacter.Start()");

        // SmartbodyManager is a dependency of this component.  Make sure Start() has been called.
        SmartbodyManager sbm = SmartbodyManager.Get();
        sbm.Start();

        m_CharacterData.m_Character = new SmartbodyExternals.SmartbodyCharacterFrameData();
        m_CharacterData.m_Character.m_name = IntPtr.Zero;

        SmartbodyCharacterInit init = GetComponent<SmartbodyCharacterInit>();
        if (init != null)
        {
            DateTime startTime = DateTime.Now;

            SmartbodyFaceDefinition face = GetComponent<SmartbodyFaceDefinition>();   // ok if it's null
            SmartbodyJointMap jointMap = GetComponent<SmartbodyJointMap>();   // ok if it's null
            GestureMapDefinition gestureMap = GetComponent<GestureMapDefinition>();   // ok if it's null
            CreateCharacter(init, face, jointMap, gestureMap);

            Debug.Log(string.Format("Finished initializing character {0} in {1} seconds", SBMCharacterName, (DateTime.Now - startTime).TotalSeconds.ToString("f3")));
        }
        else
        {
            Debug.LogWarning("UnitySmartbodyCharacter.Start() - " + name + " - No SmartbodyCharacterInit script attached.  You need to attach a SmartbodyCharacterInit script to this gameobject so that it will initialize properly");
        }
    }

    public void Update()
    {
    }

    void OnDestroy()
    {
        //Debug.Log("UnitySmartbodyCharacter.OnDestroy()");

        SmartbodyManager sbm = SmartbodyManager.Get();
        if (sbm != null)
        {
            //sbm.PythonCommand(string.Format(@"scene.command('sbm char {0} remove')", SBMCharacterName)); // old-style command
            sbm.PythonCommand(string.Format(@"scene.removeCharacter('{0}')", SBMCharacterName));

            sbm.RemoveCharacter(this);
        }
    }

    public void OnDrawGizmos()
    {
        if (ShowEyeBeams)
        {
            Transform t;
            t = GetBone("eyeball_left");
            if (t == null ) t = GetBone("JtEyeLf");
            Debug.DrawRay(t.position, t.forward, Color.red);
            t = GetBone("eyeball_right");
            if (t == null ) t = GetBone("JtEyeRt");
            Debug.DrawRay(t.position, t.forward, Color.red);
        }

        if (ShowAxes)
        {
            for (int i = 0; i < m_Bones.Length; i++)
            {
                VHUtils.DrawTransformLines(m_Bones[i].transform, 0.025f);
            }
        }

        if (ShowBones)
        {
            for (int i = 0; i < m_Bones.Length; i++)
            {
                Gizmos.DrawSphere(m_Bones[i].transform.position, 0.0125f);

                if (m_Bones[i].parent != null)
                {
                    Debug.DrawLine(m_Bones[i].transform.position, m_Bones[i].parent.transform.position);
                }
            }
        }
    }


    #region Functions
    public static string FixJointName(string jointName)
    {
        string fixedName = jointName;
        fixedName = fixedName.Replace(".", "");
        fixedName = fixedName.Replace(" ", "");
        return fixedName;
    }

    public void GetAllBlendShapes(out Dictionary<string, string> blendShapeNameMap, out Dictionary<string, List<SkinnedMeshRenderer>> blendShapes)
    {
        blendShapeNameMap = new Dictionary<string, string>();
        blendShapes = new Dictionary<string, List<SkinnedMeshRenderer>>();

        SkinnedMeshRenderer[] skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        for (int skinnedMeshIndex = 0; skinnedMeshIndex < skinnedMeshes.Length; skinnedMeshIndex++)
        {
            SkinnedMeshRenderer smr = skinnedMeshes[skinnedMeshIndex];
            int blendShapeCount = smr.sharedMesh.blendShapeCount;
            for (int blendShapeIndex = 0; blendShapeIndex < blendShapeCount; blendShapeIndex++)
            {
                string blendShapeName = smr.sharedMesh.GetBlendShapeName(blendShapeIndex);
                blendShapeName = FixJointName(blendShapeName);

                if (!blendShapeNameMap.ContainsKey(blendShapeName))
                {
                    blendShapeNameMap.Add(blendShapeName, smr.sharedMesh.GetBlendShapeName(blendShapeIndex));
                }

                if (!blendShapes.ContainsKey(blendShapeName))
                {
                    blendShapes.Add(blendShapeName, new List<SkinnedMeshRenderer>());
                }

                blendShapes[blendShapeName].Add(smr);
            }
        }
    }


    public void CreateSkeleton()
    {
        SmartbodyCharacterInit characterInit = GetComponent<SmartbodyCharacterInit>();
        CreateSkeleton(characterInit);
    }


    public void CreateSkeleton(SmartbodyCharacterInit characterInit)
    {
        if (characterInit != null)
        {
            if (characterInit.loadSkeletonFromSk)
                return;

            SmartbodyManager sbm = SmartbodyManager.Get();

            if (sbm.IsSkeletonLoaded(characterInit.skeletonName))
                return;

            Transform skeletonTransform = VHUtils.FindChild(gameObject, characterInit.unityBoneParent).transform;
            bool loadAllChannels = characterInit.loadAllChannels;
            List<string> blendShapesList = new List<string>(m_BlendShapes.Keys);
            sbm.CreateSkeleton(characterInit.skeletonName, skeletonTransform, loadAllChannels, blendShapesList);
        }
        else
        {
            Debug.LogError("Failed to create skeleton because no SmartbodyCharacterInit was found");
        }
    }


    public static void InstantiateMotionSets(SmartbodyMotionSet [] motionSets)
    {
        // put the referenced MotionSets in the scene if they point to prefabs
        // the MotionSets need to be instantiated because they do work in Awake() and start coroutines.

        SmartbodyMotionSet [] allObjectsInScene = FindObjectsOfType<SmartbodyMotionSet>();
        for (int i = 0; i < motionSets.Length; i++)
        {
            SmartbodyMotionSet motionSet = motionSets[i];

            if (motionSet && motionSet.gameObject.activeSelf)
            {
                bool found = false;
                foreach (SmartbodyMotionSet obj in allObjectsInScene)
                {
                    if (obj == motionSet)
                    {
                        // object matches
                        found = true;
                        break;
                    }

                    if (obj.gameObject.name == motionSet.gameObject.name)
                    {
                        // object name matches, so we found a gameobject in the scene that matches the prefab
                        // this assumes all motion sets have unique names
                        motionSets[i] = obj;

                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    GameObject topLevel = GameObject.Find("__DynamicMotionSets");
                    if (topLevel == null)
                        topLevel = new GameObject("__DynamicMotionSets");

                    GameObject newObj = (GameObject)UnityEngine.Object.Instantiate(motionSet.gameObject);
                    newObj.name = newObj.name.Replace("(Clone)", "");
                    newObj.transform.parent = topLevel.transform;

                    motionSets[i] = newObj.GetComponent<SmartbodyMotionSet>();
                }
            }
        }
    }


    public void CreateCharacter(SmartbodyCharacterInit character, SmartbodyFaceDefinition face, SmartbodyJointMap jointMap, GestureMapDefinition gestureMap)
    {
        /*
            brad = scene.createCharacter("brad", "brad-attach")
            brad.setSkeleton(scene.createSkeleton("common.sk"))
            brad.setFaceDefinition(defaultFace)
            brad.createStandardControllers()

            brad.setVoice("audiofile")
            brad.setVoiceCode("Sounds")
            brad.setVoiceBackup("remote")
            brad.setVoiceBackupCode("Festival_voice_rab_diphone")
            brad.setUseVisemeCurves(True)
        */

        {
            GameObject boneParent = VHUtils.FindChild(gameObject, character.unityBoneParent);
            m_Bones = boneParent.GetComponentsInChildren<Transform>();
            m_BaseBonePositions = new Vector3class[m_Bones.Length];
            m_BaseBoneRotations = new Quaternionclass[m_Bones.Length];
            m_BoneLookupTable = new Dictionary<string, int>(NumBones);

            //Debug.Log("num bones: " + m_Bones.Length + " m_Bones[0].name: " + m_Bones[0].name);

            for (int i = 0; i < m_Bones.Length; i++)
            {
                m_BoneLookupTable.Add(m_Bones[i].gameObject.name, i);
                m_BaseBonePositions[i] = new Vector3class();
                m_BaseBonePositions[i].vector3 = m_Bones[i].localPosition;
                m_BaseBoneRotations[i] = new Quaternionclass(m_Bones[i].localRotation);
            }
        }

        // find all blend shapes
        GetAllBlendShapes(out m_BlendShapeNameMap, out m_BlendShapes);

        GameObject soundNode = VHUtils.FindChild(gameObject, SoundNodeName);
        if (soundNode != null)
        {
            m_AudioSource = soundNode.GetComponent<AudioSource>();
        }
        else
        {
            Debug.LogWarning("No SoundNode found for " + name + ". You need to create a gameobject called '" +
                SoundNodeName + "' ,attach it as a child to this character's prefab, and give it an audiosource component. " +
                "Until you do this, sound cannot be played from this character");
        }

        SBMCharacterName = character.name;

        SmartbodyManager sbm = SmartbodyManager.Get();


        CreateSkeleton();  // only if loadSkeletonFromSk is false


        InstantiateMotionSets(character.m_MotionSets);


        DateTime startTime = DateTime.Now;
        sbm.LoadAssetPaths(character.assetPaths);
        Debug.Log(string.Format("Finished loading asset paths {0} seconds", (DateTime.Now - startTime).TotalSeconds.ToString("f3")));


        if (jointMap != null)
        {
            sbm.AddJointMap(jointMap);

            sbm.ApplySkeletonToJointMap(jointMap, character.skeletonName);

            sbm.MapSkeletonAndAssetPaths(jointMap, character.skeletonName, character.assetPaths);
        }


        SmartbodyMotionSet [] allMotionSets = GameObject.FindObjectsOfType<SmartbodyMotionSet>();
        foreach (SmartbodyMotionSet motionSet in allMotionSets)
        {
            if (motionSet && motionSet.gameObject.activeSelf)
            {
                motionSet.LoadMotions();

                sbm.CreateRetargetPair(motionSet.SkeletonName, character.skeletonName);
            }
        }


        foreach (var pair in character.assetPaths)
        {
            string skeletonName = pair.Key;
            sbm.CreateRetargetPair(skeletonName, character.skeletonName);
        }


        sbm.PythonCommand(string.Format(@"scene.createCharacter('{0}', '{1}')", SBMCharacterName, CharacterName));
        sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setSkeleton(scene.createSkeleton('{1}'))", SBMCharacterName, character.skeletonName));

        sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('bmlscheduledelay', 0)", SBMCharacterName));

        if (face != null && face.enabled)
        {
            //Debug.Log("face.definitionName: " + face.definitionName);
            sbm.AddFaceDefinition(face);
            sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setFaceDefinition(scene.getFaceDefinition('{1}'))", SBMCharacterName, face.definitionName));
        }

        sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').createStandardControllers()", SBMCharacterName));

        if (!string.IsNullOrEmpty(character.voiceType) &&
            !string.IsNullOrEmpty(character.voiceCode))
        {
            sbm.SetCharacterVoice(SBMCharacterName, character.voiceType, character.voiceCode, false);
            //sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setVoice('{1}')", SBMCharacterName, character.voiceType));
            //sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setVoiceCode('{1}')", SBMCharacterName, character.voiceCode));
        }

        if (!string.IsNullOrEmpty(character.voiceTypeBackup) &&
            !string.IsNullOrEmpty(character.voiceCodeBackup))
        {
            sbm.SetCharacterVoice(SBMCharacterName, character.voiceTypeBackup, character.voiceCodeBackup, true);
            //sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setVoiceBackup('{1}')", SBMCharacterName, character.voiceTypeBackup));
            //sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setVoiceBackupCode('{1}')", SBMCharacterName, character.voiceCodeBackup));
        }

        if (character.usePhoneBigram)
        {
            sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setStringAttribute('lipSyncSetName', 'default')", SBMCharacterName));
            sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setBoolAttribute('usePhoneBigram', True)", SBMCharacterName));
        }
        else
        {
            sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setUseVisemeCurves(True)", SBMCharacterName));
        }

        if (gestureMap != null && gestureMap.enabled)
        {
            sbm.AddGestureMapDefinition(gestureMap);
            sbm.PythonCommand(string.Format(@"scene.getCharacter('{0}').setStringAttribute('gestureMap', '{1}')", SBMCharacterName, gestureMap.gestureMapName));
            //SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setBoolAttribute('bmlRequest.autoGestureTransition', True)", character.SBMCharacterName));
        }

        if (!string.IsNullOrEmpty(character.startingPosture))
        {
            sbm.SBPosture(SBMCharacterName, character.startingPosture, UnityEngine.Random.Range(0, 4.0f));
        }


        // locomotion/steering currently only working under certain platforms  (can't find pprAI lib)
        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.IPhonePlayer)
        {
            if (!string.IsNullOrEmpty(character.locomotionInitPythonFile))
            {
                if (!string.IsNullOrEmpty(character.locomotionInitPythonSkeletonName))
                {
                    sbm.PythonCommand(string.Format(@"locomotionInitSkeleton = '{0}'", character.locomotionInitPythonSkeletonName));
                }

                sbm.SBRunPythonScript(character.locomotionInitPythonFile);

                sbm.PythonCommand(string.Format(@"scene.getSteerManager().removeSteerAgent('{0}')", SBMCharacterName));
                sbm.PythonCommand(string.Format(@"scene.getSteerManager().createSteerAgent('{0}')", SBMCharacterName));

                if (!string.IsNullOrEmpty(character.locomotionSteerPrefix))
                {
                    sbm.PythonCommand(string.Format(@"scene.getSteerManager().getSteerAgent('{0}').setSteerStateNamePrefix('{1}')", SBMCharacterName, character.locomotionSteerPrefix));
                }
                else
                {
                    Debug.LogWarning("UnitySmartbodyCharacter.CreateCharacter() - locomotionInitPython file specified, but no locomotionSteerPrefix specified.  This must be specified for locomotion to work");
                }

                sbm.PythonCommand(string.Format(@"scene.getSteerManager().getSteerAgent('{0}').setSteerType('{1}')", SBMCharacterName, "example"));

                //# Toggle the steering manager
                sbm.PythonCommand(string.Format(@"scene.getSteerManager().setEnable(False)"));
                sbm.PythonCommand(string.Format(@"scene.getSteerManager().setEnable(True)"));
            }
        }


        sbm.CreateCharacter(this);


        sbm.SBTransform(SBMCharacterName, transform);


        character.TriggerPostLoadEvent(this);
    }

    void ShowGeometry(bool show)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = show;
        }
    }

    public bool IsVisible()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].isVisible)
            {
                return true;
            }
        }
        return false;
    }

    public virtual void OnBoneTransformations(float positionScale)
    {
        Transform currentBoneTransform = null;
        Quaternion tempQ = Quaternion.identity;
        Vector3 tempVec = Vector3.zero;
        UnitySmartbodyCharacter.Vector3class baseBonePosition = null;
        string jointName = String.Empty;
        float curTime = Time.time;
        SmartbodyFaceDefinition face = GetComponent<SmartbodyFaceDefinition>();
        for (int i = 0; i < m_CharacterData.m_Character.m_numJoints; i++)
        {
            bool posCacheHit = false;
            bool rotCacheHit = false;

            if (m_CharacterData.jNextUpdateTime[i] > curTime)
            {
                if (m_CharacterData.jx[i] == m_CharacterData.jprevx[i] &&
                    m_CharacterData.jy[i] == m_CharacterData.jprevy[i] &&
                    m_CharacterData.jz[i] == m_CharacterData.jprevz[i])
                {
                    posCacheHit = true;
                }

                if (m_CharacterData.jrw[i] == m_CharacterData.jprevrw[i] &&
                    m_CharacterData.jrx[i] == m_CharacterData.jprevrx[i] &&
                    m_CharacterData.jry[i] == m_CharacterData.jprevry[i] &&
                    m_CharacterData.jrz[i] == m_CharacterData.jprevrz[i])
                {
                    rotCacheHit = true;
                }
            }


            if (posCacheHit && rotCacheHit)
                continue;

            // update when next to force an update no matter what the cache status
            const float cacheRefreshTimeMin = 5.0f;  // seconds between refreshes
            const float cacheRefreshTimeMax = 6.0f;
            float refreshTime = Mathf.Lerp(cacheRefreshTimeMin, cacheRefreshTimeMax, UnityEngine.Random.value);   // return number between min/max

            m_CharacterData.jNextUpdateTime[i] = curTime + refreshTime;

            jointName = Marshal.PtrToStringAnsi(m_CharacterData.jnames[i]);
            if (String.IsNullOrEmpty(jointName))
            {
                continue;
            }

            if (face != null && face.enabled && (face.visemes.FindIndex(s => s.Key == jointName) != -1 || jointName.IndexOf("au_") != -1))
            {
                SetChannel(jointName, m_CharacterData.jx[i] * positionScale);
            }

            bool ret = GetBoneAndBaseBonePosition(jointName, out currentBoneTransform, out baseBonePosition);
            if (ret == false)
            {
                if (IsBlendShape(jointName))
                {
                    HandleBlendShape(jointName, m_CharacterData.jx[i]);
                }
                continue;
            }

            if (!rotCacheHit)
            {
                // set rotation
                tempQ.Set( m_CharacterData.jrx[i],
                          -m_CharacterData.jry[i],
                          -m_CharacterData.jrz[i],
                           m_CharacterData.jrw[i]);

                currentBoneTransform.localRotation = tempQ;
            }

            if (!posCacheHit)
            {
                // set position
                tempVec.Set(baseBonePosition.vector3.x + (-m_CharacterData.jx[i] * positionScale),
                            baseBonePosition.vector3.y + ( m_CharacterData.jy[i] * positionScale),
                            baseBonePosition.vector3.z + ( m_CharacterData.jz[i] * positionScale));

                currentBoneTransform.localPosition = tempVec;
            }
        }
    }

    public virtual void SetChannel(string channelName, float value)
    {
        //Debug.Log("SetChannel() - " + channelName + " " + value);

        if (m_ChannelCB != null)
        {
            m_ChannelCB(this, channelName, value);
        }
        else
        {
            //Debug.LogError("UnitySmartbodyCharacter::SetChannel was called but no callback is set. Call SetChannelCallback to set up a callback function.");
        }
    }

    public void SetChannelCallback(ChannelCallback channelCB)
    {
        m_ChannelCB += channelCB;
    }

    public void ResetSkeleton()
    {
        for (int i = 0; i < m_Bones.Length; i++)
        {
            m_Bones[i].transform.localPosition = GetBaseBonePosition(m_Bones[i].name);
            m_Bones[i].transform.localRotation = GetBaseBoneRotation(m_Bones[i].name);
        }
    }

    public Transform GetBone(string boneName)
    {
        int index = -1;
        if (m_BoneLookupTable.TryGetValue(boneName, out index))
        {
            return m_Bones[index];
        }
        else
        {
            Debug.LogError("there's no bone named: " + boneName);
        }
        return null;
    }

    public Transform GetBone(int index)
    {
        if (index < 0 || index >= m_Bones.Length)
        {
            return null;
        }

        return m_Bones[index];
    }


    public Vector3 GetBaseBonePosition(string boneName)
    {
        int index = -1;
        if (m_BoneLookupTable.TryGetValue(boneName, out index))
        {
            return m_BaseBonePositions[index].vector3;
        }
        else
        {
            Debug.LogError("there's no bone named: " + boneName);
        }
        return Vector3.zero;
    }

    public Quaternion GetBaseBoneRotation(string boneName)
    {
        int index = -1;
        if (m_BoneLookupTable.TryGetValue(boneName, out index))
        {
            return m_BaseBoneRotations[index].quat;
        }
        else
        {
            Debug.LogError("there's no bone named: " + boneName);
        }
        return Quaternion.identity;
    }

    public bool GetBoneAndBaseBonePosition(string boneName, out Transform bone, out Vector3class baseBonePosition)
    {
        // this is a combination of GetBone() and GetBaseBonePosition() to reduce dictionary lookups

        int index;
        if (m_BoneLookupTable.TryGetValue(boneName, out index))
        {
            bone = m_Bones[index];
            baseBonePosition = m_BaseBonePositions[index];
            return true;
        }
        else
        {
            //Debug.LogError("there's no bone named: " + boneName);
            bone = null;
            baseBonePosition = null;
        }

        return false;
    }

    bool IsBlendShape(string shapeName)
    {
        if (m_BlendShapes.ContainsKey(shapeName))
        {
            return true;
        }

        return false;
    }

    void HandleBlendShape(string shapeName, float weight)
    {
        if (m_BlendShapes.ContainsKey(shapeName))
        {
            foreach (SkinnedMeshRenderer smr in m_BlendShapes[shapeName])
            {
                int blendShapeIndex = smr.sharedMesh.GetBlendShapeIndex(m_BlendShapeNameMap[shapeName]);
                if (blendShapeIndex != -1)
                {
                    //Debug.Log("shapeName: " + shapeName + " weight: " + weight);
                    // unity uses blend shape scale 0-100 whereas maya uses 0-1, so we have to convert
                    smr.SetBlendShapeWeight(blendShapeIndex, Mathf.Clamp(weight, 0, 100));
                    SetChannel(shapeName, weight);
                }
                else
                {
                    Debug.LogError(string.Format("No blend shape found with name shape name {0}", shapeName));
                }
            }
        }
        else
        {
            Debug.LogError(string.Format("No blend shape found with name shape name {0}", shapeName));
        }
    }

    #region ICharacter Implementation
    public override void PlayAudio(AudioSpeechFile audioId)
    {
        //SmartbodyManager.Get().PythonCommand();
    }

    public override void PlayXml(string xml)
    {
        throw new NotImplementedException();
    }

    public override void PlayXml(AudioSpeechFile xml)
    {
        throw new NotImplementedException();
    }

    public override void Transform(Transform trans)
    {
        throw new NotImplementedException();
    }

    public override void Transform(Vector3 pos, Quaternion rot)
    {
        throw new NotImplementedException();
    }

    public override void Transform(float y, float p)
    {
        throw new NotImplementedException();
    }

    public override void Transform(float x, float y, float z)
    {
        throw new NotImplementedException();
    }

    public override void Transform(float x, float y, float z, float h, float p, float r)
    {
        throw new NotImplementedException();
    }

    public override void Rotate(float h)
    {
        throw new NotImplementedException();
    }

    public override void PlayPosture(string posture, float startTime)
    {
        throw new NotImplementedException();
    }

    public override void PlayAnim(string animName)
    {
        throw new NotImplementedException();
    }

    public override void PlayAnim(string animName, float readyTime, float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime)
    {
        throw new NotImplementedException();
    }

    public override void PlayViseme(string viseme, float weight)
    {
        throw new NotImplementedException();
    }

    public override void PlayViseme(string viseme, float weight, float totalTime, float blendTime)
    {
        throw new NotImplementedException();
    }

    public override void Nod(float amount, float repeats, float time)
    {
        throw new NotImplementedException();
    }

    public override void Shake(float amount, float repeats, float time)
    {
        throw new NotImplementedException();
    }

    public override void Gaze(string gazeAt)
    {
        throw new NotImplementedException();
    }

    public override void Gaze(string gazeAt, float headSpeed)
    {
        throw new NotImplementedException();
    }

    public override void Gaze(string gazeAt, float headSpeed, float eyeSpeed, CharacterDefines.GazeJointRange jointRange)
    {
        throw new NotImplementedException();
    }

    public override void Gaze(string gazeAt, string targetBone, CharacterDefines.GazeDirection gazeDirection, CharacterDefines.GazeJointRange jointRange, float angle, float headSpeed, float eyeSpeed, float fadeOut, string gazeHandleName, float duration)
    {
        throw new NotImplementedException();
    }

    public override void StopGaze()
    {
        throw new NotImplementedException();
    }

    public override void StopGaze(float fadoutTime)
    {
        throw new NotImplementedException();
    }

    public override void Saccade(CharacterDefines.SaccadeType type, bool finish, float duration)
    {
        throw new NotImplementedException();
    }

    public override void Saccade(CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude)
    {
        throw new NotImplementedException();
    }

    public override void StopSaccade()
    {
        throw new NotImplementedException();
    }
    #endregion
    #endregion

}
