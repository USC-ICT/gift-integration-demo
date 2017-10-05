using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public abstract class SmartbodyCharacterInit : MonoBehaviour
{
    public string unityBoneParent;
    public string skeletonName;
    public bool loadSkeletonFromSk = true;  // whether to load the skeleton from the .sk, or from the Unity gameobject
    [NonSerialized] public bool loadAllChannels;  // whether to tell smartbody to load all channels for each joint, or determine which channels to load based on metadata.  Set to 'true' for non-ICT characters.
    [NonSerialized] public string voiceType;  // 'audiofile' or 'remote'
    [NonSerialized] public string voiceCode;  // if 'audiofile' then points to folder that has sounds, else the TTS voice to use
    [NonSerialized] public string voiceTypeBackup;  // if the main voice is not available, offer a backup voice
    [NonSerialized] public string voiceCodeBackup;

    [NonSerialized][Obsolete] public bool useVisemeCurves;  // true for all current characters
    [NonSerialized] public bool usePhoneBigram = false;  // true to use Smartbody viseme mapping, false to use viseme curves

    [NonSerialized] public List<KeyValuePair<string, string>> assetPaths = new List<KeyValuePair<string,string>>();   // "ME", "Art/Characters"

    public string startingPosture;

    [NonSerialized] public string locomotionInitPythonSkeletonName;  // Set to the skeleton name to use for locomotion initialization, eg "ChrBrad.sk"
    [NonSerialized] public string locomotionInitPythonFile;  // which python file to run to initialize all the smartbody locomotion parameters for this character
    [NonSerialized] public string locomotionSteerPrefix;  // the steering agent looks for particular blend names like 'xxxLocomotion'. The prefix in this case is 'xxx'. Usually, it is the characgter's name.

    public delegate void CharacterInitHandler(UnitySmartbodyCharacter character);
    public event CharacterInitHandler PostLoadEvent;

    public bool m_loadAllMotionSetsInScene = false;    // instead of specifying which motionsets to load, just load all the motion sets in the scene.
    public SmartbodyMotionSet[] m_MotionSets;

    protected virtual void Awake()
    {
        if (m_loadAllMotionSetsInScene)
        {
            List<SmartbodyMotionSet> newMotionSets = new List<SmartbodyMotionSet>(m_MotionSets);

            SmartbodyMotionSet [] motionSetsInScene = GameObject.FindObjectsOfType<SmartbodyMotionSet>();
            foreach (var motionSet in motionSetsInScene)
            {
                newMotionSets.Add(motionSet);
            }

            m_MotionSets = newMotionSets.ToArray();
        }
    }

    protected virtual void Start()
    {
    }

    public virtual void TriggerPostLoadEvent(UnitySmartbodyCharacter character)
    {
        if (PostLoadEvent != null)
            PostLoadEvent(character);
    }
}
