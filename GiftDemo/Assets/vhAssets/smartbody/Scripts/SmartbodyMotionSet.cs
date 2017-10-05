using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class SmartbodyMotionSet : MonoBehaviour
{
    #region Constants
    public enum LoadType
    {
        Preload,
        Streaming_SequentialTime,
        Streaming_SequentialMinFps,
    }
    #endregion

    #region Variables
    public LoadType m_loadType;
    public float m_SecondsBeforeLoading = 0;
    public float m_StreamingCompletionTime = 5;
    public float m_MinimumFramerate = 30;
    public FpsCounter m_fpsCounter;
    public UnitySmartbodyCharacter m_ReferenceCharacter;
    public SmartbodyCharacterInit m_AllMotionsFinishedLoadingReceiver;
    public string m_AllMotionsFinishedLoadingCallback = "";
    public SmartbodyMotion[] m_MotionsList;
    SmartbodyMotion[] m_Motions;
    bool m_AllMotionsLoaded = false;
    #endregion

    #region Properties
    public string SkeletonName
    {
        get { return m_ReferenceCharacter.SkeletonName; }
    }

    public string BoneParentName
    {
        get { return m_ReferenceCharacter.BoneParentName; }
    }
    #endregion

    #region Functions
    void Awake()
    {
        // instantiate all the motions
        // the SmartbodyMotions need to be instantiated because they start coroutines.
        for (int i = 0; i < m_MotionsList.Length; i++)
        {
            SmartbodyMotion motion = m_MotionsList[i];

            if (motion && motion.gameObject.activeSelf)
            {
                GameObject newObj = (GameObject)UnityEngine.Object.Instantiate(motion.gameObject);
                newObj.name = newObj.name.Replace("(Clone)", "");
                newObj.transform.parent = this.transform;

                m_MotionsList[i] = newObj.GetComponent<SmartbodyMotion>();
            }
        }


        m_Motions = (SmartbodyMotion[])GetComponentsInChildren<SmartbodyMotion>();

        //Debug.Log(string.Format("Motion set {0} has {1} motions", name, m_Motions.Length));
        //StartCoroutine(WaitToLoad(m_SecondsBeforeLoading));
    }


    void Start()
    {
    }


    public void LoadMotions()
    {
        LoadSkeleton();
        LoadJointMap();
        ApplySkeletonToJointMap();
        CreateRetargetPairOnAllCharactersInScene();

        StartCoroutine(WaitToLoad(m_SecondsBeforeLoading));
    }


    public void PlayAllMotions(string characterName)
    {
        StartCoroutine(PlayAllMotions_Internal(characterName));
    }


    IEnumerator PlayAllMotions_Internal(string characterName)
    {
        SmartbodyManager sbm = SmartbodyManager.Get();
        for (int i = 0; i < m_Motions.Length; i++)
        {
            Debug.Log("Playing " + m_Motions[i].MotionName);
            sbm.SBPlayAnim(characterName, m_Motions[i].MotionName);
            yield return new WaitForSeconds(m_Motions[i].MotionLength);
        }
    }


    IEnumerator WaitToLoad(float seconds)
    {
        if (seconds > 0)
        {
            yield return new WaitForSeconds(seconds);
        }

        switch (m_loadType)
        {
            case LoadType.Preload:
                LoadMotionsNow();
                break;

            case LoadType.Streaming_SequentialTime:
                StartCoroutine(LoadMotionsStreaming_Sequential(false));
                break;

            case LoadType.Streaming_SequentialMinFps:
                StartCoroutine(LoadMotionsStreaming_Sequential(true));
                break;

            default:
                LoadMotionsNow();
                break;
        }
    }


    void LoadSkeleton()
    {
        m_ReferenceCharacter.CreateSkeleton();
    }


    void LoadJointMap()
    {
        SmartbodyJointMap jointMap = GetComponent<SmartbodyJointMap>();   // ok if it's null
        if (jointMap == null)
            return;

        SmartbodyManager sbm = SmartbodyManager.Get();
        sbm.AddJointMap(jointMap);
    }


    void ApplySkeletonToJointMap()
    {
        SmartbodyJointMap jointMap = GetComponent<SmartbodyJointMap>();   // ok if it's null
        if (jointMap == null)
            return;

        SmartbodyManager sbm = SmartbodyManager.Get();
        string skeletonName = m_ReferenceCharacter.GetComponent<SmartbodyCharacterInit>().skeletonName;
        sbm.ApplySkeletonToJointMap(jointMap, skeletonName);
    }


    void CreateRetargetPairOnAllCharactersInScene()
    {
        // potentially a new skeleton and/or joint map was created
        // so, go through all the characters in the scene and created retarget pairs for them
        // but only if the character's skeleton has been created.  if not created, hold off and it'll get remapped then.

        SmartbodyManager sbm = SmartbodyManager.Get();
        UnitySmartbodyCharacter [] allCharacters = GameObject.FindObjectsOfType<UnitySmartbodyCharacter>();
        foreach (UnitySmartbodyCharacter character in allCharacters)
        {
            if (character && character.gameObject.activeSelf)
            {
                if (sbm.IsSkeletonLoaded(character.SkeletonName))
                    sbm.CreateRetargetPair(SkeletonName, character.SkeletonName);
            }
        }
    }


    void LoadMotionsNow()
    {
        SmartbodyJointMap jointMap = GetComponent<SmartbodyJointMap>();   // ok if it's null
        string jointMapName = jointMap == null ? "" : jointMap.mapName;

        DateTime startTime = DateTime.Now;
        foreach (SmartbodyMotion motion in m_Motions)
        {
            motion.Load(SkeletonName, jointMapName);
        }

        if (!m_AllMotionsLoaded)  // limit the amount of spam.  If we are reloading motions for real, eg, with ResetLoadFlag(), it won't report this, but that's ok.
            Debug.Log(string.Format("Finished loading motion set {0} ({1} motions) in {2} seconds", name, m_Motions.Length, (DateTime.Now - startTime).TotalSeconds.ToString("f3")));

        FinishedLoadingMotions();
    }


    IEnumerator LoadMotionsStreaming_Sequential(bool requireMinimumFramerate)
    {
        SmartbodyJointMap jointMap = GetComponent<SmartbodyJointMap>();   // ok if it's null
        string jointMapName = jointMap == null ? "" : jointMap.mapName;

        DateTime startTime = DateTime.Now;

        FpsCounter fpsCounter = null;
        if (requireMinimumFramerate)
        {
            fpsCounter = m_fpsCounter;
            if (fpsCounter == null)
            {
                fpsCounter = FindObjectOfType<FpsCounter>();
                if (fpsCounter == null)
                {
                    Debug.LogError("LoadMotionsStreaming_Sequential() - cannot find FPSCounter object in scene - " + name);
                }
            }
        }

        for (int i = 0; i < m_Motions.Length; i++)
        {
            SmartbodyMotion motion = m_Motions[i];

            if (requireMinimumFramerate)
            {
                while (fpsCounter.AverageFps < m_MinimumFramerate)
                {
                    // TODO: add a emergency break if we do this for too long.  Otherwise, on slow machines, we may never load all the motions

                    yield return new WaitForEndOfFrame();
                }

                yield return StartCoroutine(motion.LoadStreaming(SkeletonName, jointMapName));
            }
            else
            {
                float streamingTimePerMotion = m_StreamingCompletionTime / m_Motions.Length;
                float streamingTimeUpToThisMotion = streamingTimePerMotion * i;

                while ((DateTime.Now - startTime).TotalSeconds < streamingTimeUpToThisMotion)
                {
                    yield return new WaitForEndOfFrame();
                }

                yield return StartCoroutine(motion.LoadStreaming(SkeletonName, jointMapName));
            }
        }

        if (!m_AllMotionsLoaded)  // limit the amount of spam.  If we are reloading motions for real, eg, with ResetLoadFlag(), it won't report this, but that's ok.
            Debug.Log(string.Format("Finished loading motion set {0} ({1} motions) in {2} seconds", name, m_Motions.Length, (DateTime.Now - startTime).TotalSeconds.ToString("f3")));

        FinishedLoadingMotions();
    }


    void FinishedLoadingMotions()
    {
        if (!m_AllMotionsLoaded && m_AllMotionsFinishedLoadingReceiver != null && !string.IsNullOrEmpty(m_AllMotionsFinishedLoadingCallback))
        {
            m_AllMotionsFinishedLoadingReceiver.SendMessage(m_AllMotionsFinishedLoadingCallback, this);
        }
        m_AllMotionsLoaded = true;
    }


    public void ResetLoadFlag()
    {
        foreach (var motion in m_Motions)
        {
            motion.ResetLoadFlag();
        }
    }
    #endregion
}
