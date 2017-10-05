using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System;
using System.Text;

public class SmartBodyEvents : GenericEvents
{
    #region Functions
    public override string GetEventType() { return GenericEventNames.SmartBody; }
    #endregion

    #region Events
    public class SmartBodyEvent_Base : ICutsceneEventInterface
    {
        #region Functions
        public static ICharacter FindCharacter(string gameObjectName, string eventName)
        {
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return null;
            }

            ICharacter[] chrs = (ICharacter[])GameObject.FindObjectsOfType(typeof(ICharacter));
            foreach (ICharacter chr in chrs)
            {
                if (chr.CharacterName == gameObjectName || chr.gameObject.name == gameObjectName)
                {
                    return chr;
                }
            }

            Debug.LogWarning(string.Format("Couldn't find Character {0} in the scene. Event {1} needs to be looked at", gameObjectName, eventName));
            return null;
        }

        protected string GetObjectName(CutsceneEvent ce, string objectParamName)
        {
            CutsceneEventParam param = ce.FindParameter(objectParamName);
            UnityEngine.Object o = param.objData;
            return o != null ? o.name : param.stringData;
        }

        static public SmartbodyMotion FindMotion(string motionName)
        {
            if (string.IsNullOrEmpty(motionName))
            {
                return null;
            }

            SmartbodyMotion[] motions = FindObjectsOfType<SmartbodyMotion>();

            for (int i = 0; i < motions.Length; i++)
            {
                if (motions[i].MotionName == motionName)
                {
                    return motions[i];
                }
            }

            Debug.LogError(string.Format("Motion {0} doesn't exist in the scene. It has to be added", motionName));
            return null;
        }

        protected StringBuilder AppendParam<T>(StringBuilder builder, CutsceneEvent ce, string attName, string paramName)
        {
            return AppendParam<T>(builder, ce, attName, paramName, false);
        }

        protected StringBuilder AppendParam<T>(StringBuilder builder, CutsceneEvent ce, string attName, string paramName, bool requiresTimeOffset)
        {
            const string Replace = " />";
            if (ce.DoesParameterExist(paramName))
            {
                if (typeof(T) == typeof(int))
                {
                    builder = builder.Replace(Replace, string.Format(@"{0}=""{1}"" {2}", attName, Param(ce, ce.FindParameterIndex(paramName)).intData, Replace));
                }
                else if (typeof(T) == typeof(float))
                {
                    float data = Param(ce, ce.FindParameterIndex(paramName)).floatData;
                    if (requiresTimeOffset)
                    {
                        data += ce.StartTime;
                    }
                    builder = builder.Replace(Replace, string.Format(@"{0}=""{1}"" {2}", attName, data.ToString("f3"), Replace));
                }
                else if (typeof(T) == typeof(string))
                {
                    string data = Param(ce, ce.FindParameterIndex(paramName)).stringData;
                    if (!string.IsNullOrEmpty(data))
                    {
                        builder = builder.Replace(Replace, string.Format(@"{0}=""{1}"" {2}", attName, data, Replace));
                    }
                }
                else if (typeof(T) == typeof(bool))
                {
                    builder = builder.Replace(Replace, string.Format(@"{0}=""{1}"" {2}", attName, Param(ce, ce.FindParameterIndex(paramName)).boolData.ToString(), Replace));
                }
                else if (typeof(T) == typeof(Enum))
                {
                    builder = builder.Replace(Replace, string.Format(@"{0}=""{1}"" {2}", attName, Param(ce, ce.FindParameterIndex(paramName)).enumDataString, Replace));
                }
                else if (typeof(T) == typeof(SmartbodyMotion))
                {
                    builder = builder.Replace(Replace, string.Format(@"{0}=""{1}"" {2}", attName, Cast<SmartbodyMotion>(ce, ce.FindParameterIndex(paramName)).MotionName, Replace));
                }
                else
                {
                    builder = builder.Replace(Replace, string.Format(@"{0}=""{1}"" {2}", attName, Param(ce, ce.FindParameterIndex(paramName)).objData.name, Replace));
                }
            }
            else
            {
                //Debug.LogError(string.Format("parameter {0} doesn't exist in event {1}", paramName, ce.Name));
            }
            return builder;
        }

        static public UnityEngine.Object FindObject(string assetPath, Type assetType, string fileExtension)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            UnityEngine.Object retVal = null;
#if UNITY_EDITOR
            retVal = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, assetType);
            if (retVal == null)
            {
                // try a project search, this is slow but doesn't require a media path
                //Debug.Log(string.Format("looking for: ", ));
                string dir = string.Format("{0}/{1}", Application.dataPath, assetPath);
                if (Directory.Exists(dir) || assetType == typeof(AudioClip))
                {
                    string[] files = VHFile.DirectoryWrapper.GetFiles(string.Format("{0}", Application.dataPath), assetPath + fileExtension, SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        files[0] = files[0].Replace("\\", "/"); // unity doesn't like backslashes in the asset path
                        files[0] = files[0].Replace(Application.dataPath, "");
                        files[0] = files[0].Insert(0, "Assets");
                        retVal = UnityEditor.AssetDatabase.LoadAssetAtPath(files[0], assetType);
                    }
                }
            }

            // if it's still null, it wasn't found at all
            if (retVal == null)
            {
                Debug.LogError(string.Format("Couldn't load {0} {1}", assetType, assetPath));
            }
#endif
            return retVal;
        }

        protected string GetSkmPath(CutsceneEvent ce)
        {
            string assetPath = "";
#if UNITY_EDITOR
            UnityEngine.Object obj = ce.FindParameter("skm").objData;
            string skmName = ce.FindParameter("skm").stringData;
            if (obj != null)
            {
                assetPath = UnityEditor.AssetDatabase.GetAssetPath(obj);
            }
            else if (!string.IsNullOrEmpty(skmName))
            {
                string[] files = Directory.GetFiles(string.Format("{0}/StreamingAssets", Application.dataPath), string.Format("{0}.skm", skmName), SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    assetPath = files[0].Replace("\\", "/");
                }
            }
#endif
            return assetPath;
        }
        #endregion
    }

    public class SmartBodyEvent_RunPythonScript : SmartBodyEvent_Base
    {
        #region Functions
        public void RunPythonScript(string script)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBRunPythonScript(script);
            else
                SmartbodyManager.Get().SBRunPythonScript(script);
        }
        #endregion
    }

    public class SmartBodyEvent_MoveCharacter : SmartBodyEvent_Base
    {
        #region Functions
        public void MoveCharacter(ICharacter character, string direction, float fSpeed, float fLrps, float fFadeOutTime)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBMoveCharacter(character.CharacterName, direction, fSpeed, fLrps, fFadeOutTime);
            else
                SmartbodyManager.Get().SBMoveCharacter(character.CharacterName, direction, fSpeed, fLrps, fFadeOutTime);
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                return SaveTransformHierarchy(Cast<ICharacter>(ce, 0).transform);
            }
            else
            {
                return null;
            }
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            LoadTransformHierarchy((List<TransformData>)rData, Cast<ICharacter>(ce, 0).transform);
            SmartbodyManager.Get().QueueCharacterToUpload(Cast<ICharacter>(ce, 0));
        }
        #endregion
    }

    public class SmartBodyEvent_WalkTo : SmartBodyEvent_Base
    {
        #region Functions
        public void WalkTo(ICharacter character, ICharacter waypoint, bool isRunning)
        {
            WalkTo(character.CharacterName, waypoint.CharacterName, isRunning);
        }

        public void WalkTo(string character, string waypoint, bool isRunning)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBWalkTo(character, waypoint, isRunning);
            else
                SmartbodyManager.Get().SBWalkTo(character, waypoint, isRunning);
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                return SaveTransformHierarchy(Cast<ICharacter>(ce, 0).transform);
            }
            else
            {
                return null;
            }
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            LoadTransformHierarchy((List<TransformData>)rData, Cast<ICharacter>(ce, 0).transform);
            SmartbodyManager.Get().QueueCharacterToUpload(Cast<ICharacter>(ce, 0));
        }
        #endregion
    }

    public class SmartBodyEvent_WalkImmediate : SmartBodyEvent_Base
    {
        #region Functions
        public void WalkImmediate(ICharacter character, string locomotionPrefix, float velocity, float turn, float strafe)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBWalkImmediate(character.CharacterName, locomotionPrefix, velocity, turn, strafe);
            else
                SmartbodyManager.Get().SBWalkImmediate(character.CharacterName, locomotionPrefix, velocity, turn, strafe);
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                return SaveTransformHierarchy(Cast<ICharacter>(ce, 0).transform);
            }
            else
            {
                return null;
            }
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            LoadTransformHierarchy((List<TransformData>)rData, Cast<ICharacter>(ce, 0).transform);
            SmartbodyManager.Get().QueueCharacterToUpload(Cast<ICharacter>(ce, 0));
        }
        #endregion
    }

    public class SmartBodyEvent_PlayAudio : SmartBodyEvent_Base
    {
        #region Functions
        public void PlayAudio(ICharacter character, AudioClip uttID)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAudio(character.CharacterName, uttID.name);
            else
                SmartbodyManager.Get().SBPlayAudio(character.CharacterName, uttID);
        }

        public void PlayAudio(ICharacter character, AudioClip uttID, string text)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAudio(character.CharacterName, uttID, text);
            else
                SmartbodyManager.Get().SBPlayAudio(character.CharacterName, uttID, text);
        }

        public void PlayAudio(ICharacter character, TextAsset uttID)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAudio(character.CharacterName, uttID.name);
            else
                SmartbodyManager.Get().SBPlayAudio(character.CharacterName, uttID.name);
        }

        public void PlayAudio(ICharacter character, TextAsset uttID, string text)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAudio(character.CharacterName, uttID.name, text);
            else
                SmartbodyManager.Get().SBPlayAudio(character.CharacterName, uttID.name, text);
        }

        public void PlayAudio(ICharacter character, string uttID)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAudio(character.CharacterName, uttID);
            else
                SmartbodyManager.Get().SBPlayAudio(character.CharacterName, uttID);
        }

        public void PlayAudio(ICharacter character, string uttID, string text)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAudio(character.CharacterName, uttID, text);
            else
                SmartbodyManager.Get().SBPlayAudio(character.CharacterName, uttID, text);
        }

        public void PlayAudio(string character, AudioClip uttID, string text)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAudio(character, uttID, text);
            else
                SmartbodyManager.Get().SBPlayAudio(character, uttID, text);
        }

        public void PlayAudio(string character, string uttID, string text)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAudio(character, uttID, text);
            else
                SmartbodyManager.Get().SBPlayAudio(character, uttID, text);
        }

        public override string GetLengthParameterName() { return "uttID"; }

        public override bool NeedsToBeFired (CutsceneEvent ce) { return false; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
            if ((ce.FunctionOverloadIndex == 0 || ce.FunctionOverloadIndex == 1 || ce.FunctionOverloadIndex == 6) && !IsParamNull(ce, 1))
            {
                length = Cast<AudioClip>(ce, 1).length;
            }
            return length;
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.Name = reader["ref"];
            if (ce.FunctionOverloadIndex == 0 || ce.FunctionOverloadIndex == 1)
            {
                AudioClip clip = (AudioClip)FindObject(reader["ref"], typeof(AudioClip), ".wav");
                if (clip != null)
                {
                    ce.FindParameter("uttID").objData = clip;
                }

                if (clip == null)
                {
                    Debug.LogError("Couldn't find audio clip: " + reader["ref"]);
                }
            }

            ce.FindParameter("uttID").stringData = reader["ref"];

            if (ce.FindParameter("text") != null)
            {
                ce.FindParameter("text").stringData = "";// reader.ReadString(); // TODO: Figure out a way to parse this
            }
        }
        #endregion
    }

    public class SmartBodyEvent_PlayXml : SmartBodyEvent_Base
    {
        #region Functions
        public void PlayXml(ICharacter character, string xml)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayXml(character.CharacterName, xml);
            else
                SmartbodyManager.Get().SBPlayXml(character.CharacterName, xml);
        }

        public void PlayXml(ICharacter character, TextAsset xml)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayXml(character.CharacterName, xml.name + ".xml");
            else
                SmartbodyManager.Get().SBPlayXml(character.CharacterName, xml.name + ".xml");
        }

        public override string GetLengthParameterName() { return "xml"; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
#if UNITY_EDITOR
            // when it comes to bml, xml, txt, and .wav files associated with bml markup,
            // they all have the same name, but different file extensions
            string assetPath = "";
            if (ce.FunctionOverloadIndex == 0 && !string.IsNullOrEmpty(Param(ce, 1).stringData))
            {
                assetPath = Param(ce, 1).stringData.Replace('\\', '/');
            }
            else if (ce.FunctionOverloadIndex == 1 && Param(ce, 1).objData != null)
            {
                UnityEngine.Object obj = Param(ce, 1).objData;
                assetPath = UnityEditor.AssetDatabase.GetAssetPath(obj);
            }
            else
            {
                // failed
                return length;
            }
            assetPath = System.IO.Path.ChangeExtension(assetPath, ".wav");

            AudioClip relatedClip = (AudioClip)UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, typeof(AudioClip));
            if (relatedClip != null)
            {
                length = relatedClip.length;
            }
            else
            {
                Debug.LogWarning(string.Format("Couldn't calculate length of xml file {0} because the associated .wav file isn't located in the same folder",
                    ce.FunctionOverloadIndex == 0 ? Param(ce, 1).stringData : Param(ce, 1).objData.name));
            }
#endif
            return length;
        }
        #endregion
    }

    public class SmartBodyEvent_Transform : SmartBodyEvent_Base
    {
        #region Functions
        public void Transform(ICharacter character, Transform transform)
        {
            Transform(character.CharacterName, transform.position.x, transform.position.y, transform.position.z);
        }

        public void Transform(ICharacter character, float x, float y, float z)
        {
            Transform(character.CharacterName, x, y, z);
        }

        public void Transform(ICharacter character, float x, float y, float z, float h, float p, float r)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBTransform(character.CharacterName, x, y, z, h, p, r);
            else
                SmartbodyManager.Get().SBTransform(character.CharacterName, x, y, z, h, p, r);
        }

        public void Transform(ICharacter character, float y, float p)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBTransform(character.CharacterName, y, p);
            else
                SmartbodyManager.Get().SBTransform(character.CharacterName, y, p);
        }

        public void Transform(string character, float x, float y, float z)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBTransform(character, x, y, z);
            else
                SmartbodyManager.Get().SBTransform(character, x, y, z);
        }

        public void Transform(string character, string transform)
        {
            GameObject transformGo = GameObject.Find(transform);
            if (transformGo != null)
            {
                Vector3 pos = transformGo.transform.position;
                Vector3 rot = transformGo.transform.rotation.eulerAngles;
                if (m_MetaData != null)
                    CastMetaData<ICharacterController>().SBTransform(character, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z);
                else
                    SmartbodyManager.Get().SBTransform(character, -pos.x, pos.y, pos.z, -rot.y, rot.x, -rot.z);
            }
            else
            {
                Debug.LogError("Couldn't find gameobject named " + transform);
            }
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                return SaveTransformHierarchy(Cast<ICharacter>(ce, 0).transform);
            }
            else
            {
                return null;
            }
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            if (rData != null)
            {
                Transform rewindData = (Transform)rData;
                Transform characterData = null;
                ICharacter character = null;
                if (IsParamNull(ce, 0))
                {
                    character = FindCharacter(Param(ce, 0).stringData, ce.Name);
                    characterData = character.transform;
                }
                else
                {
                    character = Cast<ICharacter>(ce, 0);
                    characterData = character.transform;
                }

                characterData.position = rewindData.position;
                characterData.rotation = rewindData.rotation;
                SmartbodyManager.Get().QueueCharacterToUpload(character);
            }
        }
        #endregion
    }

    public class SmartBodyEvent_Rotate : SmartBodyEvent_Base
    {
        #region Functions
        public void Rotate(ICharacter character, float h)
        {
            Rotate(character.CharacterName, h);
        }

        public void Rotate(string character, float h)
        {
            SmartbodyManager.Get().SBRotate(character, h);
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                return SaveTransformHierarchy(Cast<ICharacter>(ce, 0).transform);
            }
            else
            {
                return null;
            }
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            Rotate(Cast<ICharacter>(ce, 0), (float)rData);
            SmartbodyManager.Get().QueueCharacterToUpload(Cast<ICharacter>(ce, 0));
        }
        #endregion
    }

    public class SmartBodyEvent_Posture : SmartBodyEvent_Base
    {
        #region Functions
        public void Posture(ICharacter character, SmartbodyMotion motion)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPosture(character.CharacterName, motion.MotionName, 0);
            else
                SmartbodyManager.Get().SBPosture(character.CharacterName, motion.MotionName, 0);
        }

        //public void Posture(ICharacter character, SmartbodyMotion motion, float startTime)
        //{
        //    if (m_MetaData != null)
        //        CastMetaData<ICharacterController>().SBPosture(character.CharacterName, motion.MotionName, startTime);
        //    else
        //        SmartbodyManager.Get().SBPosture(character.CharacterName, motion.MotionName, startTime);
        //}

        public void Posture(ICharacter character, string motion)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPosture(character.CharacterName, motion, 0);
            else
                SmartbodyManager.Get().SBPosture(character.CharacterName, motion, 0);
        }

        //public void Posture(ICharacter character, string motion, float startTime)
        //{
        //    if (m_MetaData != null)
        //        CastMetaData<ICharacterController>().SBPosture(character.CharacterName, motion, startTime);
        //    else
        //        SmartbodyManager.Get().SBPosture(character.CharacterName, motion, startTime);
        //}

        public void Posture(string character, string motion)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPosture(character, motion, 0);
            else
                SmartbodyManager.Get().SBPosture(character, motion, 0);
        }

        public override string GetLengthParameterName() { return "motion"; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
            SmartbodyMotion motion = Cast<SmartbodyMotion>(ce, 1);
            if (motion != null)
            {
                length = motion.MotionLength;
            }
            return length;
        }

        public override string GetXMLString(CutsceneEvent ce)
        {
            StringBuilder builder = new StringBuilder(string.Format(@"<body character=""{0}"" mm:ypos=""{1}"" mm:eventName=""{2}"" mm:overload=""{3}"" start=""{4}"" mm:length=""{5}""  />",
                GetObjectName(ce, "character"), ce.GuiPosition.y, ce.Name, ce.FunctionOverloadIndex, ce.StartTime, ce.Length));

            if (ce.FunctionOverloadIndex == 0)
            {
                AppendParam<SmartbodyMotion>(builder, ce, "posture", "motion");
            }
            else
            {
                AppendParam<string>(builder, ce, "posture", "motion");
            }

            //AppendParam<float>(builder, ce, "start", "startTime");
            return builder.ToString();
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }

            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            if (ce.FunctionOverloadIndex == 0)
            {
                ce.FindParameter("motion").SetObjData(FindMotion(reader["posture"]));
            }
            else
            {
                ce.FindParameter("motion").stringData = reader["posture"];
            }

            ce.Length = 1;
            if (!string.IsNullOrEmpty(reader["mm:length"]))
            {
                ce.Length = ParseFloat(reader["mm:length"], ref ce.Length);
            }
        }
        #endregion
    }

    public class SmartBodyEvent_PlayAnim : SmartBodyEvent_Base
    {
        #region Functions
        public void PlayAnim(ICharacter character, SmartbodyMotion motion)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAnim(character.CharacterName, motion.MotionName);
            else
                SmartbodyManager.Get().SBPlayAnim(character.CharacterName, motion.MotionName);
        }

        public void PlayAnim(ICharacter character, SmartbodyMotion motion, float readyTime,
            float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAnim(character.CharacterName, motion.MotionName, readyTime, strokeStartTime, emphasisTime, strokeTime, relaxTime);
            else
                SmartbodyManager.Get().SBPlayAnim(character.CharacterName, motion.MotionName, readyTime, strokeStartTime, emphasisTime, strokeTime, relaxTime);
        }

        public void PlayAnim(ICharacter character, string motion)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAnim(character.CharacterName, motion);
            else
                SmartbodyManager.Get().SBPlayAnim(character.CharacterName, motion);
        }

        public void PlayAnim(ICharacter character, string motion, float readyTime,
            float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime)
        {
            if (m_MetaData != null)
            {
                CastMetaData<ICharacterController>().SBPlayAnim(character.CharacterName, motion, readyTime, strokeStartTime, emphasisTime, strokeTime, relaxTime);
            }
            else
                SmartbodyManager.Get().SBPlayAnim(character.CharacterName, motion, readyTime, strokeStartTime, emphasisTime, strokeTime, relaxTime);
        }

        public void PlayAnim(string character, string motion)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayAnim(character, motion);
            else
                SmartbodyManager.Get().SBPlayAnim(character, motion);
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                return SaveTransformHierarchy(Cast<ICharacter>(ce, 0).transform);
            }
            else
            {
                return null;
            }
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                LoadTransformHierarchy((List<TransformData>)rData, Cast<ICharacter>(ce, 0).transform);
                SmartbodyManager.Get().QueueCharacterToUpload(Cast<ICharacter>(ce, 0));
            }
        }

        public override string GetLengthParameterName() { return "motion"; }
        public override bool NeedsToBeFired (CutsceneEvent ce) { return false; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
            SmartbodyMotion motion = Cast<SmartbodyMotion>(ce, 1);
            if (motion != null)
            {
                length = motion.MotionLength;
            }
            return length;
        }

        public override string GetXMLString(CutsceneEvent ce)
        {
#if UNITY_EDITOR
            string animationName = string.Empty;
            string animationPath = string.Empty;

            CutsceneEventParam motionParam = ce.FindParameter("motion");
            if (motionParam != null)
            {
                if (motionParam.objData != null)
                {
                    animationName = (motionParam.objData as SmartbodyMotion).MotionName;
                }
                else
                {
                    animationName = motionParam.stringData;
                }
            }
            else
            {
                UnityEngine.Object skm = ce.FindParameter("skm").objData;
                if (skm != null)
                {
                    animationName = skm.name;
                    animationPath = UnityEditor.AssetDatabase.GetAssetPath(skm);
                }
                else
                {
                    animationName = ce.FindParameter("skm").stringData;
                }
            }

            float readyTime = 0, strokeStartTime = 0, emphasisTime = 0, strokeTime = 0, relaxTime = 0;
            if (ce.m_Params.Count > 2)
            {
                readyTime = ce.FindParameter("readyTime").floatData;
                strokeStartTime = ce.FindParameter("strokeStartTime").floatData;
                emphasisTime = ce.FindParameter("emphasisTime").floatData;
                strokeTime = ce.FindParameter("strokeTime").floatData;
                relaxTime = ce.FindParameter("relaxTime").floatData;

                return string.Format(@"<sbm:animation character=""{0}"" name=""{1}"" start=""{2}"" type=""{3}"" duration=""{4}"" weight=""{5}"" ready=""{6}"" strokestart=""{7}""
                                        emphasis=""{8}"" stroke=""{9}"" relax=""{10}"" end=""{11}"" mm:track=""{12}"" mm:ypos=""{13}"" mm:eventName=""{14}"" mm:assetPath=""{15}""/>",
                        GetObjectName(ce, "character"), animationName, ce.StartTime, "body", ce.Length, 1, ce.StartTime + readyTime, ce.StartTime + strokeStartTime, ce.StartTime + emphasisTime,
                        ce.StartTime + strokeTime, ce.StartTime + relaxTime, ce.StartTime + ce.Length, "BODY", ce.GuiPosition.y, ce.Name, animationPath);
            }
            else
            {
                return string.Format(@"<sbm:animation character=""{0}"" name=""{1}"" start=""{2}"" type=""{3}"" duration=""{4}"" weight=""{5}"" mm:track=""{6}"" mm:ypos=""{7}"" mm:eventName=""{8}"" mm:assetPath=""{9}"" mm:overload=""{10}""/>",
                                GetObjectName(ce, "character"), animationName, ce.StartTime, "body", ce.Length, 1, "BODY", ce.GuiPosition.y, ce.Name, animationPath, ce.FunctionOverloadIndex);
            }

#else
            return "";
#endif
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            // this is used for reading in older xml formats
            if (!string.IsNullOrEmpty(reader["id"]))
            {
                ce.Name = reader["id"];
            }
            else if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else
            {
                ce.Name = reader["name"];
            }

            ce.FindParameter("character").stringData = reader["character"];
            ICharacter sbChar = FindCharacter(reader["character"], ce.Name);
            if (sbChar != null)
            {
                ce.FindParameter("character").SetObjData(sbChar);
            }

            CutsceneEventParam motionParam = ce.FindParameter("motion");
            if (motionParam != null) // new motion system
            {
                SmartbodyMotion motion = FindMotion(reader["name"]);
                if (motion != null)
                {
                    motionParam.SetObjData(motion);
                    ce.Length = motion.MotionLength;
                }
                else
                {
                    ce.FindParameter("motion").stringData = reader["name"];
                }
            }
            else // old skm system
            {
                // I took this code out when I removed the object overloads of play anim. Add this back in when unity fixes the missing skm bug in builds
                if (ce.FunctionOverloadIndex == 0 || ce.FunctionOverloadIndex == 1)
                {
                    // try the easy search first. Old xml will not have this attribute
                    if (VHUtils.IsEditor())
                    {
                        ce.FindParameter("skm").SetObjData(FindObject(reader["mm:assetPath"], typeof(SmartbodyMotion), ".skm"));
                    }

                    // if it's still null, it wasn't found at all
                    if (ce.FindParameter("skm").objData == null)
                    {
                        // asset path wasn't found, just try the name
                        ce.FindParameter("skm").SetObjData(FindObject(reader["name"], typeof(SmartbodyMotion), ".skm"));
                    }
                    else
                    {
#if UNITY_EDITOR
                        ce.Length = SmartbodyManager.FindSkmLength(UnityEditor.AssetDatabase.GetAssetPath(ce.FindParameter("skm").objData));
#endif
                    }
                }
            }

            if (VHUtils.IsEditor())
            {
                string attSuffix = "";
                float startTime = 0;
                startTime = ParseFloat(reader["start"], ref startTime);
                if (string.IsNullOrEmpty(reader["ready"]))
                {
                    attSuffix = "_time"; // handles legacy xml
                    startTime = 0;
                }

                if (ce.m_Params.Count > 2)
                {
                    float ready = 0;
                    ce.FindParameter("readyTime").floatData = ParseFloat(reader["ready" + attSuffix], ref ready) - startTime ;
                    float strokeStart = 0;
                    ce.FindParameter("strokeStartTime").floatData = ParseFloat(reader["strokestart" + attSuffix], ref strokeStart) - startTime;
                    float emphasis = 0;
                    ce.FindParameter("emphasisTime").floatData = ParseFloat(reader["emphasis" + attSuffix], ref emphasis) - startTime;
                    float stroke = 0;
                    ce.FindParameter("strokeTime").floatData = ParseFloat(reader["stroke" + attSuffix], ref stroke) - startTime;
                    float relax = 0;
                    ce.FindParameter("relaxTime").floatData = ParseFloat(reader["relax" + attSuffix], ref relax) - startTime;
                }
            }
        }
        #endregion
    }

    public class SmartBodyEvent_PlayFAC : SmartBodyEvent_Base
    {
        #region Functions
        public void PlayFAC(ICharacter character, int au, CharacterDefines.FaceSide side, float weight, float duration)
        {
            PlayFAC(character.CharacterName, au, side, weight, duration);
        }

        public void PlayFAC(ICharacter character, int au, CharacterDefines.FaceSide side, float weight, float duration, float readyTime, float relaxTime)
        {
            PlayFAC(character.CharacterName, au, side, weight, duration, readyTime, relaxTime);
        }

        public void PlayFAC(string character, int au, CharacterDefines.FaceSide side, float weight, float duration)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayFAC(character, au, side, weight, duration);
            else
                SmartbodyManager.Get().SBPlayFAC(character, au, side, weight, duration);
        }

        public void PlayFAC(string character, int au, CharacterDefines.FaceSide side, float weight, float duration, float readyTime, float relaxTime)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayFAC(character, au, side, weight, duration);
            else
                SmartbodyManager.Get().SBPlayFAC(character, au, side, weight, duration, readyTime, relaxTime);
        }

        public override string GetLengthParameterName() { return "duration"; }

        public override string GetXMLString(CutsceneEvent ce)
        {
            float readyTime = 0, relaxTime = 0;
            if (ce.FunctionOverloadIndex == 0  || ce.FunctionOverloadIndex == 2)
            {
                return string.Format(@"<face type=""FACS"" mm:eventName=""{1}"" au=""{0}"" side=""{2}"" start=""{3}"" end=""{4}"" amount=""{5}"" mm:ypos=""{6}"" character=""{7}"" mm:overload=""{8}"" />",
                    ce.FindParameter("au").intData, ce.Name, ce.FindParameter("side").enumDataString, ce.StartTime, ce.EndTime, ce.FindParameter("weight").floatData,
                    ce.GuiPosition.y, GetObjectName(ce, "character"), ce.FunctionOverloadIndex);
            }
            else
            {
                readyTime = ce.FindParameter("readyTime").floatData;
                relaxTime = ce.FindParameter("relaxTime").floatData;

                return string.Format(@"<face type=""FACS"" mm:eventName=""{1}"" au=""{0}"" side=""{2}"" start=""{3}"" ready=""{4}"" relax=""{5}"" end=""{6}"" amount=""{7}"" mm:ypos=""{8}"" character=""{9}"" mm:overload=""{10}"" />",
                    ce.FindParameter("au").intData, ce.Name, ce.FindParameter("side").enumDataString, ce.StartTime, ce.StartTime + readyTime, ce.StartTime + relaxTime, ce.EndTime, ce.FindParameter("weight").floatData,
                    ce.GuiPosition.y, GetObjectName(ce, "character"), ce.FunctionOverloadIndex);
            }
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.FindParameter("au").intData = int.Parse(reader["au"]);
            int au = ce.FindParameter("au").intData;
            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);
            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            if (ce.FindParameter("character").objData == null)
            {
                ce.FindParameter("character").stringData = reader["character"];
            }

            if (ce.FunctionOverloadIndex == 1)
            {
                float ready = 0;
                ce.FindParameter("readyTime").floatData = ParseFloat(reader["ready"], ref ready) - ce.StartTime;
                float relax = 0;
                ce.FindParameter("relaxTime").floatData = ParseFloat(reader["relax"], ref relax) - ce.StartTime;
            }

            ce.FindParameter("weight").floatData = ParseFloat(reader["amount"], ref ce.FindParameter("weight").floatData);

            if (!string.IsNullOrEmpty(reader["side"]))
            {
                ce.FindParameter("side").SetEnumData((CharacterDefines.FaceSide)Enum.Parse(typeof(CharacterDefines.FaceSide), reader["side"]));
            }

            if (!string.IsNullOrEmpty(reader["duration"]))
            {
                ce.FindParameter("duration").floatData = ce.Length = ParseFloat(reader["duration"], ref ce.Length);
            }
            else
            {
                float endTime = 0;
                endTime = ParseFloat(reader["end"], ref endTime);
                ce.FindParameter("duration").floatData = ce.Length = endTime - ce.StartTime;
            }

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else
            {
                ce.Name = string.Format("FAC {0}", CharacterDefines.AUToFacialLookUp[au]);
            }
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "au")
            {
                param.intData = 26; // jaw
            }
            else if (param.Name == "weight")
            {
                param.floatData = 1.0f;
            }
        }
        #endregion
    }

    public class SmartbBodyEvent_PlayViseme : SmartBodyEvent_Base
    {
        #region Functions
        public void PlayViseme(ICharacter character, string viseme, float weight, float duration, float blendTime)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayViseme(character.CharacterName, viseme, weight, duration, blendTime);
            else
                SmartbodyManager.Get().SBPlayViseme(character.CharacterName, viseme, weight, duration, blendTime);
        }

        public void PlayViseme(string character, string viseme, float weight, float duration, float blendTime)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBPlayViseme(character, viseme, weight, duration, blendTime);
            else
                SmartbodyManager.Get().SBPlayViseme(character, viseme, weight, duration, blendTime);
        }

        public override string GetLengthParameterName() { return "duration"; }

        public override string GetXMLString(CutsceneEvent ce)
        {
            string character = GetObjectName(ce, "character");
            string viseme = ce.FindParameter("viseme").stringData;
            float weight = ce.FindParameter("weight").floatData;
            float blendTime = ce.FindParameter("blendTime").floatData;
            float duration = ce.FindParameter("duration").floatData;

            string messageStart = string.Format(@"scene.command('char {0} viseme {1} {2} {3}')", character, viseme, weight, blendTime);
            string messageStop = string.Format(@"scene.command('char {0} viseme {1} {2} {3}')", character, viseme, 0, blendTime);
            return string.Format(@"<event message=""{0}"" start=""{1}"" mm:ypos=""{2}"" mm:eventName=""{3}"" mm:messageType=""visemeStart"" character=""{4}"" viseme=""{5}"" weight=""{6}"" blendTime=""{7}"" duration=""{8}"" mm:overload=""{9}"" />",
                messageStart, ce.StartTime, ce.GuiPosition.y, ce.Name, character, viseme, weight, blendTime, duration, ce.FunctionOverloadIndex)
                 + string.Format(@"<event message=""{0}"" start=""{1}"" mm:ypos=""{2}"" mm:eventName=""{3}"" mm:messageType=""visemeStop""  character=""{4}"" viseme=""{5}"" weight=""{6}"" blendTime=""{7}"" duration=""{8}"" mm:overload=""{9}"" />",
                 messageStop, ce.StartTime + (duration - blendTime), ce.GuiPosition.y, ce.Name, character, viseme, weight, blendTime, duration, ce.FunctionOverloadIndex);
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            float startTime;
            if (!string.IsNullOrEmpty(reader["start"]))
            {
                if (float.TryParse(reader["start"], out startTime))
                {
                    ce.StartTime = startTime;
                }
            }
            else if (!string.IsNullOrEmpty(reader["stroke"]))
            {
                if (float.TryParse(reader["stroke"], out startTime))
                {
                    ce.StartTime = startTime;
                }
            }

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else
            {
                ce.Name = reader["viseme"];
            }

            if (ce.FunctionOverloadIndex == 0)
            {
                ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            }
            else
            {
                ce.FindParameter("character").stringData = reader["character"];
            }

            ce.FindParameter("viseme").stringData = reader["viseme"];
            ce.FindParameter("weight").floatData = ParseFloat(reader["weight"], ref ce.FindParameter("weight").floatData );
            ce.FindParameter("blendTime").floatData = ParseFloat(reader["blendTime"], ref ce.FindParameter("blendTime").floatData);
            ce.Length = ce.FindParameter("duration").floatData = ParseFloat(reader["duration"], ref ce.Length);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "viseme")
            {
                param.stringData = "open";
            }
            else if (param.Name == "weight")
            {
                param.floatData = 1.0f;
            }
            else if (param.Name == "duration")
            {
                param.floatData = 2.0f;
            }
            else if (param.Name == "blendTime")
            {
                param.floatData = 1.0f;
            }
        }
        #endregion
    }

    public class SmartBodyEvent_Nod : SmartBodyEvent_Base
    {
        #region Functions
        public void Nod(ICharacter character, float amount, float repeats, float time)
        {
            Nod(character.CharacterName, amount, repeats, time);
        }

        public void Nod(string character, float amount, float repeats, float time)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBNod(character, amount, repeats, time);
            else
                SmartbodyManager.Get().SBNod(character, amount, repeats, time);
        }

        public void Nod(string character, float amount, float repeats, float time, float velocity)
        {
            SmartbodyManager.Get().SBNod(character, amount, repeats, time, velocity);
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                return SaveTransformHierarchy(Cast<ICharacter>(ce, 0).transform);
            }
            else
            {
                return null;
            }
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                LoadTransformHierarchy((List<TransformData>)rData, Cast<ICharacter>(ce, 0).transform);
                SmartbodyManager.Get().QueueCharacterToUpload(Cast<ICharacter>(ce, 0));
            }
        }

        public override string GetLengthParameterName() { return "time"; }

        public override string GetXMLString(CutsceneEvent ce)
        {
            float velocity = (ce.DoesParameterExist ("velocity") == true) ? ce.FindParameter ("velocity").floatData : 1;

            return string.Format(@"<head start=""{0}"" type=""{1}"" repeats=""{2}"" amount=""{3}"" mm:track=""{4}"" mm:ypos=""{5}"" mm:eventName=""{6}"" end=""{7}"" character=""{8}"" velocity=""{9}"" mm:overload=""{10}""/>",
                                 ce.StartTime, "NOD", ce.FindParameter("repeats").floatData, ce.FindParameter("amount").floatData, "NOD", ce.GuiPosition.y, ce.Name,
                                 ce.FindParameter("time").floatData + ce.StartTime, GetObjectName(ce, "character"), velocity.ToString(), ce.FunctionOverloadIndex);
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.FindParameter("repeats").floatData = ParseFloat(reader["repeats"], ref ce.FindParameter("repeats").floatData);
            ce.FindParameter("amount").floatData = ParseFloat(reader["amount"], ref ce.FindParameter("amount").floatData);
            //ce.EventData.NodVelocity = ParseFloat(reader["velocity"]);
            if (!string.IsNullOrEmpty(reader["start"]))
            {
                ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime );
            }

            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            if (ce.FindParameter("character") == null)
            {
                ce.FindParameter("character").SetObjData(FindCharacter(reader["participant"], ce.Name));
            }

            // it's still null stringify it
            if (ce.FindParameter("character").objData == null || string.IsNullOrEmpty(ce.FindParameter("character").stringData))
            {
                ce.FindParameter("character").stringData = reader["character"];
            }

            if (!string.IsNullOrEmpty(reader["duration"]))
            {
                float dur = 0;
                ce.FindParameter("time").floatData = ce.Length = (ParseFloat(reader["duration"], ref dur) - ce.StartTime);
            }
            else if (!string.IsNullOrEmpty(reader["end"]))
            {
                float end = 0;
                ce.FindParameter("time").floatData = ce.Length = (ParseFloat(reader["end"], ref end) - ce.StartTime);
            }
            else
            {
                ce.FindParameter("time").floatData = ce.Length = 1;
            }

            if (!string.IsNullOrEmpty(reader["velocity"]))
            {
                //ce.FindParameter("velocity").floatData = ParseFloat(reader["velocity"], ref ce.FindParameter("velocity").floatData);
            }
            else
            {
                //ce.FindParameter("velocity").floatData = 0.5f;
            }

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else if (string.IsNullOrEmpty(ce.Name))
            {
                ce.Name = string.Format("Head Nod");
            }
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "amount")
            {
                param.floatData = 1;
            }
            else if (param.Name == "repeats")
            {
                param.floatData = 2.0f;
            }
            else if (param.Name == "time")
            {
                param.floatData = 1.0f;
            }
            else if (param.Name == "velocity")
            {
                param.floatData = 1.0f;
            }
        }
        #endregion
    }

    public class SmartBodyEvent_Shake : SmartBodyEvent_Base
    {
        #region Functions
        public void Shake(ICharacter character, float amount, float repeats, float time)
        {
            Shake(character.CharacterName, amount, repeats, time);
        }

        public void Shake(string character, float amount, float repeats, float time)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBShake(character, amount, repeats, time);
            else
                SmartbodyManager.Get().SBShake(character, amount, repeats, time);
        }

        public void Shake(string character, float amount, float repeats, float time, float velocity)
        {
            SmartbodyManager.Get().SBShake(character, amount, repeats, time, velocity);
        }

        public override object SaveRewindData(CutsceneEvent ce)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                return SaveTransformHierarchy(Cast<ICharacter>(ce, 0).transform);
            }
            else
            {
                return null;
            }
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                LoadTransformHierarchy((List<TransformData>)rData, Cast<ICharacter>(ce, 0).transform);
                SmartbodyManager.Get().QueueCharacterToUpload(Cast<ICharacter>(ce, 0));
            }
        }

        public override string GetLengthParameterName() { return "time"; }

        public override string GetXMLString(CutsceneEvent ce)
        {
            float velocity = (ce.DoesParameterExist ("velocity") == true) ? ce.FindParameter ("velocity").floatData : 1;

            return string.Format(@"<head start=""{0}"" type=""{1}"" repeats=""{2}"" amount=""{3}"" track=""{4}"" mm:ypos=""{5}"" mm:eventName=""{6}"" end=""{7}"" character=""{8}"" velocity=""{9}"" mm:overload=""{10}""/>",
                                 ce.StartTime, "SHAKE", ce.FindParameter("repeats").floatData, ce.FindParameter("amount").floatData, "NOD", ce.GuiPosition.y, ce.Name,
                                 ce.FindParameter("time").floatData + ce.StartTime, GetObjectName(ce, "character"), velocity.ToString(), ce.FunctionOverloadIndex);
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.FindParameter("repeats").floatData = ParseFloat(reader["repeats"], ref ce.FindParameter("repeats").floatData);
            ce.FindParameter("amount").floatData = ParseFloat(reader["amount"], ref ce.FindParameter("amount").floatData);
            //ce.EventData.NodVelocity = ParseFloat(reader["velocity"]);
            if (!string.IsNullOrEmpty(reader["start"]))
            {
                ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);
            }

            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            if (ce.FindParameter("character") == null)
            {
                ce.FindParameter("character").SetObjData(FindCharacter(reader["participant"], ce.Name));
            }

            // it's still null stringify it
            if (ce.FindParameter("character").objData == null || string.IsNullOrEmpty(ce.FindParameter("character").stringData))
            {
                ce.FindParameter("character").stringData = reader["character"];
            }

            if (!string.IsNullOrEmpty(reader["duration"]))
            {
                float dur = 0;
                ce.FindParameter("time").floatData = ce.Length = (ParseFloat(reader["duration"], ref dur) - ce.StartTime);
            }
            else if (!string.IsNullOrEmpty(reader["end"]))
            {
                float end = 0;
                ce.FindParameter("time").floatData = ce.Length = (ParseFloat(reader["end"], ref end) - ce.StartTime);
            }
            else
            {
                ce.FindParameter("time").floatData = ce.Length = 1;
            }

            if (!string.IsNullOrEmpty(reader["velocity"]))
            {
                //ce.FindParameter("velocity").floatData = ParseFloat(reader["velocity"], ref ce.FindParameter("velocity").floatData);
            }

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else if (string.IsNullOrEmpty(ce.Name))
            {
                ce.Name = string.Format("Head Nod");
            }
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "amount")
            {
                param.floatData = 1;
            }
            else if (param.Name == "repeats")
            {
                param.floatData = 2.0f;
            }
            else if (param.Name == "time")
            {
                param.floatData = 1.0f;
            }
            else if (param.Name == "velocity")
            {
                param.floatData = 1.0f;
            }
        }
        #endregion
    }

    public class SmartBodyEvent_Gaze : SmartBodyEvent_Base
    {
        #region Functions
        public void Gaze(ICharacter character, ICharacter gazeAt)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBGaze(character.CharacterName, gazeAt.CharacterName);
            else
                SmartbodyManager.Get().SBGaze(character.CharacterName, gazeAt.CharacterName);
        }

        public void Gaze(ICharacter character, ICharacter gazeAt, float neckSpeed)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBGaze(character.CharacterName, gazeAt.CharacterName, neckSpeed);
            else
                SmartbodyManager.Get().SBGaze(character.CharacterName, gazeAt.CharacterName, neckSpeed);
        }

        public void Gaze(ICharacter character, ICharacter gazeAt, float neckSpeed, float eyeSpeed, CharacterDefines.GazeJointRange jointRange)
        {
            Gaze(character.CharacterName, gazeAt.CharacterName, neckSpeed, eyeSpeed, jointRange);
        }

        public void Gaze(string character, string gazeAt, float neckSpeed, float eyeSpeed, CharacterDefines.GazeJointRange jointRange)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBGaze(character, gazeAt, neckSpeed, eyeSpeed, jointRange);
            else
                SmartbodyManager.Get().SBGaze(character, gazeAt, neckSpeed, eyeSpeed, jointRange);
        }

        string GazeTargetName(CutsceneEvent ce)
        {
            string gazeTargetName = "NO_GAZE_TARGET";
            if (ce.FunctionOverloadIndex == 0)
            {
                ICharacter sbChar = Cast<ICharacter>(ce, 1);
                if (sbChar != null && sbChar.gameObject != null)
                {
                    gazeTargetName = sbChar.CharacterName;
                    if (string.IsNullOrEmpty(gazeTargetName))
                    {
                        gazeTargetName = sbChar.gameObject.name;
                    }
                    gazeTargetName = gazeTargetName.Replace(" ", "");
                }
            }
            else
            {
                CutsceneEventParam p = ce.FindParameter("gazeAt");
                if (p != null)
                {
                    gazeTargetName = p.stringData;
                }
            }
            return gazeTargetName;
        }

        public override string GetXMLString(CutsceneEvent ce)
        {
            string jointRangeString = "EYES NECK";
            float neckSpeed = 400;
            float eyeSpeed = 400;
            if (ce.FunctionOverloadIndex > 1)
            {
                CutsceneEventParam jointRangeParam = ce.FindParameter("jointRange");
                if (jointRangeParam != null)
                {
                    jointRangeString = jointRangeParam.enumDataString;
                    if (!string.IsNullOrEmpty(jointRangeString))
                    {
                        jointRangeString = jointRangeString.Replace("_", " ");
                    }
                }

                neckSpeed = ce.FindParameter ("neckSpeed").floatData;
                eyeSpeed = ce.FindParameter ("eyeSpeed").floatData;
            }


            return string.Format(@"<gaze character=""{0}"" mm:eventName=""{1}"" target=""{2}"" start=""{3}"" sbm:joint-range=""{4}"" mm:advanced=""false"" mm:overload=""{5}"" headspeed=""{6}"" eyespeed=""{7}"" sbm:joint-speed=""{6} {7}""/>",
                                 GetObjectName(ce, "character"), ce.Name, GazeTargetName(ce), ce.StartTime, jointRangeString, ce.FunctionOverloadIndex, neckSpeed, eyeSpeed);
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            if (ce.FindParameter("character") == null)
            {
                ce.FindParameter("character").SetObjData(FindCharacter(reader["participant"], ce.Name));
            }

            if (ce.FindParameter("character").objData == null)
            {
                ce.FindParameter("character").stringData = reader["character"];
            }

            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);
            string targetName = reader["target"];

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }

            //ce.FindParameter("headSpeed").floatData = ParseFloat(reader["headspeed"]);
            //ce.FindParameter("eyeSpeed").floatData = ParseFloat(reader["eyespeed"]);

            // we have the target name, so now let's search through the scene looking for the reference
            if (ce.FindParameter("gazeAt").objData == null)
            {
                // there aren't any pawns in the scene with this name, let's do a character search instead.
                ICharacter targetChr = FindCharacter(targetName, ce.Name);
                ce.FindParameter("gazeAt").SetObjData(targetChr);
                //ce.FunctionOverloadIndex = 0;
            }

            if (ce.FindParameter("gazeAt").objData == null)
            {
                ce.FindParameter("gazeAt").stringData = targetName;
                //Debug.LogWarning(string.Format("{0} event {1} has a target named {2} but that target was not found in this scene", "Gaze", ce.Name, targetName));
            }
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "neckSpeed")
            {
                param.floatData = 400;
            }
            else if (param.Name == "eyeSpeed")
            {
                param.floatData = 400;
            }
            else if (param.Name == "jointRange")
            {
                param.SetEnumData(CharacterDefines.GazeJointRange.EYES_NECK);
            }
        }
        #endregion
    }

    public class SmartBodyEvent_GazeAdvanced : SmartBodyEvent_Base
    {
        #region Functions
        public void GazeAdvanced(ICharacter character, ICharacter gazeTarget, string targetBone, CharacterDefines.GazeDirection gazeDirection,
            CharacterDefines.GazeJointRange jointRange, float angle, float headSpeed, float eyeSpeed, float fadeOut)
        {
            GazeAdvanced(character.CharacterName, gazeTarget.CharacterName, targetBone, gazeDirection, jointRange, angle, headSpeed, eyeSpeed, fadeOut);
        }

        public void GazeAdvanced(string character, string gazeTarget, string targetBone, CharacterDefines.GazeDirection gazeDirection,
            CharacterDefines.GazeJointRange jointRange, float angle, float headSpeed, float eyeSpeed, float fadeOut)
        {
            if (character == null || gazeTarget == null)
            {
                return;
            }

            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBGaze(character, gazeTarget, targetBone, gazeDirection, jointRange, angle, headSpeed, eyeSpeed, fadeOut, "", 0);
            else
                SmartbodyManager.Get().SBGaze(character, gazeTarget, targetBone, gazeDirection, jointRange, angle, headSpeed, eyeSpeed, fadeOut, "", 0);
        }

        string GazeTargetName(CutsceneEvent ce)
        {
            string gazeTargetName = "NO_GAZE_TARGET";
            if (ce.FunctionOverloadIndex == 0)
            {
                ICharacter sbChar = Cast<ICharacter>(ce, 1);
                if (sbChar != null && sbChar.gameObject != null)
                {
                    gazeTargetName = sbChar.CharacterName;
                    if (string.IsNullOrEmpty(gazeTargetName))
                    {
                        gazeTargetName = sbChar.gameObject.name;
                    }
                    gazeTargetName = gazeTargetName.Replace(" ", "");
                }
            }
            else
            {
                CutsceneEventParam p = ce.FindParameter("gazeTarget");
                if (p != null)
                {
                    gazeTargetName = p.stringData;
                }
            }

            string targetBone = ce.FindParameter("targetBone").stringData;
            if (!string.IsNullOrEmpty(targetBone))
            {
                targetBone = targetBone.Insert(0, ":");
            }
            return gazeTargetName + targetBone;
        }

        //public override string GetLengthParameterName() { return "duration"; }

        public override string GetXMLString(CutsceneEvent ce)
        {
            string targetBone = ce.FindParameter("targetBone").stringData;
            string jointRangeString = ce.FindParameter("jointRange").enumDataString;
            if (!string.IsNullOrEmpty(jointRangeString))
            {
                jointRangeString = jointRangeString.Replace("_", " ");
            }
            float duration = (ce.DoesParameterExist ("duration") == true) ? ce.FindParameter ("duration").floatData : 1f;

            return string.Format(@"<gaze character=""{0}"" mm:eventName=""{1}"" target=""{2}"" angle=""{3}"" start=""{4}"" duration=""{5}"" headspeed=""{6}"" eyespeed=""{7}"" fadeout=""{8}"" sbm:joint-range=""{9}"" sbm:joint-speed=""{6} {7}"" mm:track=""{10}"" mm:ypos=""{11}"" direction=""{12}"" sbm:handle=""{13}"" targetBone=""{14}"" mm:advanced=""true""  mm:overload=""{15}""/>",
                                 GetObjectName(ce, "character"), ce.Name, GazeTargetName(ce), ce.FindParameter("angle").floatData,
                                 ce.StartTime, duration.ToString (), ce.FindParameter("headSpeed").floatData, ce.FindParameter("eyeSpeed").floatData, ce.FindParameter("fadeOut").floatData,
                                 jointRangeString, "GAZE", ce.GuiPosition.y, ce.FindParameter("gazeDirection").enumDataString, /*ce.FindParameter("gazeHandleName").stringData*/"", targetBone, ce.FunctionOverloadIndex);
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(reader["sbm:joint-range"]))
            {
                //ce.FindParameter("jointRange").SetEnumData((SmartbodyManager.GazeJointRange)Enum.Parse(typeof(SmartbodyManager.GazeJointRange), reader["sbm:joint-range"].ToString().Replace(" ", "_"), true));
                ce.FindParameter("jointRange").SetEnumData(CharacterDefines.ParseGazeJointRange(reader["sbm:joint-range"]));
            }

            if (!string.IsNullOrEmpty(reader["direction"]))
            {
                string direction = reader["direction"];
                if (reader["direction"].IndexOf(' ') != -1)
                {
                    string[] split = direction.Split(' ');
                    direction = split[0];
                }
                ce.FindParameter("gazeDirection").SetEnumData((CharacterDefines.GazeDirection)Enum.Parse(typeof(CharacterDefines.GazeDirection), direction, true));
            }
            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            if (ce.FindParameter("character") == null)
            {
                ce.FindParameter("character").SetObjData(FindCharacter(reader["participant"], ce.Name));
            }
            if (ce.FindParameter("character").objData == null)
            {
                ce.FindParameter("character").stringData = reader["character"];
            }
            ce.FindParameter("angle").floatData = ParseFloat(reader["angle"], ref ce.FindParameter("angle").floatData);
            ce.FindParameter("headSpeed").floatData = ParseFloat(reader["headspeed"], ref ce.FindParameter("headSpeed").floatData);
            ce.FindParameter("eyeSpeed").floatData = ParseFloat(reader["eyespeed"], ref ce.FindParameter("eyeSpeed").floatData);
            ce.FindParameter("fadeOut").floatData = ParseFloat(reader["fadeout"], ref ce.FindParameter("fadeOut").floatData);
            //ce.FindParameter("gazeHandleName").stringData = reader["sbm:handle"];
            ce.FindParameter("targetBone").stringData = reader["targetBone"];
            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);
            //ce.FindParameter("duration").floatData = ce.Length = ParseFloat(reader["duration"]);
            if (ce.Length == 0)
            {
                ce.Length = 1;
            }

            string targetName = reader["target"];
            int colonIndex = targetName.IndexOf(":");
            if (colonIndex != -1)
            {
                // there's a specific bone that needs to be looked at
                targetName = targetName.Remove(colonIndex);
            }

            // we have the target name, so now let's search through the scene looking for the reference
            if (ce.FindParameter("gazeTarget").objData == null)
            {
                // there aren't any pawns in the scene with this name, let's do a character search instead.
                ICharacter targetChr = FindCharacter(targetName, ce.Name);
                ce.FindParameter("gazeTarget").SetObjData(targetChr);
                //ce.FunctionOverloadIndex = 0;
            }

            if (ce.FindParameter("gazeTarget").objData == null)
            {
                Debug.LogWarning(string.Format("{0} event {1} has a target named {2} but that target was not found in this scene", "Gaze", ce.Name, targetName));
            }
            if (ce.FindParameter("gazeTarget").objData == null)
            {
                ce.FindParameter("gazeTarget").stringData = targetName;
                //Debug.LogWarning(string.Format("{0} event {1} has a target named {2} but that target was not found in this scene", "Gaze", ce.Name, targetName));
            }
        }


        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "headSpeed")
            {
                param.floatData = 400;
            }
            else if (param.Name == "eyeSpeed")
            {
                param.floatData = 400;
            }
            else if (param.Name == "jointRange")
            {
                param.SetEnumData(CharacterDefines.GazeJointRange.EYES_NECK);
            }
            else if (param.Name == "targetBone")
            {
                param.SetEnumData(CharacterDefines.GazeTargetBone.NONE);
            }
            else if (param.Name == "gazeDirection")
            {
                param.SetEnumData(CharacterDefines.GazeDirection.NONE);
            }
            else if (param.Name == "duration")
            {
                param.floatData = 1;
            }
            else if (param.Name == "fadeOut")
            {
                param.floatData = 0.25f;
            }
        }
        #endregion
    }

    public class SmartBodyEvent_StopGaze : SmartBodyEvent_Base
    {
        #region Constants
        const float DefaultStopGazeTime = 1;
        #endregion

        #region Functions
        public void StopGaze(ICharacter character)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBStopGaze(character.CharacterName, DefaultStopGazeTime);
            else
                SmartbodyManager.Get().SBStopGaze(character.CharacterName, DefaultStopGazeTime);
        }

        public void StopGaze(ICharacter character, float fadeOut)
        {
            StopGaze(character.CharacterName, fadeOut);
        }

        public void StopGaze(string character, float fadeOut)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBStopGaze(character, fadeOut);
            else
                SmartbodyManager.Get().SBStopGaze(character, fadeOut);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "fadeOut")
            {
                param.floatData = 1;
            }
        }

        public override string GetXMLString(CutsceneEvent ce)
        {
            float fadeOut = 1;
            if (ce.FunctionOverloadIndex > 0)
            {
                fadeOut = ce.FindParameter("fadeOut").floatData;
            }
            return string.Format(@"<event message=""sb scene.command('char {0} gazefade out {1}')"" character=""{0}"" stroke=""{2}"" mm:eventName=""{3}"" mm:ypos=""{4}""  mm:overload=""{5}""/>",
              GetObjectName(ce, "character"), fadeOut, ce.StartTime, ce.Name, ce.GuiPosition.y, ce.FunctionOverloadIndex);
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(reader["start"]))
            {
                float.TryParse(reader["start"], out ce.StartTime);
            }
            else if (!string.IsNullOrEmpty(reader["stroke"]))
            {
                float.TryParse(reader["stroke"], out ce.StartTime);
            }

            if (ce.FunctionOverloadIndex == 1)
            {
                ce.FindParameter("fadeOut").floatData = ParseFloat(reader["fadeOut"], ref ce.FindParameter("fadeOut").floatData);
            }

            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            if (ce.FindParameter("character").objData == null)
            {
                ce.FindParameter("character").stringData = reader["character"];
            }
            ce.Name = reader["mm:eventName"];
        }

        #endregion
    }

    public class SmartBodyEvent_StopSaccade : SmartBodyEvent_Base
    {
        #region Functions
        public void StopSaccade(ICharacter character)
        {
            StopSaccade(character.CharacterName);
        }

        public void StopSaccade(string character)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBStopSaccade(character);
            else
                SmartbodyManager.Get().SBStopSaccade(character);
        }

        public override string GetXMLString(CutsceneEvent ce)
        {
            return string.Format(@"<event message=""sbm bml char {0} &lt;saccade finish=&quot;true&quot; /&gt;"" stroke=""{1}"" mm:ypos=""{2}"" character=""{0}"" mm:stopSaccade=""true"" mm:eventName=""{3}"" />",
                    GetObjectName(ce, "character"), ce.StartTime, ce.GuiPosition.y, ce.Name);
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(reader["stroke"]))
            {
                float.TryParse(reader["stroke"], out ce.StartTime);
            }

            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            ce.Name = reader["mm:eventName"];
        }
        #endregion
    }

    public class SmartBodyEvent_Saccade : SmartBodyEvent_Base
    {
        #region Functions
        public void Saccade(ICharacter character, CharacterDefines.SaccadeType type, bool finish, float duration)
        {
            Saccade(character.CharacterName, type, finish, duration);
        }

        public void Saccade(ICharacter character, CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude)
        {
            Saccade(character.CharacterName, type, finish, duration, angleLimit, direction, magnitude);
        }

        public void Saccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBSaccade(character, type, finish, duration);
            else
                SmartbodyManager.Get().SBSaccade(character, type, finish, duration);
        }

        public void Saccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBSaccade(character, type, finish, duration, angleLimit, direction, magnitude);
            else
                SmartbodyManager.Get().SBSaccade(character, type, finish, duration, angleLimit, direction, magnitude);
        }

        public override string GetLengthParameterName() { return "duration"; }

        public override string GetXMLString(CutsceneEvent ce)
        {
            string returnString = "";

            if (ce.FindParameter("type").enumDataString.ToLower() == "end")
            {
                returnString = string.Format(@"<event message=""sbm bml char {0} &lt;saccade finish=&quot;true&quot; /&gt;"" stroke=""{1}"" mm:ypos=""{2}"" character=""{0}"" mm:stopSaccade=""true"" mm:eventName=""{3}"" />",
                                             GetObjectName(ce, "character"), ce.StartTime, ce.GuiPosition.y, ce.Name);
            }
            else if (ce.FindParameter("finish").boolData == true)
            {
                returnString = string.Format(@"<event message=""sbm bml char {0} &lt;saccade finish=&quot;true&quot; /&gt;"" stroke=""{3}"" type=""{1}"" mm:track=""{4}"" mm:ypos=""{5}"" character=""{0}"" mm:eventName=""{6}"" mm:finish=""{7}""/>",
                                             GetObjectName(ce, "character"), "", 0, ce.StartTime, "Saccade", ce.GuiPosition.y, ce.Name, ce.FindParameter("finish").boolData.ToString().ToLower());
            }
            else if (ce.FindParameter("finish").boolData == false)
            {
                returnString = string.Format(@"<event message=""sbm bml char {0} &lt;saccade mode=&quot;{1}&quot; /&gt;"" stroke=""{3}"" type=""{1}"" mm:track=""{4}"" mm:ypos=""{5}"" character=""{0}"" mm:eventName=""{6}"" mm:finish=""{7}""/>",
                                             GetObjectName(ce, "character"), ce.FindParameter("type").enumDataString.ToLower(), 0, ce.StartTime, "Saccade", ce.GuiPosition.y, ce.Name, ce.FindParameter("finish").boolData.ToString().ToLower());
            }
            return returnString;
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(reader["start"]))
            {
                float.TryParse(reader["start"], out ce.StartTime);
            }
            else if (!string.IsNullOrEmpty(reader["stroke"]))
            {
                float.TryParse(reader["stroke"], out ce.StartTime);
            }

            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));

            ce.FindParameter("type").SetEnumData((CharacterDefines.SaccadeType)Enum.Parse(typeof(CharacterDefines.SaccadeType), reader["type"], true));
            if (!string.IsNullOrEmpty(reader["duration"]))
            {
                ce.FindParameter("duration").floatData = ParseFloat(reader["duration"], ref ce.FindParameter("duration").floatData );
            }

            //ce.Name = ce.FindParameter("type").enumData.ToString();
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "duration")
            {
                param.floatData = 1;
            }
        }
        #endregion
    }

    public class SmartBodyEvent_StateChange : SmartBodyEvent_Base
    {
        #region Functions
        public void StateChange(ICharacter character, string state, string mode, string wrapMode, string scheduleMode)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBStateChange(character.CharacterName, state, mode, wrapMode, scheduleMode);
            else
                SmartbodyManager.Get().SBStateChange(character.CharacterName, state, mode, wrapMode, scheduleMode);
        }

        public void StateChange(ICharacter character, string state, string mode, string wrapMode, string scheduleMode, float x)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBStateChange(character.CharacterName, state, mode, wrapMode, scheduleMode, x);
            else
                SmartbodyManager.Get().SBStateChange(character.CharacterName, state, mode, wrapMode, scheduleMode, x);
        }

        public void StateChange(ICharacter character, string state, string mode, string wrapMode, string scheduleMode, float x, float y, float z)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBStateChange(character.CharacterName, state, mode, wrapMode, scheduleMode, x, y, z);
            else
                SmartbodyManager.Get().SBStateChange(character.CharacterName, state, mode, wrapMode, scheduleMode, x, y, z);
        }
        #endregion
    }

    public class SmartBodyEvent_Express : SmartBodyEvent_Base
    {
        #region Functions
        public void Express(ICharacter character, AudioClip uttID, string uttNum, string text)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBExpress(character.CharacterName, uttID.name, uttNum, text);
            else
                SmartbodyManager.Get().SBExpress(character.CharacterName, uttID.name, uttNum, text);
        }

        public void Express(ICharacter character, string uttID, string uttNum, string text)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBExpress(character.CharacterName, uttID, uttNum, text);
            else
                SmartbodyManager.Get().SBExpress(character.CharacterName, uttID, uttNum, text);
        }

        public void Express(ICharacter character, AudioClip uttID, string uttNum, string text, string target)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBExpress(character.CharacterName, uttID.name, uttNum, text, target);
            else
                SmartbodyManager.Get().SBExpress(character.CharacterName, uttID.name, uttNum, text, target);
        }

        public void Express(ICharacter character, string uttID, string uttNum, string text, string target)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBExpress(character.CharacterName, uttID, uttNum, text, target);
            else
                SmartbodyManager.Get().SBExpress(character.CharacterName, uttID, uttNum, text, target);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "target")
            {
                param.stringData = "user";
            }
        }

        public override string GetLengthParameterName() { return "uttID"; }
        public override bool NeedsToBeFired (CutsceneEvent ce) { return false; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
            if ((ce.FunctionOverloadIndex == 0) && !IsParamNull(ce, 1))
            {
                length = Cast<AudioClip>(ce, 1).length;
            }
            return length;
        }
        #endregion
    }

    public class SmartBodyEvent_PythonCommand : ICutsceneEventInterface
    {
        #region Functions
        public void PythonCommand(string command)
        {
            SmartbodyManager.Get().PythonCommand(command);
        }
        #endregion
    }

    public class SmartBodyEvent_Gesture : SmartBodyEvent_Base
    {
        #region Functions
        public void Gesture(ICharacter character, SmartbodyMotion motion)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBGesture(character.CharacterName, motion.MotionName);
            else
                SmartbodyManager.Get().SBGesture(character.CharacterName, motion.MotionName);
        }

        public void Gesture(ICharacter character, string lexeme, string lexemeType, GestureUtils.Handedness hand, GestureUtils.Style style, GestureUtils.Emotion emotion,
            ICharacter target, bool additive, string jointRange, float perlinFrequency, float perlinScale)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBGesture(character.CharacterName, lexeme, lexemeType, hand, style, emotion,
                    target.CharacterName, additive, jointRange, perlinFrequency, perlinScale, -1, -1, -1, -1, -1);
            else
                SmartbodyManager.Get().SBGesture(character.CharacterName, lexeme, lexemeType, hand, style, emotion,
                    target.CharacterName, additive, jointRange, perlinFrequency, perlinScale, -1, -1, -1, -1, -1);
        }

        public void Gesture(ICharacter character, string lexeme, string lexemeType, GestureUtils.Handedness hand, GestureUtils.Style style, GestureUtils.Emotion emotion,
            ICharacter target, bool additive, string jointRange, float perlinFrequency, float perlinScale,
            float readyTime, float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime)
        {
            if (m_MetaData != null)
                CastMetaData<ICharacterController>().SBGesture(character.CharacterName, lexeme, lexemeType, hand, style, emotion,
                    target.CharacterName, additive, jointRange, perlinFrequency, perlinScale, readyTime, strokeStartTime, emphasisTime, strokeTime, relaxTime);
            else
                SmartbodyManager.Get().SBGesture(character.CharacterName, lexeme, lexemeType, hand, style, emotion,
                    target.CharacterName, additive, jointRange, perlinFrequency, perlinScale, readyTime, strokeStartTime, emphasisTime, strokeTime, relaxTime);
        }

        public override string GetXMLString(CutsceneEvent ce)
        {
            StringBuilder builder = new StringBuilder(string.Format(@"<gesture character=""{0}"" mm:ypos=""{1}"" mm:eventName=""{2}"" mm:overload=""{3}"" start=""{4}""  />",
                GetObjectName(ce, "character"), ce.GuiPosition.y, ce.Name, ce.FunctionOverloadIndex, ce.StartTime));

            AppendParam<SmartbodyMotion>(builder, ce, "name", "motion");
            AppendParam<string>(builder, ce, "lexeme", "lexeme");
            AppendParam<string>(builder, ce, "type", "lexemeType");
            AppendParam<Enum>(builder, ce, "mode", "hand");
            AppendParam<Enum>(builder, ce, "sbm:style", "style");
            AppendParam<Enum>(builder, ce, "emotion", "emotion");
            AppendParam<System.Object>(builder, ce, "target", "target");
            AppendParam<bool>(builder, ce, "sbm:additive", "additive");
            AppendParam<string>(builder, ce, "sbm:joint-range", "jointRange");
            AppendParam<float>(builder, ce, "sbm:frequency", "perlinFrequency");
            AppendParam<float>(builder, ce, "sbm:scale", "perlinScale");
            AppendParam<float>(builder, ce, "ready", "readyTime", true);
            AppendParam<float>(builder, ce, "stoke_start", "strokeStartTime", true);
            AppendParam<float>(builder, ce, "stroke", "emphasisTime", true);
            AppendParam<float>(builder, ce, "stroke_end", "strokeTime", true);
            AppendParam<float>(builder, ce, "relax", "relaxTime", true);

            return builder.ToString();
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);

            if (!string.IsNullOrEmpty(reader["sbm:joint-range"]))
            {
                if (ce.DoesParameterExist("jointRange"))
                    ce.FindParameter("jointRange").stringData = reader["sbm:joint-range"];
            }
            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            if (!string.IsNullOrEmpty(reader["lexeme"]))
            {
                if (ce.DoesParameterExist("lexeme"))
                    ce.FindParameter("lexeme").stringData = reader["lexeme"];
            }
            if (!string.IsNullOrEmpty(reader["type"]))
            {
                if (ce.DoesParameterExist("lexemeType"))
                    ce.FindParameter("lexemeType").stringData = reader["type"];
            }
            if (!string.IsNullOrEmpty(reader["mode"]))
            {
                if (ce.DoesParameterExist("hand"))
                    ce.FindParameter("hand").SetEnumData((GestureUtils.Handedness)Enum.Parse(typeof(GestureUtils.Handedness), reader["mode"], true));
            }
            if (!string.IsNullOrEmpty(reader["sbm:style"]))
            {
                if (ce.DoesParameterExist("style"))
                    ce.FindParameter("style").SetEnumData((GestureUtils.Style)Enum.Parse(typeof(GestureUtils.Style), reader["sbm:style"], true));
            }
            if (!string.IsNullOrEmpty(reader["emotion"]))
            {
                if (ce.DoesParameterExist("emotion"))
                    ce.FindParameter("emotion").SetEnumData((GestureUtils.Emotion)Enum.Parse(typeof(GestureUtils.Emotion), reader["emotion"], true));
            }
            if (!string.IsNullOrEmpty(reader["sbm:additive"]))
            {
                if (ce.DoesParameterExist("additive"))
                    ce.FindParameter("additive").boolData = bool.Parse(reader["sbm:additive"]);
            }
            if (!string.IsNullOrEmpty(reader["sbm:frequency"]))
            {
                if (ce.DoesParameterExist("perlinFrequency"))
                    ce.FindParameter("perlinFrequency").floatData = ParseFloat(reader["sbm:frequency"], ref ce.FindParameter("perlinFrequency").floatData);
            }
            if (!string.IsNullOrEmpty(reader["sbm:scale"]))
            {
                if (ce.DoesParameterExist("perlinScale"))
                    ce.FindParameter("perlinScale").floatData = ParseFloat(reader["sbm:scale"], ref ce.FindParameter("perlinScale").floatData);
            }
            if (!string.IsNullOrEmpty(reader["ready"]))
            {
                if (ce.DoesParameterExist("readyTime"))
                {
                    float ready = 0;
                    ce.FindParameter("readyTime").floatData = ParseFloat(reader["ready"], ref ready) - ce.StartTime;
                }
            }
            if (!string.IsNullOrEmpty(reader["stoke_start"]))
            {
                if (ce.DoesParameterExist("strokeStartTime"))
                {
                    float strokeStart = 0;
                    ce.FindParameter("strokeStartTime").floatData = ParseFloat(reader["stoke_start"], ref strokeStart) - ce.StartTime;
                }
            }
            if (!string.IsNullOrEmpty(reader["stroke"]))
            {
                if (ce.DoesParameterExist("emphasisTime"))
                {
                    float emphasis = 0;
                    ce.FindParameter("emphasisTime").floatData = ParseFloat(reader["stroke"], ref emphasis) - ce.StartTime;
                }
            }
            if (!string.IsNullOrEmpty(reader["stroke_end"]))
            {
                if (ce.DoesParameterExist("strokeTime"))
                {
                    float strokeTime = 0;
                    ce.FindParameter("strokeTime").floatData = ParseFloat(reader["stroke_end"], ref strokeTime) - ce.StartTime;
                }
            }
            if (!string.IsNullOrEmpty(reader["relax"]))
            {
                if (ce.DoesParameterExist("relaxTime"))
                {
                    float relax = 0;
                    ce.FindParameter("relaxTime").floatData = ParseFloat(reader["relax"], ref relax) - ce.StartTime;
                }
            }
            if (!string.IsNullOrEmpty(reader["name"]))
            {
                ce.FindParameter("motion").SetObjData(FindMotion(reader["name"]));
            }

            ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            if (ce.FindParameter("character") == null)
            {
                ce.FindParameter("character").SetObjData(FindCharacter(reader["participant"], ce.Name));
            }

            if (ce.Length == 0)
            {
                ce.Length = 1;
            }

            if (!string.IsNullOrEmpty(reader["target"]))
            {
                // we have the target name, so now let's search through the scene looking for the reference
                if (ce.FindParameter("target").objData == null)
                {
                    // there aren't any pawns in the scene with this name, let's do a character search instead.
                    ICharacter targetChr = FindCharacter(reader["target"], ce.Name);
                    ce.FindParameter("target").SetObjData(targetChr);
                }

                if (ce.FindParameter("target").objData == null)
                {
                    Debug.LogWarning(string.Format("{0} event {1} has a target named {2} but that target was not found in this scene", "Gaze", ce.Name, reader["target"]));
                }
            }
        }

        public override string GetLengthParameterName() { return "motion"; }
        public override bool NeedsToBeFired (CutsceneEvent ce) { return false; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
            if ((ce.FunctionOverloadIndex == 0) && !IsParamNull(ce, 1))
            {
                length = Cast<SmartbodyMotion>(ce, 1).MotionLength;
            }
            return length;
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "perlinFrequency")
            {
                param.floatData = 0.05f;
            }
            else if (param.Name == "perlinScale")
            {
                param.floatData = 0.03f;
            }
            else if (param.Name == "emotion")
            {
                param.SetEnumData(GestureUtils.Emotion.neutral);
            }
        }
        #endregion
    }

    #endregion
}
