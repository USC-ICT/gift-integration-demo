using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections;

public abstract class SmartbodyJointMap : MonoBehaviour
{
    [NonSerialized] public string mapName;
    [NonSerialized] public List<KeyValuePair<string, string>> mappings = new List<KeyValuePair<string,string>>();  // 'JtSpineA', 'spine1'  or  newJoint, origSBJoint


    void Start()
    {
    }
}
