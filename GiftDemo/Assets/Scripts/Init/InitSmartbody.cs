using UnityEngine;
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections;

public class InitSmartbody : SmartbodyInit
{
    void Awake()
    {
        this.mediaPath = VHFile.GetExternalAssetsPath() + "Sounds";  // PlayXML path

        PostLoadEvent += delegate {
            SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCollisionManager().setStringAttribute('collisionResolutionType', 'default')"));
            SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCollisionManager().setBoolAttribute('singleChrCapsuleMode', True)"));
            SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCollisionManager().setBoolAttribute('enable', True)"));
        };


    }

    void Start()
    {
    }
}
