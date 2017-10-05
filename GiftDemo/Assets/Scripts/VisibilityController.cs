using UnityEngine;
using System.Collections;

public class VisibilityController : MonoBehaviour
{
    // Mesh variables
    //-------------------------------------------------------------------------
    private Renderer[] meshRenderers;
    public bool _debugIsVisible = true;

    //
    // Unity functions
    // Note: This class is event driven so there is no need for the Update() function
    //------------------------------------------------------------------------- 
    void Awake()
    {
        meshRenderers = (Renderer[])gameObject.GetComponentsInChildren<Renderer>(true); // Get body parts, some which can get injured
    }

    public void SetVisible(bool visibilityFlag)
    {
        // avoid applying visibility change if not required
        if (_debugIsVisible != visibilityFlag)
        {
            // make gameobject visible
            if (visibilityFlag == true)
            {
                foreach (Renderer meshRenderer in meshRenderers)
                {
                    meshRenderer.enabled = true;
                }
            }
            // make gameobject invisible
            else
            {
                foreach (Renderer meshRenderer in meshRenderers)
                {
                    meshRenderer.enabled = false;
                }
            }
            _debugIsVisible = visibilityFlag;
        }
        else
        {
            //if (_debugIsVisible == true) Debug.LogWarning("Currently gameobject is (already) visible. Not redoing the same.");
            //else Debug.LogWarning("Currently gameobject is (already) invisible. Not redoing the same.");
        }
    }
}
