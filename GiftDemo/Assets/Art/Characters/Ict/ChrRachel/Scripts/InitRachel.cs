using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class InitRachel : SmartbodyCharacterInit
{
    protected override void Awake()
    {
        base.Awake();

        unityBoneParent = "ChrRachel/CharacterRoot/JtRoot";
        //assetPaths.Add(new KeyValuePair<string, string>("ChrRachel.sk", "Art/Characters/SB/ChrRachel/sk"));
        //assetPaths.Add(new KeyValuePair<string, string>("ChrRachel.sk", "Art/Characters/SB/ChrRachel/face"));
        //assetPaths.Add(new KeyValuePair<string, string>("ChrRachel.sk", "Art/Characters/SB/ChrRachel/motion"));
        //assetPaths.Add(new KeyValuePair<string, string>("ChrBrad.sk", "Art/Characters/SB/ChrBrad/sk"));
        //assetPaths.Add(new KeyValuePair<string, string>("ChrBrad.sk", "Art/Characters/SB/ChrBrad/face"));
        //assetPaths.Add(new KeyValuePair<string, string>("ChrBrad.sk", "Art/Characters/SB/ChrBrad/motion"));
        skeletonName = "ChrRachel.sk";
        loadSkeletonFromSk = false;
        voiceType = "remote_audiofile";
        voiceCode = VHFile.GetExternalAssetsPath() + "Sounds";
        voiceTypeBackup = "remote";
        voiceCodeBackup = VHUtils.IsWindows8OrGreater() ? "Microsoft|Zira|Desktop" : "Microsoft|Anna";
        usePhoneBigram = false;
        startingPosture = "ChrRachel@IdleHandsAtSide01";

        locomotionInitPythonSkeletonName = "ChrRachel.sk";
        locomotionInitPythonFile = "locomotion-ChrBrad-init.py";
        locomotionSteerPrefix = "ChrMarine";

        PostLoadEvent += delegate(UnitySmartbodyCharacter character)
            {
                SmartbodyManager.Get().PythonCommand(string.Format(@"bml.execBML('{0}', '<gaze target=""Camera""   sbm:joint-range=""HEAD EYES NECK"" />')", character.SBMCharacterName));
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
