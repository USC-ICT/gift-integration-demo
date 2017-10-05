using UnityEngine;
using System.Collections;
using System;

public class SmartbodyPawn : ICharacter
{
    #region Variables
    public string m_PawnName;
    public float m_PositionScale = 1.0f;  // HACK: in case the data from the skeleton file and unity don't match scale, we use this.

    Vector3 m_PreviousPosition;
    Vector3 m_PreviousRotation;
    Vector3 m_PreviousScale;

    string m_ColliderType = string.Empty;
    Collider m_Collider;
    #endregion

    #region Properties
    public string PawnName
    {
        get { return m_PawnName; }
    }

    public string ColliderType
    {
        get { return m_ColliderType; }
    }

    float InversePositionScale
    {
        get { return 1 / m_PositionScale; }
    }

    public override string CharacterName
    {
        get { return PawnName; }
    }

    public override AudioSource Voice
    {
        get { return GetComponent<AudioSource>(); }
    }
    #endregion

    #region Functions
    void Start()
    {
        // SmartbodyManager is a dependency of this component.  Make sure Start() has been called.
        SmartbodyManager sbm = SmartbodyManager.Get();
        if (sbm != null)
        {
            sbm.Start();
        }


        if (string.IsNullOrEmpty(m_PawnName))
        {
            m_PawnName = gameObject.name;
        }

        m_Collider = GetComponent<Collider>();
        if (m_Collider != null)
        {
            if (m_Collider is SphereCollider)
            {
                m_ColliderType = "sphere";
            }
            else if (m_Collider is BoxCollider)
            {
                m_ColliderType = "box";
            }
            else if (m_Collider is CapsuleCollider)
            {
                m_ColliderType = "capsule";
            }
            else if (m_Collider is CharacterController)
            {
                m_ColliderType = "character";
            }
            else
            {
                Debug.LogError("SmartbodyPawn " + PawnName + " doesn't have a known collision type");
            }
        }

        m_PreviousScale = transform.localScale;
        m_PreviousRotation = transform.rotation.eulerAngles;

        Init(m_PawnName, transform.position, m_PositionScale);

        AddToSmartbody();
    }

    void Init(string name, Vector3 position, float positionScale)
    {
        m_PawnName = name.Replace(" ", "");
        transform.position = position;
        m_PreviousPosition = position;
        m_PositionScale = positionScale;
    }

    public void AddToSmartbody()
    {
        SmartbodyManager sbm = SmartbodyManager.Get();

        if (sbm != null)
        {
            sbm.PythonCommand(string.Format(@"scene.createPawn('{0}')", m_PawnName));
            SendPawnTransformation(transform.position, transform.rotation.eulerAngles);
            SendPawnGeometry();

            sbm.AddPawn(this);
        }
    }

    void Update()
    {
        Transform transform = this.transform;

        if (m_PreviousPosition != transform.position
            || m_PreviousRotation != transform.rotation.eulerAngles)
        {
            m_PreviousPosition = transform.position;
            m_PreviousRotation = transform.rotation.eulerAngles;

            // send a message saying that the pawn moved or rotated
            SendPawnTransformation(m_PreviousPosition, m_PreviousRotation);
        }

        if (m_PreviousScale != transform.localScale)
        {
            m_PreviousScale = transform.localScale;
            SendPawnGeometry();
        }
    }

    void OnDestroy()
    {
        SmartbodyManager sbm = SmartbodyManager.Get();
        if (sbm != null)
        {
            sbm.PythonCommand(string.Format(@"scene.removePawn('{0}')", m_PawnName));
            sbm.RemovePawn(this);
        }
    }

    void SendPawnTransformation(Vector3 pos, Vector3 rot)
    {
        // send it back to sbm in the correct scale
        pos *= InversePositionScale;

        SmartbodyManager sbm = SmartbodyManager.Get();

        if (sbm != null)
        {
            //string message = string.Format(@"scene.command('set pawn {0} world_offset h {1} p {2} r {3} x {4} y {5} z {6}')", m_PawnName, -rot.y, rot.x, -rot.z, -pos.x, pos.y, pos.z);
            sbm.PythonCommand(string.Format(@"scene.getPawn('{0}').setPosition(SrVec({1},{2},{3}))", m_PawnName, -pos.x, pos.y, pos.z));
            sbm.PythonCommand(string.Format(@"scene.getPawn('{0}').setHPR(SrVec({1},{2},{3}))", m_PawnName, rot.y, rot.x, -rot.z));
        }
    }

    void SendPawnGeometry()
    {
        if (!string.IsNullOrEmpty(m_ColliderType))
        {
            // pawn.setStringAttribute('collisionShape', '<sphere | box | capsule>')
            // pawn.setVec3Attribute('collisionShapeScale', <size>, <size>, <size>

            SmartbodyManager sbm = SmartbodyManager.Get();

            if (sbm != null)
            {
                string message;
                message = string.Format(@"scene.getPawn('{0}').setStringAttribute('collisionShape', '{1}')", m_PawnName, m_ColliderType);
                sbm.PythonCommand(message);

                message = string.Format(@"scene.getPawn('{0}').setVec3Attribute('collisionShapeScale', {1}, {1}, {1})", m_PawnName, GetBoundsSize() * InversePositionScale);
                sbm.PythonCommand(message);
            }
        }
    }

    float GetBoundsSize()
    {
        Transform transform = this.transform;

        float largestAxis = Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        float size = 1.0f;
        if (m_Collider is SphereCollider)
        {
            size = largestAxis * ((SphereCollider)m_Collider).radius;
        }
        else if (m_Collider is BoxCollider)
        {
            BoxCollider box = (BoxCollider)m_Collider;
            size = largestAxis * Mathf.Max(box.size.x, box.size.y, box.size.z);
            size *= 0.5f;
        }
        else if (m_Collider is CapsuleCollider)
        {
            size = largestAxis * ((CapsuleCollider)m_Collider).height;
            size *= 0.5f;
        }
        else if (m_Collider is CharacterController)
        {
            size = largestAxis * ((CharacterController)m_Collider).height;
            size *= 0.5f;
        }
        else
        {
            Debug.LogError("SmartbodyPawn " + PawnName + " doesn't have a known mesh collision type");
        }

        return size;
    }

    public override void PlayAudio(AudioSpeechFile audioId)
    {

    }

    public override void PlayXml(string xml)
    {

    }

    public override void PlayXml(AudioSpeechFile xml)
    {

    }

    public override void Transform(Transform trans)
    {

    }

    public override void Transform(Vector3 pos, Quaternion rot)
    {

    }

    public override void Transform(float y, float p)
    {

    }

    public override void Transform(float x, float y, float z)
    {

    }

    public override void Transform(float x, float y, float z, float h, float p, float r)
    {

    }

    public override void Rotate(float h)
    {

    }

    public override void PlayPosture(string posture, float startTime)
    {

    }

    public override void PlayAnim(string animName)
    {

    }

    public override void PlayAnim(string animName, float readyTime, float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime)
    {

    }

    public override void PlayViseme(string viseme, float weight)
    {

    }

    public override void PlayViseme(string viseme, float weight, float totalTime, float blendTime)
    {

    }

    public override void Nod(float amount, float repeats, float time)
    {

    }

    public override void Shake(float amount, float repeats, float time)
    {

    }

    public override void Gaze(string gazeAt)
    {

    }

    public override void Gaze(string gazeAt, float headSpeed)
    {

    }

    public override void Gaze(string gazeAt, float headSpeed, float eyeSpeed, CharacterDefines.GazeJointRange jointRange)
    {

    }

    public override void Gaze(string gazeAt, string targetBone, CharacterDefines.GazeDirection gazeDirection, CharacterDefines.GazeJointRange jointRange, float angle, float headSpeed, float eyeSpeed, float fadeOut, string gazeHandleName, float duration)
    {

    }

    public override void StopGaze()
    {

    }

    public override void StopGaze(float fadoutTime)
    {

    }

    public override void Saccade(CharacterDefines.SaccadeType type, bool finish, float duration)
    {

    }

    public override void Saccade(CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude)
    {

    }

    public override void StopSaccade()
    {

    }
    #endregion
}
