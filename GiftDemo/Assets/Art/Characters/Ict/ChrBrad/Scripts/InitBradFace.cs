using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class InitBradFace : SmartbodyFaceDefinition
{
    void Awake()
    {
        definitionName = "ChrBradFace";
        neutral = "ChrBrad@face_neutral";

        actionUnits.Add(new SmartbodyFacialExpressionDefinition(1,  "left",    "ChrBrad@001_inner_brow_raiser_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(1,  "right",   "ChrBrad@001_inner_brow_raiser_rt"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(2,  "left",    "ChrBrad@002_outer_brow_raiser_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(2,  "right",   "ChrBrad@002_outer_brow_raiser_rt"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(4,  "left",    "ChrBrad@004_brow_lowerer_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(4,  "right",   "ChrBrad@004_brow_lowerer_rt"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(5,  "both",    "ChrBrad@005_upper_lid_raiser"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(6,  "both",    "ChrBrad@006_cheek_raiser"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(7,  "both",    "ChrBrad@007_lid_tightener"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(10, "both",    "ChrBrad@010_upper_lip_raiser"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(12, "left",    "ChrBrad@012_lip_corner_puller_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(12, "right",   "ChrBrad@012_lip_corner_puller_rt"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(25, "both",    "ChrBrad@025_lips_part"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(26, "both",    "ChrBrad@026_jaw_drop"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(45, "left",    "ChrBrad@045_blink_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(45, "right",   "ChrBrad@045_blink_rt"));


        //actionUnits.Add(new SmartbodyFacialExpressionDefinition(101, "both",   "ChrBrad@101_upset"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(112, "both",   "ChrBrad@112_happy"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(124, "both",   "ChrBrad@124_disgust"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(129, "both",   "ChrBrad@129_angry"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(129, "both",   "ChrBrad@126_fear"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(129, "both",   "ChrBrad@127_surprise"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(130, "both",   "ChrBrad@130_sad"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(136, "both",   "ChrBrad@131_contempt"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(132, "both",   "ChrBrad@132_browraise1"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(133, "both",   "ChrBrad@133_browraise2"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(134, "both",   "ChrBrad@134_hurt_brows"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(136, "both",   "ChrBrad@136_furrow"));

        visemes.Add(new KeyValuePair<string,string>("open",    "ChrBrad@open"));
        visemes.Add(new KeyValuePair<string,string>("W",       "ChrBrad@W"));
        visemes.Add(new KeyValuePair<string,string>("ShCh",    "ChrBrad@ShCh"));
        visemes.Add(new KeyValuePair<string,string>("PBM",     "ChrBrad@PBM"));
        visemes.Add(new KeyValuePair<string,string>("FV",      "ChrBrad@FV"));
        visemes.Add(new KeyValuePair<string,string>("wide",    "ChrBrad@wide"));
        visemes.Add(new KeyValuePair<string,string>("tBack",   "ChrBrad@tBack"));
        visemes.Add(new KeyValuePair<string,string>("tRoof",   "ChrBrad@tRoof"));
        visemes.Add(new KeyValuePair<string,string>("tTeeth",  "ChrBrad@tTeeth"));
    }


    void Start()
    {
    }
}

