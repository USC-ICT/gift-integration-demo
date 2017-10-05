using UnityEngine;
using System.Collections;

public class AGPrpScreen01 : MonoBehaviour
{
    void Start()
    {
        string[] affectedObjects = new string[]{"screen"};
        AGAffectFbx.SetLayer(unityLayer:"Walls", affectObjects:affectedObjects, recursive:false, root:this.gameObject);
    }

}
