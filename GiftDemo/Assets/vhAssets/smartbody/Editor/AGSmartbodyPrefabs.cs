/*--------------------------------------------------------------------------------------------------
 * This script creates a prefab from selected asset(s) from the Project view.
 * It automates the steps outlined in Confluence about 'Nesting a FBX in a Prefab':
 *      1. Create a new empty gameobject in your Hierarchy (in the same directory as your FBX).
 *      2. Name it "HouseGameObject"
 *      3. Drag your imported model ("HouseFBX") from the Project panel onto the empty gameobject in the Hierarchy panel.
 *      4. Restore any material links that are broken.
 *      5. Create a new prefab in your Project panel (name this "HousePrefab").
 *      6. Drag the game object ("HouseGameObject") from the Hierarchy panel onto this new prefab ("HousePrefab") in your Project panel.
 *      7. Delete the game object in the Hiearchy.
 *      8. Drag the Prefab from the Project into the scene.
 *
 * End result: Prefab > GameObject > FBX
 *
 * 2012-May-09 - Now Smartbody ready!
 *             - A separate Sbm prefab button is available, which only adds Sbm components to the
 *               prefab if 'CharacterRoot' is found in the FBX.
 *
 * Joe Yip
 * yip@ict.usc.edu
 * 2011-Apr-04
--------------------------------------------------------------------------------------------------*/

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class AGSmartbodyPrefabs : Editor{

    public static void Create(Object asset){
        //Bring asset into Hierarchy
        string FBXPath = AssetDatabase.GetAssetPath(asset);
        if (FBXPath.IndexOf(".fbx") > 0){
            Debug.Log("Creating prefab: " + asset.name);
            #if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
            GameObject obj = PrefabUtility.InstantiatePrefab(Resources.LoadAssetAtPath(FBXPath, typeof(Object))) as GameObject;
            #else
            GameObject obj = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath(FBXPath, typeof(Object))) as GameObject;
            #endif

            string assetPrefabName = obj.name + "Prefab.prefab";
            string assetGameObjectName = obj.name + "GameObject";

            //Reset transforms
            obj.transform.localPosition = new Vector3(0, 0, 0);
            obj.transform.localRotation = Quaternion.identity;
            obj.transform.localScale = new Vector3(1, 1, 1);

            //Create Prefab and GameObject
            FBXPath = FBXPath.Replace((obj.name + ".fbx"), "");
            Object assetPrefab = PrefabUtility.CreateEmptyPrefab(FBXPath + assetPrefabName);
            GameObject assetGameObject = new GameObject(assetGameObjectName);

            //Parent FBX to GameObject
            obj.transform.parent = assetGameObject.transform;

            //Check if this is a character, if so, add necessary Smartbody components
            if (obj.transform.Find("CharacterRoot") != null){
                SmartbodySetup(assetGameObject);
            }

            //Apply GameObject to Prefab Object (in the project)
            PrefabUtility.ReplacePrefab(assetGameObject, assetPrefab);

            //Cleanup
            Debug.Log(asset.name + "Prefab created.");
            Transform.DestroyImmediate(assetGameObject);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }

    public static void SmartbodySetup(GameObject assetGameObject){
        Debug.Log("    Setting up as Smartbody character.");

        //Add "UnitySmartbodyCharacter" script and set variables
        Debug.Log("        Added 'UnitySmartbodyCharacter' component.");
        assetGameObject.AddComponent<UnitySmartbodyCharacter>();
        //UnitySmartbodyCharacter SbmScript = assetGameObject.GetComponent<UnitySmartbodyCharacter>();
        //SbmScript.m_BoneParentName = assetGameObject.name.Replace("GameObject", "") + "/CharacterRoot";

        //Create SoundNode
        string soundNodeName = "SoundNode";
        GameObject soundNode = new GameObject(soundNodeName);
        Debug.Log("        Created 'SoundNode'.");
        soundNode.AddComponent<AudioSource>();
        soundNode.transform.localPosition = new Vector3(0, 0, 0);
        soundNode.transform.localRotation = new Quaternion(0, 0, 0, 0);
        soundNode.transform.localScale = new Vector3(1, 1, 1);

        //Parent SoundNode and move it to where the mouth is (Zebra1 and Zebra2)
        soundNode.transform.parent = assetGameObject.transform;
        foreach (Transform child in assetGameObject.GetComponentsInChildren<Transform>()){
            if (child.name == "JtTongueC" || child.name == "Tongue_front"){
                soundNode.transform.localPosition = child.transform.position;
                Debug.Log("            Positioned 'SoundNode' at the character's mouth.");
                break;
            }
        }
    }


}

class AGSmartbodyPrefab{
    const string menuCreateSbm = "VH/Prefabs/Create Smartbody Prefab(s) From Selected";

    [MenuItem(menuCreateSbm)]
    static void CreateSbmPrefabMenu(){
        //Creates a prefab per asset selected
        GameObject[] list = (GameObject[])Selection.gameObjects;
        foreach (GameObject asset in list){
            AGPrefabs.Create(asset);
        }
    }

    [MenuItem(menuCreateSbm, true)]
    static bool ValidateCreateSbmPrefabMenu(){
        return Selection.activeGameObject != null;
    }
}
