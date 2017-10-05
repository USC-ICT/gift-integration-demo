/*--------------------------------------------------------------------------------------------------
 * This script creates a prefab from selected asset(s) from the Project view.
 * It automates the steps outlined in Confluence: 'https://confluence.ict.usc.edu/display/AG/FBX+Prefab'
 *      1. Create Prefab.
 *          - In Project view:
 *              - Create Prefab.
 *              - Rename prefab.
 *              - Nest FBX directly under prefab.
 *      2. Create SoundNode.
 *          - Create empty GameObject named 'SoundNode'.
 *          - Add AudioSource component to SoundNode.
 *      3. Attach Smartbody Component to Prefab.
 *          - Add script component "UnitySmartbodyCharacter" component.
 *          - Turn on 'IsFaceBoneDriven'.
 *      4. Attach and move SoundNode to Prefab.
 *          - Move SoundNode to where the mouth is.
 *          - Parent SoundNode under the prefab.
 *
 * Joe Yip
 * yip@ict.usc.edu
 * 2011-Nov-14
--------------------------------------------------------------------------------------------------*/

using UnityEditor;
using UnityEngine;

public class AGGSPrefabs : Editor{

    public static void Create(Object asset){
        //Bring asset into Hierarchy
        string FBXPath = AssetDatabase.GetAssetPath(asset);
        if (FBXPath.IndexOf(".fbx") > 0)
        {
#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
            GameObject obj = PrefabUtility.InstantiatePrefab(Resources.LoadAssetAtPath(FBXPath, typeof(Object))) as GameObject;
#else
            GameObject obj = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath(FBXPath, typeof(Object))) as GameObject;
#endif
            string assetPrefabName = obj.name + "Prefab.prefab";
            string soundNodeName = "SoundNode";

            //Reset transforms
            obj.transform.localPosition = new Vector3(0, 0, 0);
            obj.transform.localRotation = new Quaternion(0, 0, 0, 0);
            obj.transform.localScale = new Vector3(1, 1, 1);

            //Create Prefab and GameObject
            FBXPath = FBXPath.Replace((obj.name + ".fbx"), "");
            Object assetPrefab = PrefabUtility.CreateEmptyPrefab(FBXPath + assetPrefabName);

            //Parent FBX directly to Prefab
            PrefabUtility.ReplacePrefab(obj, assetPrefab);

            //Create SoundNode
            GameObject soundNode = new GameObject(soundNodeName);
            soundNode.AddComponent<AudioSource>();
            soundNode.transform.localPosition = new Vector3(0, 0, 0);
            soundNode.transform.localRotation = new Quaternion(0, 0, 0, 0);
            soundNode.transform.localScale = new Vector3(1, 1, 1);

            //Bring prefab into scene to add components
            //Add "UnitySmartbodyCharacter" script and set variables
#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
            GameObject prefabObj = PrefabUtility.InstantiatePrefab(Resources.LoadAssetAtPath((FBXPath + assetPrefabName), typeof(Object))) as GameObject;
#else
            GameObject prefabObj = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath((FBXPath + assetPrefabName), typeof(Object))) as GameObject;
#endif
            prefabObj.AddComponent<UnitySmartbodyCharacter>();
            //Parent SoundNode and move it to where the mouth is (Zebra1 and Zebra2)
            soundNode.transform.parent = prefabObj.transform;
            foreach (Transform child in prefabObj.GetComponentsInChildren<Transform>()){
                if (child.name == "JtTongueC" || child.name == "Tongue_front"){
                    soundNode.transform.localPosition = child.transform.position;
                    break;
                }
            }

            //Save prefab
#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
            PrefabUtility.ReplacePrefab(prefabObj, Resources.LoadAssetAtPath((FBXPath + assetPrefabName), typeof(Object)));
#else
            PrefabUtility.ReplacePrefab(prefabObj, AssetDatabase.LoadAssetAtPath((FBXPath + assetPrefabName), typeof(Object)));
#endif

            //Cleanup
            Debug.Log(asset.name + "Prefab created.");
            Transform.DestroyImmediate(obj);
            Transform.DestroyImmediate(prefabObj);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }


}

class AGGSPrefab{
    const string menuCreate = "VH/Prefabs/Create 'GS' style Prefab(s) From Selected";

    //Adds a menu named "Create Prefab(s) From Selected" to the GameObject menu.
    [MenuItem(menuCreate)]
    static void CreatePrefabMenu(){
        //Creates a prefab per asset selected
        GameObject[] list = (GameObject[])Selection.gameObjects;
        foreach (GameObject asset in list){
            AGGSPrefabs.Create(asset);
        }
    }

    //Validates the menu; the item will be disabled if no game object is selected.
    //Returns True if the menu item is valid.
    [MenuItem(menuCreate, true)]
    static bool ValidateCreatePrefabMenu(){
        return Selection.activeGameObject != null;
    }
}
