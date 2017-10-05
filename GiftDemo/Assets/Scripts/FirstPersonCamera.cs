using UnityEngine;
using System.Collections;

public class FirstPersonCamera : FreeMouseLook
{
    #region Variables
    protected Vector3 m_ForwardDir = new Vector3();
    protected Vector3 m_RightDir = new Vector3();
    #endregion

    #region Functions

    public override void Update()
    {
        if (CameraRotationOn)
        {
            rotationY += Input.GetAxis("Mouse Y") * -sensitivityY;
            rotationX += Input.GetAxis("Mouse X") * sensitivityX;

            rotationY = ClampAngle(rotationY, minimumY, maximumY);
            rotationX = ClampAngle(rotationX, minimumX, maximumX);

            Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.right);
            Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);

            transform.localRotation = xQuaternion * yQuaternion;
        }

        m_ForwardDir = transform.forward;
        m_ForwardDir.y = 0;
        m_ForwardDir.Normalize();

        m_RightDir = transform.right;
        m_RightDir.y = 0;
        m_RightDir.Normalize();

        CheckKeyPress(m_MoveForwardKeys, MoveForward);
        CheckKeyPress(m_MoveBackwardKeys, MoveBackward);
        CheckKeyPress(m_MoveLeftKeys, MoveLeft);
        CheckKeyPress(m_MoveRightKeys, MoveRight);
        CheckKeyDown(m_ToggleMouseLookKeys, ToggleMouseLook);
    }

    public override void MoveForward()
    {
        transform.localPosition += m_ForwardDir * GetMovementSpeed() * Time.deltaTime;
    }

    public override void MoveBackward()
    {
        transform.localPosition -= m_ForwardDir * GetMovementSpeed() * Time.deltaTime;
    }

    public override void MoveLeft()
    {
        transform.localPosition -= m_RightDir * GetMovementSpeed() * Time.deltaTime;
    }

    public override void MoveRight()
    {
        transform.localPosition += m_RightDir * GetMovementSpeed() * Time.deltaTime;
    }
    #endregion
}
