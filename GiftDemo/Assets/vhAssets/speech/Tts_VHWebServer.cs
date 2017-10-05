using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tts_VHWebServer : Tts
{
    #region Variables
    [SerializeField]
    VHMsgBase m_vhmsg;
    #endregion

    #region Functions
    protected override void Convert(string text, OnConvertedTextToSpeech cb)
    {
        //throw new NotImplementedException();
        StartCoroutine(SendServerMessage(0, text, VoiceId, " ", CharName, cb));
    }
    

    IEnumerator SendServerMessage(int speechUserId, string text, string voice, string partipantId, string charName, OnConvertedTextToSpeech cb)
    {
        Output output = new Output();

        string url = string.Format("https://vhtoolkitwww.ict.usc.edu/VHMsgAsp/VHMsgSite.aspx?SpeechUserId={0}&UserMessage={1}&ClientNeedsResponse=true&ParticipantId={2}&Voice={3}&NPCProfileUserName={4}",
            speechUserId, text.Replace(" ", "%20"), partipantId, voice, charName);
        WWW www = new WWW(url);
        Debug.Log(url);
        yield return www;

        while (!www.isDone) { yield return new WaitForEndOfFrame(); }

        if (www.error != null)
        {
            Debug.Log(www.error);
        }
        else
        {
            int index = www.text.IndexOf(SpeechRecognizer_WebServer.Splitter);
            if (index != -1)
            {
                string vrSpeakMsg = www.text.Substring(0, index);
                Debug.Log(vrSpeakMsg);
                m_vhmsg.SendVHMsg(vrSpeakMsg);
            }
            else
            {
                Debug.LogError(string.Format("Expected message vrSpeak was not received"));
                Debug.Log(www.text);
            }
        }

        if (cb != null)
        {
            cb(output);
        }
    }
    #endregion
}
