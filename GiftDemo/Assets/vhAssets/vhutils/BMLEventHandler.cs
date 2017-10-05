using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System;

public class BMLEventHandler : MonoBehaviour
{
    #region Variables
    public ICharacterController m_CharacterController;
    public Cutscene m_CutscenePrefab;
    protected BMLParser m_BMLParser;
    #endregion

    #region Functions
    public virtual void Start()
    {
        m_BMLParser = new BMLParser(OnParsedBMLTiming, OnParsedVisemeTiming, OnParsedBMLEvent, OnFinishedReading, OnParsedCustomEvent);
        if (m_CharacterController as MecanimManager != null)
        {
            m_BMLParser.EventCategoryName = GenericEventNames.Mecanim;
        }
    }

    public bool LoadXMLString(string character, string xmlStr)
    {
        return m_BMLParser.LoadXMLString(character, xmlStr);
    }

    public bool LoadXMLBMLStrings(string character, string xmlStr, string bmlStr)
    {
        return m_BMLParser.LoadXMLBMLStrings(character, xmlStr, bmlStr);
    }

    public bool LoadXMLBMLStrings(string character, AudioSpeechFile speech)
    {
        return m_BMLParser.LoadXMLBMLStrings(character, speech);
    }

    protected virtual void OnParsedBMLTiming(BMLParser.BMLTiming bmlTiming) { }
    protected virtual void OnParsedVisemeTiming(BMLParser.LipData lipData) { }
    protected virtual void OnParsedBMLEvent(XmlTextReader reader, string eventType, CutsceneEvent ce)
    {
        /*if (eventType == "speech")
        {
            ce.ChangedEventFunction("PlayAudio", 5);
            ce.SetParameters(reader);
        }*/
    }

    protected virtual void OnFinishedReading(bool succeeded, List<CutsceneEvent> createdEvents)
    {
        Cutscene cs = (Cutscene)Instantiate(m_CutscenePrefab);

        foreach (CutsceneEvent ce in createdEvents)
        {
            if (m_BMLParser.IgnoreSpeechEvent && ce.FunctionName == "PlayAudio")
            {
                continue;
            }
            ce.SetMetaData(m_CharacterController);
            cs.AddEvent(ce);
        }

        cs.Play();
        cs.AddOnFinishedCutsceneCallback(OnFinishedCutscene);
    }

    protected virtual void OnFinishedCutscene(Cutscene cs)
    {
        Destroy(cs.gameObject);
    }

    protected virtual void OnParsedCustomEvent(XmlTextReader reader)
    {

    }
    #endregion
}
