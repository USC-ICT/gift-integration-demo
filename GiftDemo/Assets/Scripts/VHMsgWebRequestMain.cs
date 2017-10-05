using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class VHMsgWebRequestMain : VHMain
{
    #region Constants
    enum Logos
    {
        Facebook,
        Twitter,
        GooglePlus,
        Reddit,
    }
    const string Splitter = "___MSG_SPLIT___";
    const int MicRecordingMaxSeconds = 5;
    #endregion

    #region Variables
    public string m_WebServerUrl = "http://vhtoolkitwww/VHMsgAsp/VHMsgSite.aspx";
    public BMLEventHandler_Web m_BMLEventHandler;
    public VHMsgWebRequest vhmsg;
    public SpeechBox_Web m_SpeechBox;
    public Texture[] m_Logos;
    public string m_WebPage = "http://uperf/vhweb/vhweb.html";
    public float m_SocialMediaWidth = 500;
    public float m_SocialMediaHeight = 300;
    //public FaceFXControllerScript m_FaceFx;
    public SpeechRecognizer m_SpeechRecognizer;
    public MicrophoneRecorder m_Recorder;
    string m_ParticipantId = "";
    int m_RecordingDeviceIndex = 0;
    int m_SpeechUserId = 1;
    string m_TtsVoice = "Festival_voice_cmu_us_jmk_arctic_clunits";
    //AudioSource m_Speaker;
    AudioClip m_MicAudio;
    TtsReader m_TtsReader = new TtsReader();
    
    string SessionName = "";

    #endregion

    #region Properties
    public string ParticipantId
    {
        get { return m_ParticipantId; }
    }

    string RecordingDeviceName
    {
        get { return Microphone.devices.Length > 0 ? Microphone.devices[m_RecordingDeviceIndex] : string.Empty; } 
    }
    #endregion

    #region Functions
    public override void Start()
    {
    
        //base.Start();
        Application.runInBackground = true;

        m_ParticipantId     = Guid.NewGuid().ToString();
        SessionName          =  Guid.NewGuid().ToString();
        
        vhmsg.SubscribeMessage("vrSpeak");
        vhmsg.SubscribeMessage("vrExpress");
        vhmsg.SubscribeMessage("vrAgentBML");
        vhmsg.SubscribeMessage("RemoteSpeechReply");
        vhmsg.AddMessageEventHandler(new VHMsgBase.MessageEventHandler(VHMsg_MessageEvent));
        
        m_Console.AddCommandCallback("vhmsg", new DebugConsole.ConsoleCallback(HandleConsoleMessage));
        m_SpeechBox.WebServerUrl = m_WebServerUrl;

        m_SpeechRecognizer.AddSpeechRecognitionFinishedCallback(SpeechRecognitionResultsReceived);
        m_Recorder.AddOnFinishedRecordingCallback(OnMicFinishedRecording);
    }

    void OnMicFinishedRecording(AudioStream stream)
    {
        m_SpeechRecognizer.Recognize(stream.Clip);
    }

    void SpeechRecognitionResultsReceived(SpeechRecognizer recognizer, List<SpeechRecognizer.RecognizerResult> results)
    {
        if (results.Count > 0 && !string.IsNullOrEmpty(results[0].m_Utterance))
        {
            //Debug.Log("results[0].m_Utterance: " + results[0].m_Utterance);
            SendServerMessage(results[0].m_Utterance);
        }
    }

    static byte[] GetBytes(string str)
    {
        byte[] bytes = new byte[str.Length * sizeof(char)];
        System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
        return bytes;
    }

    static string GetString(byte[] bytes)
    {
        char[] chars = new char[bytes.Length / sizeof(char)];
        System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
        return new string(chars);
    }

    IEnumerator UploadAudioClip(string webServerUrl, AudioClip clip)
    {
        WWWForm form = new WWWForm();
        SavWav.AudioData audioData = SavWav.ConvertAudioClipToAudioData(clip);

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < audioData.buffer.Length; i++)
        {
            builder.Append(audioData.buffer[i].ToString("f4"));
            builder.Append('|');
        }
        builder = builder.Remove(builder.Length - 1, 1);

        //form.AddBinaryData("Samples", GetBytes(audioData.samples.ToString()));
        //form.AddBinaryData("Channels", GetBytes(audioData.channels.ToString()));
        //form.AddBinaryData("Frequency", GetBytes(audioData.frequency.ToString()));
        //form.AddBinaryData("Buffer", GetBytes(builder.ToString()));

        form.AddField("Samples", (audioData.samples.ToString()));
        form.AddField("Channels", (audioData.channels.ToString()));
        form.AddField("Frequency", (audioData.frequency.ToString()));
        form.AddField("Buffer", (builder.ToString()));
        string url = string.Format("{0}?ClientNeedsResponse=true&ParticipantId={1}&IsMicInput=true", webServerUrl, ParticipantId);
        WWW www = new WWW(url, form);

        //Debug.Log("url: " + url);

        yield return www;
    }

    public override void OnGUI()
    {
        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button(m_Logos[(int)Logos.Facebook]))
            {
                Application.ExternalEval(string.Format("window.open('https://www.facebook.com/sharer/sharer.php?u={0}','_blank','width={1},height={2}')", m_WebPage, m_SocialMediaWidth, m_SocialMediaHeight));
            }
            if (GUILayout.Button(m_Logos[(int)Logos.Twitter]))
            {
                Application.ExternalEval(string.Format("window.open('https://twitter.com/intent/tweet?original_referer={0}&text={1}&url={0}','_blank','width={2},height={3}')", m_WebPage, "Virtual Humans Web App", m_SocialMediaWidth, m_SocialMediaHeight));
            }
            if (GUILayout.Button(m_Logos[(int)Logos.GooglePlus]))
            {
                Application.ExternalEval(string.Format("window.open('https://plus.google.com/share?url={0}','_blank','width={1},height={2}')", m_WebPage, m_SocialMediaWidth, m_SocialMediaHeight));
            }
            if (GUILayout.Button(m_Logos[(int)Logos.Reddit]))
            {
                Application.ExternalEval(string.Format("window.open('https://www.reddit.com/submit?url={0}','_blank','width={1},height={2}')", m_WebPage, m_SocialMediaWidth, m_SocialMediaHeight));
            }
        }
        GUILayout.EndHorizontal();
        if (GUILayout.Button("Virtual Humans Website"))
        {
            Application.ExternalEval("window.open ('https://vhtoolkit.ict.usc.edu')");
        }
        
        GUILayout.Space(10);
        
        SessionName = GUILayout.TextArea(SessionName, 200);

        GUILayout.Space(10);

        // Graphics quality
        GUILayout.BeginHorizontal();
        {
            if (GUILayout.Button("<-"))
            {
                QualitySettings.SetQualityLevel(WrapInt(QualitySettings.GetQualityLevel() - 1, 0, QualitySettings.names.Length - 1));
            }
            GUILayout.Button(string.Format("Quality Level: {0}", QualitySettings.names[QualitySettings.GetQualityLevel()]));
            if (GUILayout.Button("->"))
            {
                QualitySettings.SetQualityLevel(WrapInt(QualitySettings.GetQualityLevel() + 1, 0, QualitySettings.names.Length - 1));
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("TTS Test"))
        {
            SendServerTts("the university of southern california or u s c is a leading research university. check us out at u s c dot e d u");
            //vhmsg.SendVHMsg(@"RemoteSpeechCmd speak Brad 1 Festival_voice_cmu_us_jmk_arctic_clunits ../../data/cache/audio/utt_20140206_144906_Brad_1.aiff <?xml version=""1.0"" encoding=""UTF-8""?><speech id=""sp1"" ref=""speech_womanTTS"" type=""application/ssml+xml"">the university of southern california or u s c is a leading research university. check us out at u s c dot e d u</speech>");
        }

        GUILayout.Space(10);

        // Recording Device
        if (Microphone.devices.Length > 0 && Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("<-"))
                {
                    m_RecordingDeviceIndex = WrapInt(m_RecordingDeviceIndex - 1, 0, Microphone.devices.Length - 1);
                    m_Recorder.SetRecordingDevice(Microphone.devices[m_RecordingDeviceIndex]);
                }
                GUILayout.Button(string.Format("Recording Device: {0}", Microphone.devices[m_RecordingDeviceIndex]));
                if (GUILayout.Button("->"))
                {
                    m_RecordingDeviceIndex = WrapInt(m_RecordingDeviceIndex + 1, 0, Microphone.devices.Length - 1);
                    m_Recorder.SetRecordingDevice(Microphone.devices[m_RecordingDeviceIndex]);
                }
            }
            GUILayout.EndHorizontal();

            m_Recorder.CheckRecordingInput = GUILayout.Toggle(m_Recorder.CheckRecordingInput, "Enable Microphone");
            if (m_Recorder.CheckRecordingInput)
            {
                GUILayout.Label("Click and hold the left mouse button to talk");
            }
            else
            {
                if (m_Recorder.IsRecording)
                {
                    m_Recorder.StopRecording();
                }
            }
            if (m_Recorder.IsRecording)
            {
                GUILayout.Label("Recording...");
            }
            //if (GUILayout.RepeatButton("Press and Hold to Record"))
            //{
            //    //if (!Microphone.IsRecording(RecordingDeviceName))
            //    //{
            //    //    m_MicAudio = Microphone.Start(RecordingDeviceName, false, MicRecordingMaxSeconds, 44100);
            //    //}
            //    m_Recorder.CheckRecordingInput = true;
            //}
            //else
            //{
            //    // note: I have to do this because unity's OnGUI gets called 2x a frame. If the repeat button
            //    // is being held down, then the first call is true, the second false, so I have to check
            //    // which event is being used 
            //    if (Event.current.type != EventType.Layout && Microphone.IsRecording(RecordingDeviceName))
            //    {
            //        //Microphone.End(RecordingDeviceName);
            //        m_Recorder.CheckRecordingInput = false;
            //        //SavWav.Save(ParticipantId, m_MicAudio, ref m_AudioData);
            //        //StartCoroutine(UploadAudioClip(m_WebServerUrl, m_MicAudio));  
            //    }
            //}
        }
        else
        {
            GUILayout.Label("Plug a microphone in to talk to Brad");
        }
    }

    int WrapInt(int intVal, int min, int max)
    {
        if (intVal >= max)
        {
            intVal = min;
        }
        else if (intVal < min)
        {
            intVal = max;
        }
        return intVal;
    }

    public void VHMsg_MessageEvent(object sender, VHMsgBase.Message message)
    {
        string[] splitargs = message.s.Split(" ".ToCharArray());
        Debug.Log("VHMsg_MessageEvent: " + message.s);

        if (splitargs[0] == "vrSpeak" || splitargs[0] == "vrAgentBML")
        {
            m_SpeechBox.TypingEnabled = true;
            if (splitargs.Length > 4)
            {
                if (splitargs[3] == "start" || splitargs[3] == "end")
                {                   
                    return;
                    
                }

                string character = splitargs[1];
                string xml = string.Join(" ", splitargs, 4, splitargs.Length - 4);

                //if (character == "Brad")
                {
                    m_BMLEventHandler.LoadXMLString(character, xml);
                }
            }
        }
        if (splitargs[0] == "RemoteSpeechReply")
        {
            m_SpeechBox.TypingEnabled = true;
            string xml = string.Join(" ", splitargs, 4, splitargs.Length - 4);
            string audioFilePath = "";
            //List<TtsReader.WordTiming> timings = m_TtsReader.ReadTtsXml(xml, out audioFilePath);
            TtsReader.TtsData timings = m_TtsReader.ReadTtsXml(xml, out audioFilePath);
            //Debug.Log("RemoteSpeechReply received!audioFilePath: " + audioFilePath);
            StartCoroutine(PlayTtsAnim(timings.m_WordTimings, audioFilePath));
        }
    }

    IEnumerator PlayTtsAnim(List<TtsReader.WordTiming> timings, string audioFilePath)
    {
        Debug.Log("timings.count: " + timings.Count);
        //AnimationClip clip = m_TtsPlayer.BuildTTSAnimation(timings, true);
        
        audioFilePath = audioFilePath.Replace("\\", "/");
        //audioFilePath = string.Format("{0}/{1}", m_BMLEventHandler.m_AudioUrl, System.IO.Path.GetFileName(audioFilePath));// audioFilePath.Insert(0, m_BMLEventHandler.m_AudioUrl);
        yield return StartCoroutine(DownloadUtteranceCoroutine(System.IO.Path.GetFileName(audioFilePath)));

        // place mouth animation
        //m_FaceFx.PlayAnim(clip.name, m_TtsPlayer.GetComponent<AudioSource>().clip);

        Debug.LogError("PlayTtsAnim() - TtsPlayer deprecated.  TODO - refactor to new method");
    }

    IEnumerator DownloadUtteranceCoroutine( string utteranceName)
    {
        string audioPath = string.Format("{0}/{1}", m_BMLEventHandler.m_AudioUrl, utteranceName);
        Debug.Log("audioPath: " + audioPath);
        WWW www = new WWW(audioPath);
        yield return www;

        while (!www.isDone)
        {
            yield return new WaitForEndOfFrame();
        }

        while (www.GetAudioClip().loadState == AudioDataLoadState.Unloaded || 
               www.GetAudioClip().loadState == AudioDataLoadState.Loading)
        {
            yield return new WaitForEndOfFrame();
        }

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError(string.Format("Failed to download utterance {0}", utteranceName));
        }

        

        //VHUtils.PlayWWWSound(this, www, m_TtsPlayer.GetComponent<AudioSource>(), false);
        Debug.LogError("DownloadUtteranceCoroutine() - TtsPlayer deprecated.  TODO - refactor to new method");
    }

    /// <summary>
    /// called from the console when a 'vhmsg' prefixed command is sent
    /// </summary>
    /// <param name="commandEntered"></param>
    /// <param name="console"></param>
    protected override void HandleConsoleMessage(string commandEntered, DebugConsole console)
    {
        base.HandleConsoleMessage(commandEntered, console);
        if (commandEntered.IndexOf("vhmsg") != -1)
        {
            string opCode = string.Empty;
            string args = string.Empty;
            if (console.ParseVHMSG(commandEntered, ref opCode, ref args))
            {
                if (Network.isServer)
                {
                    //m_MessageBroker.ServerSendsMessageToFIFOClient(opCode);
                }
                else
                {
                    //m_MessageBroker.ClientSendsMessageToServerFIFO(m_ClientId, opCode);
                }
            }
            else
            {
                console.AddText(commandEntered + " requires an opcode string and can have an optional argument string");
            }
        }
    }

    public void SendServerTts(string message)
    {
        StartCoroutine(SendServerMessageCoroutine(m_SpeechUserId, message, true));
    }

    public void SendServerMessage(string message)
    {
        ++m_SpeechUserId;
        StartCoroutine(SendServerMessageCoroutine(m_SpeechUserId, message, false));
    }

    IEnumerator SendServerMessageCoroutine(int speechUserId, string message, bool isTts)
    {
        if (string.IsNullOrEmpty(m_WebServerUrl))
        {
            Debug.LogError(string.Format("SendServerMessage failed. WebServerUrl is null or empty"));
            yield break;
        }

        m_SpeechBox.TypingEnabled = false;
        string url = "";
        if (isTts)
        {
            url = string.Format("{0}?&UserMessage={1}&Voice={2}&ParticipantId={3}&IsMicInput=false&IsTts=true&CharacterName={4}&SessionName={5}", m_WebServerUrl, message.Replace(" ", "%20"), m_TtsVoice, ParticipantId, "Brad", SessionName.Replace(" ", "-"));
        }
        else
        {
            url = string.Format("{0}?SpeechUserId={1}&UserMessage={2}&ClientNeedsResponse=true&ParticipantId={3}&IsMicInput=false&IsTts=false&SessionName={4}", m_WebServerUrl, speechUserId, message.Replace(" ", "%20"), ParticipantId, SessionName.Replace(" ", "-"));
        }

        WWW www = new WWW(url);
        //Debug.Log(url);
        yield return www;

        while (!www.isDone) { yield return new WaitForEndOfFrame(); }

        if (www.error != null)
        {
            Debug.Log(www.error);
            m_SpeechBox.TypingEnabled = true;
        }
        else
        {
            int index = www.text.IndexOf(Splitter);
            if (index != -1)
            {
                string vrSpeakMsg = www.text.Substring(0, index);
                Debug.Log(vrSpeakMsg);
                vhmsg.ReceiveVHMsg(vrSpeakMsg);
            }
            else
            {
                Debug.LogError(string.Format("Expected message vrSpeak was not received"));
                Debug.Log(www.text);
                m_SpeechBox.TypingEnabled = true;
            }
        }
               
    }
    #endregion
}
