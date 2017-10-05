
# PLEASE NOTE:
#  This .py file is not longer in use.  All smartbody initialization is now done within Unity itself.  See the SmartbodyManager gameobject
#
#

print "loading init-unity-campus.py"


scene.run("init-unity-sbm.py")


### Init Brad character with skeleton and model
chr = scene.createCharacter("ChrBrad", "ChrBrad-attach")
chr.setSkeleton(scene.createSkeleton("ChrBrad.sk"))
chr.setFaceDefinition(chrBradFace)
chr.createStandardControllers()

### Use pre-recorded speech, using the \sounds folder.
### Specify TTS as a backup if the prerecorded sound file is not found
chr.setVoice("audiofile")
chr.setVoiceCode("SoundsZebra2")
chr.setVoiceBackup("remote")
chr.setVoiceBackupCode("Festival_voice_rab_diphone")
chr.setUseVisemeCurves(True)

### set an idle posture, gazing at the camera
bml.execBML('ChrBrad', '<body posture="ChrBrad@Idle01"/>')
bml.execBML('ChrBrad', '<gaze target="Camera" sbm:joint-range="NECK EYES"/>')
bml.execBML('ChrBrad', '<saccade mode="talk"/>')


### Init Rachael character with skeleton and model
chr = scene.createCharacter("ChrRachel", "ChrRachel-attach")
chr.setSkeleton(scene.createSkeleton("ChrRachel.sk"))
chr.setFaceDefinition(chrBradFace)
chr.createStandardControllers()

### Use pre-recorded speech, using the \sounds folder.
### Specify TTS as a backup if the prerecorded sound file is not found
chr.setVoice("audiofile")
chr.setVoiceCode("SoundsZebra2")
chr.setVoiceBackup("remote")
chr.setVoiceBackupCode("MicrosoftAnna")
chr.setUseVisemeCurves(True)

### set an idle posture, gazing at the camera
bml.execBML('ChrRachel', '<body posture="ChrBrad@Idle01"/>')
bml.execBML('ChrRachel', '<gaze target="Camera" sbm:joint-range="NECK EYES"/>')
bml.execBML('ChrRachel', '<saccade mode="talk"/>')

scene.run("init-param-animation.py")
