/*
Links the local position of game object with this component to specified 
game object. Used in cases where you need an one object to match position of
another game object but you dont want to make it a child. This updates every
frame so should be used sparingly.
*/

using UnityEngine;
using System.Collections;

public class AGLinkTransform : MonoBehaviour {
    //Variables
    public bool m_driverFindByName;
    public string m_driverName;
    public Transform Driver ;
    Transform currentTransform ;
    
    void Start()
    {
        //Get the game object this script is attached to.
        currentTransform = gameObject.transform ;
        
        //Check that a Driver is actually assigned.
        if (Driver == null && m_driverFindByName == true)
        {
            Driver = GameObject.Find(m_driverName).GetComponent<Transform>();
        }

        if (Driver == null)
        {
            Debug.LogWarning("AGLinkTransform component has not been assigned/found a 'Driver' on game object: "+currentTransform) ;
        }
    }

    void Update()
    {
        if (Driver != null)
        {
            //Set the game objects world position to be the same as 'the driver'
            currentTransform.position = Driver.transform.position;
            currentTransform.rotation = Driver.transform.rotation;
            currentTransform.localScale = Driver.transform.localScale;
        }
    }
}
