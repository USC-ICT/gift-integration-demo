using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CameraManager : MonoBehaviour
{
    #region Variables
    public List<Camera> m_Cameras = new List<Camera>();
    #endregion

    #region Functions
    public void ActivateCamera(Camera _camera)
    {
        // shutdown all the others
        m_Cameras.ForEach(c => c.gameObject.SetActive(false));

        _camera.gameObject.SetActive(true);

        int index = m_Cameras.FindIndex(c => c == _camera);
        if (index == -1)
        {
            Debug.LogWarning(string.Format("Camera {0} wasn't managed by the camera manager but is being managed now", _camera.name));
            m_Cameras.Add(_camera);
        }
    }
    #endregion
}
