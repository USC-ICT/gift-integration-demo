using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class InitRachelFace : SmartbodyFaceDefinition
{
    void Awake()
    {
        definitionName = "ChrRachelFace";
        neutral = "ChrRachel@face_neutral";

        actionUnits.Add(new SmartbodyFacialExpressionDefinition(1,  "left",    "ChrRachel@001_inner_brow_raiser_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(1,  "right",   "ChrRachel@001_inner_brow_raiser_rt"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(2,  "left",    "ChrRachel@002_outer_brow_raiser_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(2,  "right",   "ChrRachel@002_outer_brow_raiser_rt"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(4,  "left",    "ChrRachel@004_brow_lowerer_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(4,  "right",   "ChrRachel@004_brow_lowerer_rt"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(5,  "both",    "ChrRachel@005_upper_lid_raiser"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(6,  "both",    "ChrRachel@006_cheek_raiser"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(7,  "both",    "ChrRachel@007_lid_tightener"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(10, "both",    "ChrRachel@010_upper_lip_raiser"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(12, "left",    "ChrRachel@012_lip_corner_puller_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(12, "right",   "ChrRachel@012_lip_corner_puller_rt"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(14, "both",    "ChrRachel@014_dimpler"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(15, "both",    "ChrRachel@015_lip_corner_depressor"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(17, "both",    "ChrRachel@017_chin_raiser"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(18, "both",    "ChrRachel@018_lip_pucker"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(20, "both",    "ChrRachel@020_lip_stretcher"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(23, "both",    "ChrRachel@023_lip_tightener"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(24, "both",    "ChrRachel@024_lip_pressor"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(25, "both",    "ChrRachel@025_lips_part"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(26, "both",    "ChrRachel@026_jaw_drop"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(45, "left",    "ChrRachel@045_blink_lf"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(45, "right",   "ChrRachel@045_blink_rt"));

        actionUnits.Add(new SmartbodyFacialExpressionDefinition(99, "both",    "ChrRachel@099_big_smile"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(100, "both",   "ChrRachel@100_small_smile"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(101, "both",   "ChrRachel@101_upset"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(102, "both",   "ChrRachel@102_skeptical"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(103, "both",   "ChrRachel@103_concern"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(124, "both",   "ChrRachel@124_disgust"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(112, "both",   "ChrRachel@112_happy"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(129, "both",   "ChrRachel@129_angry"));
        actionUnits.Add(new SmartbodyFacialExpressionDefinition(130, "both",   "ChrRachel@130_sad"));



        visemes.Add(new KeyValuePair<string,string>("open",    "ChrRachel@open"));
        visemes.Add(new KeyValuePair<string,string>("W",       "ChrRachel@W"));
        visemes.Add(new KeyValuePair<string,string>("ShCh",    "ChrRachel@ShCh"));
        visemes.Add(new KeyValuePair<string,string>("PBM",     "ChrRachel@PBM"));
        visemes.Add(new KeyValuePair<string,string>("FV",      "ChrRachel@FV"));
        visemes.Add(new KeyValuePair<string,string>("wide",    "ChrRachel@wide"));
        visemes.Add(new KeyValuePair<string,string>("tBack",   "ChrRachel@tBack"));
        visemes.Add(new KeyValuePair<string,string>("tRoof",   "ChrRachel@tRoof"));
        visemes.Add(new KeyValuePair<string,string>("tTeeth",  "ChrRachel@tTeeth"));
    }


    void Start()
    {
    }
}
