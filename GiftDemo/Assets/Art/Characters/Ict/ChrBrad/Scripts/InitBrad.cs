using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class InitBrad : SmartbodyCharacterInit
{
    protected override void Awake()
    {
        base.Awake();

        unityBoneParent = "ChrBrad/CharacterRoot/JtRoot";
        //assetPaths.Add(new KeyValuePair<string, string>("ChrBrad.sk", "Art/Characters/SB/ChrBrad/sk"));
        //assetPaths.Add(new KeyValuePair<string, string>("ChrBrad.sk", "Art/Characters/SB/ChrBrad/face"));
        //assetPaths.Add(new KeyValuePair<string, string>("ChrBrad.sk", "Art/Characters/SB/ChrBrad/motion"));
        skeletonName = "ChrBrad.sk";
        loadSkeletonFromSk = false;
        voiceType = "remote_audiofile";
        voiceCode = VHFile.GetExternalAssetsPath() + "Sounds";
        voiceTypeBackup = "remote";
        voiceCodeBackup = VHUtils.IsWindows8OrGreater() ? "Microsoft|David|Desktop" : "Microsoft|Anna";
        usePhoneBigram = false;
        startingPosture = "ChrBrad@Idle01";

        locomotionInitPythonSkeletonName = "ChrBrad.sk";
        locomotionInitPythonFile = "locomotion-ChrBrad-init.py";
        locomotionSteerPrefix = "ChrMarine";

        PostLoadEvent += delegate(UnitySmartbodyCharacter character)
            {
                SmartbodyManager.Get().PythonCommand(string.Format(@"bml.execBML('{0}', '<gaze target=""Camera""    sbm:joint-range=""HEAD EYES NECK""  />')", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"bml.execBML('{0}', '<saccade mode=""talk""/>')", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setStringAttribute('saccadePolicy', 'alwayson')", character.SBMCharacterName));

                // limiting gaze
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('limitHeadingNeck', 60 )", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('gaze.limitHeadingBack', 0 )", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('gaze.limitPitchDownBack', 0 )", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('gaze.limitHeadingBack', 0 )", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('gaze.limitRollBack', 0 )", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('gaze.limitRollChest', 0 )", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('gaze.limitHeadingChest', 0 )", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('gaze.limitPitchDownChest', 0 )", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setDoubleAttribute('gaze.limitPitchUpChest', 0 )", character.SBMCharacterName));
            };
    }


    protected override void Start()
    {
        base.Start();
    }
}
