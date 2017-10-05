using UnityEngine;
using UnityEditor;
using System.Collections;

class SmartbodyImportSettings : AssetPostprocessor
{
    public override int GetPostprocessOrder() { return -10; }

    void OnPreprocessModel()
    {
        ModelImporter modelImporter = (ModelImporter)assetImporter;

        if (assetPath.Contains("@") && modelImporter.animationType != ModelImporterAnimationType.Human)
             modelImporter.animationType = ModelImporterAnimationType.Legacy;
    }
}
