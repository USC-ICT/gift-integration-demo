using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class InitGenericMleAdult : SmartbodyCharacterInit
{
    protected override void Awake()
    {
        base.Awake();
        voiceType = "remote_audiofile";
        voiceCode = VHFile.GetExternalAssetsPath() + "Sounds";
        voiceTypeBackup = "remote";
        voiceCodeBackup = "Festival_voice_cmu_us_jmk_arctic_clunits";
        usePhoneBigram = false;

        PostLoadEvent += delegate(UnitySmartbodyCharacter character)
            {
                SmartbodyManager.Get().PythonCommand(string.Format(@"bml.execBML('{0}', '<gaze target=""Camera"" sbm:joint-range=""NECK EYES""/>')", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"bml.execBML('{0}', '<saccade mode=""talk""/>')", character.SBMCharacterName));
                SmartbodyManager.Get().PythonCommand(string.Format(@"scene.getCharacter('{0}').setStringAttribute('saccadePolicy', 'alwayson')", character.SBMCharacterName));
            };
    }


    protected override void Start()
    {
        base.Start();
    }
}
