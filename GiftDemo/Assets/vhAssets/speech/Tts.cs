using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class Tts : MonoBehaviour
{
    #region Constants
    public delegate void OnConvertedTextToSpeech(Output output);

    [System.Serializable]
    public class Config
    {
        public string VoiceId = "";
        public string OutputFormat = "wav";
        public string SampleRate = "22050";
        public string Text = "This is text to speech";
        public string CharName = "";
        //public string TextType = "text";
    }

    public class Output
    {
        public AudioClip Speech;
        public string Text = "";
    }
    #endregion

    #region Variables
    [SerializeField] Config m_Config = new Config();
    #endregion

    #region Properties
    public string VoiceId
    {
        get { return m_Config.VoiceId; }
        set { m_Config.VoiceId = value; }
    }

    public string OutputFormat
    {
        get { return m_Config.OutputFormat; }
        set { m_Config.OutputFormat = value; }
    }

    public string SampleRate
    {
        get { return m_Config.SampleRate; }
        set { m_Config.SampleRate = value; }
    }

    public string CharName
    {
        get { return m_Config.CharName; }
        set { m_Config.CharName = value; }
    }

    /*public string Text
    {
        get { return m_SpeechData.Text; }
        set { m_SpeechData.Text = value; }
    }*/
    #endregion

    #region Functions
    public void ConvertTextToSpeech(string text)
    {
        Convert(text, null);
    }

    public void ConvertTextToSpeech(string text, OnConvertedTextToSpeech cb)
    {
        Convert(text, cb);
    }

    abstract protected void Convert(string text, OnConvertedTextToSpeech cb);

    
    #endregion
}
