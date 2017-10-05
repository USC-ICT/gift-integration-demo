using UnityEngine;
using System.Collections;

public class SBCharacterController : MonoBehaviour
{
    #region Variables
    public string m_CharacterName = "";
    public float m_SbmScale = 1.0f;
    public string m_PrevState = "";
    public string m_LocomotionStateName = "allLocomotion";
    public bool m_ShowVelocityNumbers = false;
    //float m_LastTriggerVal = 0;

    public float m_LinearAccel = 2.0f;
    public float m_LinearDeccel = 1.0f;

    public float m_AngularAccel = 150.0f;
    public float m_AngularDeccel = 100.0f;

    public float m_StrafeAccel = 1.0f;
    public float m_CurLinearSpeedLimit = 4.0f;
    public float m_CurAngularSpeedLimit = 140.0f;

    float v;
    float w;
    float s;
    float vPrev;
    float wPrev;
    float sPrev;
    bool starting = false;
    bool stopping = false;
    bool inTransition = false;

    // HACK - for TAB
    bool startedInTransition = false;
    bool startedOutTransition = false;
    float startTransitionTime = 0;

    #endregion

    #region Functions
    void Start() 
    {
        if (string.IsNullOrEmpty(m_CharacterName))
        {
            m_CharacterName = gameObject.name;
        }
    }

    void Update()
    {
        DoMovement();
    }

    void ResetMovement()
    {
        v = w = s = 0;
        starting = stopping = false;
    }

    void DoMovement()
    {
        float verticalAxis = Input.GetAxis("Vertical");
        float horizontalAxis = Input.GetAxis("Horizontal");

        string currentStateName = SmartbodyManager.Get().SBGetCurrentStateName(m_CharacterName);

        m_PrevState = currentStateName;

        float unitScale = 1.0f / m_SbmScale;// / m_sbm.m_PositionScale;
        float scale = 1.0f;

        float linearAcc = m_LinearAccel * scale * unitScale;
        float angularAcc = m_AngularAccel * scale;
        //float strifeAcc = m_StrafeAccel * scale * unitScale;

        // automatic de-acceleration when the key is not pressed
        float linearDcc = m_LinearDeccel;
        float angularDcc = m_AngularDeccel;
        //float straifeDc = strifeAcc*2.5f;

        // speed limits
        float runSpeedLimit = 1.0f*unitScale;
        //float walkSpeedLimit = 1.2f*unitScale;
        float angSpeedLimit = 140.0f;
        float strifeSpeedLimit = 1.0f*unitScale;

        // do not perform any speed update during start
        if (currentStateName == m_LocomotionStateName)
            inTransition = false;
        if (!inTransition)
        {
            if (verticalAxis > 0)
            {
                v += verticalAxis * linearAcc * Time.deltaTime;
                runSpeedLimit *= verticalAxis;
            }
            else if (verticalAxis < 0)
            {
                v += verticalAxis * linearAcc * Time.deltaTime;   // verticalAxis is negative
            }
            else // gradually de-accelerate
            {
                if (v > 0)
                {
                    v -= linearDcc * Time.deltaTime;
                }
            }


            /*if (Input.GetKey(KeyCode.End) || Input.GetButton("StrafeRight"))
            {
                s += strifeAcc * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.Delete) || Input.GetButton("StrafeLeft"))
            {
                s -= strifeAcc * Time.deltaTime;
            }
            else // gradually de-accelerate
            {
                if (s > 0)
                {
                    s -= straifeDc * Time.deltaTime;
                    if (s < 0) s = 0;
                }
                else if (s < 0)
                {
                    s += straifeDc * Time.deltaTime;
                    if (s > 0) w = s;
                }
            }*/


            if (horizontalAxis < 0)
            {
                //Debug.Log("horizontalAxis: " + horizontalAxis);
                w += Mathf.Abs(horizontalAxis) * angularAcc * Time.deltaTime;
                angSpeedLimit *= Mathf.Abs(horizontalAxis);
            }
            else if (horizontalAxis > 0)
            {
                //Debug.Log("horizontalAxis: " + horizontalAxis);
                w -= Mathf.Abs(horizontalAxis) * angularAcc * Time.deltaTime;
                angSpeedLimit *= Mathf.Abs(horizontalAxis);
            }
            else // gradually de-accelerate
            {
                if (w > 0)
                {
                    w -= angularDcc * Time.deltaTime;
                    if (w < 0) w = 0;
                }
                else if (w < 0)
                {
                    w += angularDcc * Time.deltaTime;
                    if (w > 0) w = 0;
                }
            }

            // gradually change the angular speed limit to avoid sudden change
            if (m_CurAngularSpeedLimit > angSpeedLimit)
            {
                m_CurAngularSpeedLimit -= angularDcc * Time.deltaTime;
                if (m_CurAngularSpeedLimit < angSpeedLimit) m_CurAngularSpeedLimit = angSpeedLimit;
            }
            else if (m_CurAngularSpeedLimit < angSpeedLimit)
            {
                m_CurAngularSpeedLimit += angularAcc * Time.deltaTime;
                if (m_CurAngularSpeedLimit > angSpeedLimit) m_CurAngularSpeedLimit = angSpeedLimit;
            }

            if (m_CurLinearSpeedLimit > runSpeedLimit)
            {
                m_CurLinearSpeedLimit -= linearDcc * Time.deltaTime;
                if (m_CurLinearSpeedLimit < runSpeedLimit) m_CurLinearSpeedLimit = runSpeedLimit;
            }
            else if (m_CurLinearSpeedLimit < runSpeedLimit)
            {
                m_CurLinearSpeedLimit += linearAcc * Time.deltaTime;
                if (m_CurLinearSpeedLimit > runSpeedLimit) m_CurLinearSpeedLimit = runSpeedLimit;
            }


            // make sure the control parameter does not exceed limits


            if (w > m_CurAngularSpeedLimit) w = m_CurAngularSpeedLimit;
            if (w < -m_CurAngularSpeedLimit) w = -m_CurAngularSpeedLimit;

            if (s > strifeSpeedLimit) s = strifeSpeedLimit;
            if (s < -strifeSpeedLimit) s = -strifeSpeedLimit;

            if (v > m_CurLinearSpeedLimit) v = m_CurLinearSpeedLimit;
            if (v < 0) v = 0; // we don't allow backward walking....yet
        }


        if (m_ShowVelocityNumbers)
        {
            Debug.Log(currentStateName + " - v: " + v + " w: " + w + " s: " + s + " vert: " + verticalAxis + " horiz: " + horizontalAxis);
        }


        // HACK - for TAB
        if (startedInTransition)
        {
            if (Time.time - startTransitionTime > 0.666f)
            {
                if (v != vPrev || w != wPrev || s != sPrev)
                {
                    SmartbodyManager.Get().SBStateChange(m_CharacterName, m_LocomotionStateName, "schedule", "Loop", "Now", v, w, s);
                    vPrev = v;
                    wPrev = w;
                    sPrev = s;
                    starting = true;
                    stopping = false;
                    inTransition = true;

                    startedInTransition = false;
                }
            }
        }

        // HACK - for TAB
        if (startedOutTransition)
        {
            if (Time.time - startTransitionTime > 0.666f)
            {
                v = w = 0;
                SmartbodyManager.Get().SBStateChange(m_CharacterName, "PseudoIdle", "schedule", "Loop", "Now");

                stopping = true;
                starting = false;

                startedOutTransition = false;
            }
        }

        float speedEps = 0.001f * unitScale;
        float angleEps = 0.01f;
        if ( (Mathf.Abs(v) > 0 || Mathf.Abs(w) > 0 || Mathf.Abs(s) > 0)
            && currentStateName == "PseudoIdle" && !starting)
        {
#if false
            if (v != vPrev || w != wPrev || s != sPrev)
            {
                SBHelpers.SBStateChange(m_CharacterName, m_LocomotionStateName, "schedule", "Loop", "Now", v, w, s);
                vPrev = v;
                wPrev = w;
                sPrev = s;
                starting = true;
                stopping = false;
                inTransition = true;
            }
#else
            // HACK - for TAB
            if (!startedInTransition)
            {
                SmartbodyManager.Get().SBPlayAnim(m_CharacterName, "ChrBrad@Idle01_ToLocIdle01");
                startedInTransition = true;
                startTransitionTime = Time.time;
            }
#endif
        }
        else if (Mathf.Abs(v) < speedEps && Mathf.Abs(w) < angleEps && Mathf.Abs(s) < speedEps
            && currentStateName == m_LocomotionStateName && !stopping)
        {
#if false
            v = w = 0;
            SBHelpers.SBStateChange(m_CharacterName, "PseudoIdle", "schedule", "Loop", "Now");

            stopping = true;
            starting = false;
#else
            // HACK - for TAB
            if (!startedOutTransition)
            {
                SmartbodyManager.Get().SBPlayAnim(m_CharacterName, "ChrBrad@LocIdle01_ToIdle01");
                startedOutTransition = true;
                startTransitionTime = Time.time;
            }
#endif
        }
        else if (currentStateName == m_LocomotionStateName)// update moving parameter
        {
            if (v != vPrev || w != wPrev || s != sPrev)
            {
                SmartbodyManager.Get().SBStateChange(m_CharacterName, m_LocomotionStateName, "update", "Loop", "Now", v, w, s);
                vPrev = v;
                wPrev = w;
                sPrev = s;
            }
        }
    }

    /*public override void VHOnDisable()
    {
        ResetMovement();
        SBHelpers.SBWalkImmediate(charName, m_LocomotionStateName, 0, 0, 0);
    }*/
    #endregion
}
