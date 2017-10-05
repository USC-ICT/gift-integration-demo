using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

public class BMLParser
{
    #region Constants
    static readonly string[] EventXmlNames =
    {
        "sbm:animation",
        "animation",
        "gaze",
        "head",
        "saccade",
        "face",
        "text",
        "event",
        "sbm:event",
        "speech",
        "gesture",
        "body"
    };

    const string start = "start";
    const string stroke = "stroke";
    const string relax = "relax";
    const string end = "end";

    //const string Speech = "speech";
    const string Participant = "participant";

    class CachedEvent
    {
        public CutsceneEvent ce;
        public string syncPointName;
        public string timing;

        public CachedEvent(CutsceneEvent _ce, string _syncPointName, string _timing)
        {
            ce = _ce;
            syncPointName = _syncPointName;
            timing = _timing;
        }
    }

    [System.Serializable]
    public class BMLTiming
    {
        public string id;
        public float time;
        public string text;

        public BMLTiming(string _id, float _time, string _text)
        {
            id = _id;
            time = _time;
            text = _text;
        }
    }

    [System.Serializable]
    public class LipData
    {
        public string viseme = "";
        public float articulation = 1.0f;
        public float startTime;
        public float readyTime;
        public float relaxTime;
        public float endTime;

        public LipData(string _viseme, float _articulation, float _startTime, float _readyTime, float _relaxTime, float _endTime)
        {
            viseme = _viseme;
            articulation = _articulation;
            startTime = _startTime;
            readyTime = _readyTime;
            relaxTime = _relaxTime;
            endTime = _endTime;
        }
    }

    [System.Serializable]
    public class CurveData
    {
        public class SlopeData
        {
            public float mr;
            public float dr;
            public float ml;
            public float dl;

            public SlopeData(float _ml, float _mr, float _dl, float _dr)
            {
                ml = _ml;
                mr = _mr;
                dl = _dl;
                dr = _dr;
            }
        }

        readonly public string name = ""; // i.e. BMP
        readonly public string owner = "";
        readonly public int numKeys = 0;
        readonly public Quaternion[] curveKeys;
        readonly public SlopeData[] m_SlopeData;

        public CurveData(string _name, string _owner, int _numKeys)
        {
            name = _name;
            owner = _owner;
            numKeys = _numKeys;

            if (numKeys > 0)
            {
                curveKeys = new Quaternion[numKeys];
                m_SlopeData = new SlopeData[numKeys];
            }
        }

        public void Set(int keyIndex, float _ml, float _mr, float _dl, float _dr)
        {
            m_SlopeData[keyIndex] = new SlopeData(_ml, _mr, _dl, _dr);
        }

        public SlopeData GetSlopeData(int key)
        {
            if (key < 0 || key > numKeys)
            {
                Debug.LogError("Bad Key " + key + " for viseme " + name);
                return null;
            }
            return m_SlopeData[key];
        }

        public void AddKey(Quaternion key, int keyIndex)
        {
            if (keyIndex < 0 || keyIndex >= numKeys)
            {
                Debug.LogError(string.Format("bad keyIndex {0}. Has to be in range 0-{1}", keyIndex, numKeys - 1));
            }
            else
            {
                curveKeys[keyIndex] = key;
            }
        }

        public void AddKey(float time, float articulation, int keyIndex)
        {
            AddKey(new Quaternion(time, articulation, 0, 0), keyIndex);
        }

        public float GetTime(int key)
        {
            if (key < 0 || key > numKeys)
            {
                Debug.LogError("Bad Key " + key + " for viseme " + name);
                return 0;
            }
            return curveKeys[key].x;
        }

        public float GetArticulation(int key)
        {
            if (key < 0 || key > numKeys)
            {
                Debug.LogError("Bad Key " + key + " for viseme " + name);
                return 0;
            }
            return curveKeys[key].y;
        }

        public float GetSlopeIn(int key)
        {
            if (key < 0 || key > numKeys)
            {
                Debug.LogError("Bad Key " + key + " for viseme " + name);
                return 0;
            }
            return curveKeys[key].z;
        }

        public float GetSlopeOut(int key)
        {
            if (key < 0 || key > numKeys)
            {
                Debug.LogError("Bad Key " + key + " for viseme " + name);
                return 0;
            }
            return curveKeys[key].w;
        }

        public float GetSpan()
        {
            float span = 0;
            if (numKeys > 1)
            {
                span = curveKeys[numKeys - 1].x - curveKeys[0].x;
            }
            return span;
        }

        public void SortKeysByTime()
        {
            Array.Sort<Quaternion>(curveKeys, (a, b) => a.x < b.x ? -1 : 1);
        }

        public void PrintCurve()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(string.Format("Curve Name {0}. Num Keys {1} ", name, numKeys));
            for (int i = 0; i < numKeys; i++)
            {
                builder.Append(string.Format(" Time {0} Weight {1} ", GetTime(i), GetArticulation(i)));
            }
            Debug.Log(builder.ToString());
        }
    }

    public delegate void OnParsedBMLTiming(/*string id, float time, string text*/BMLTiming bmlTiming);
    public delegate void OnParsedVisemeTiming(LipData lipData);
    public delegate void OnParsedCurveData(CurveData curveData);
    public delegate void OnParsedBMLEvent(XmlTextReader reader, string eventType, CutsceneEvent ce);
    public delegate void OnFinishedReading(bool succeeded, List<CutsceneEvent> createdEvents);
    public delegate void OnReadBMLFile(string bmlFileName);
    public delegate void OnParsedCustomEvent(XmlTextReader reader);
    #endregion

    #region Variables
    OnParsedBMLTiming m_ParsedBMLTimingCB;
    OnParsedVisemeTiming m_ParsedVisemeTimingCB;
    OnParsedCurveData m_ParsedCurveDataCB;
    OnParsedBMLEvent m_ParsedBMLEventCB;
    OnFinishedReading m_FinishedReadingCB;
    OnReadBMLFile m_ReadBmlFileCB;
    OnParsedCustomEvent m_ParsedCustomEventCB;
    List<CachedEvent> m_CachedEvents = new List<CachedEvent>();
    List<CutsceneEvent> m_CreatedEvents = new List<CutsceneEvent>();
    List<BMLTiming> m_BMLTimings = new List<BMLTiming>();
    AudioSpeechFile m_Utterance;
    string m_LoadPath = "";
    string m_Character = "";
    string m_SpeechId = "";
    bool m_ReadBMLFile;
    bool m_BMLFileHasBeenRead;
    string m_CachedXml = "";
    string m_EventCategoryName = GenericEventNames.SmartBody;
    string m_LoadPathSubFolder = "";
    #endregion

    #region Properties
    public string EventCategoryName
    {
        get { return m_EventCategoryName; }
        set
        {
            m_EventCategoryName = value;
            if (m_EventCategoryName != GenericEventNames.SmartBody && m_EventCategoryName != GenericEventNames.Mecanim)
            {
                m_EventCategoryName = GenericEventNames.SmartBody;
            }
        }
    }

    public bool IgnoreSpeechEvent { get { return m_Utterance != null; } }

    public string LoadPathSubFolder
    {
        get { return m_LoadPathSubFolder; }
        set { m_LoadPathSubFolder = value; }
    }

    #endregion

    #region Functions
    public BMLParser(OnParsedBMLTiming parsedBMLTimingCB, OnParsedVisemeTiming parsedVisemeTimingCB, OnParsedBMLEvent parsedBMLEventCB, OnFinishedReading finishedReadingCB, OnParsedCustomEvent parsedCustomEventCB)
    {
        m_ParsedBMLTimingCB = parsedBMLTimingCB;
        m_ParsedVisemeTimingCB = parsedVisemeTimingCB;
        m_ParsedBMLEventCB = parsedBMLEventCB;
        m_FinishedReadingCB = finishedReadingCB;
        m_ParsedCustomEventCB = parsedCustomEventCB;
    }

    public BMLParser(OnParsedBMLTiming parsedBMLTimingCB, OnParsedVisemeTiming parsedVisemeTimingCB, OnParsedCurveData parsedCurveDataCB)
    {
        m_ParsedBMLTimingCB = parsedBMLTimingCB;
        m_ParsedVisemeTimingCB = parsedVisemeTimingCB;
        m_ParsedCurveDataCB = parsedCurveDataCB;
    }

    public void AddOnReadBMLFileCB(OnReadBMLFile cb)
    {
        m_ReadBmlFileCB += cb;
    }

    /// <summary>
    /// Loads and reads either a bml or xml file. Returns true if successfully read
    /// </summary>
    /// <param name="filePathAndName"></param>
    /// <returns></returns>
    public bool LoadFile(string filePathAndName)
    {
        string fileExt = Path.GetExtension(filePathAndName);
        if (!File.Exists(filePathAndName))
        {
            if (fileExt.ToLower() == ".bml" && File.Exists(filePathAndName + ".txt"))
            {
                filePathAndName += ".txt";
            }
            else
            {
                return false;
            }
        }

        bool success = false;
        if (fileExt.ToLower() == ".xml")
        {
            success = LoadXMLFile(filePathAndName);
        }
        else if (fileExt.ToLower() == ".bml" || fileExt.ToLower() == ".txt")
        {
            success = LoadBMLFile(filePathAndName);
        }
        else
        {
            Debug.LogError(string.Format("Couldn't load {0} because it's not a supported file extension", filePathAndName));
        }

        return success;
    }

    /// <summary>
    /// Read a bml file, internal only
    /// </summary>
    /// <param name="filePathAndName"></param>
    /// <returns></returns>
    bool LoadBMLFile(string filePathAndName)
    {
        bool succeeded = true;

        FileStream xml = null;
        XmlTextReader reader = null;
        try
        {
            xml = new FileStream(filePathAndName, FileMode.Open, FileAccess.Read);
            reader = new XmlTextReader(xml);
            ReadBML(reader);
        }
        catch (Exception e)
        {
            succeeded = false;
            Debug.LogError(string.Format("Failed when loading {0}. Error: {1}", filePathAndName, e.Message));
        }
        finally
        {
            if (xml != null)
            {
                xml.Close();
            }
            if (reader != null)
            {
                reader.Close();
            }

            FinishedReadingBML(succeeded);
        }

        return succeeded;
    }

    public bool LoadBMLString(string bmlStr)
    {
        return LoadBMLString(bmlStr, true);
    }

    public bool LoadBMLString(string bmlStr, bool skipBOM)
    {
        bool succeeded = true;
        XmlTextReader reader = null;
        StringReader bml = null;

        try
        {
            bml = new StringReader(bmlStr);
            if (skipBOM)
            {
                bml.Read(); // skip BOM see this link for more detail: http://answers.unity3d.com/questions/10904/xmlexception-text-node-canot-appear-in-this-state.html
            }
            reader = new XmlTextReader(bml);
            ReadBML(reader);
        }
        catch (Exception e)
        {
            succeeded = false;
            Debug.LogError(string.Format("Failed when loading. Error: {0} {1}. bmlStr {2}", e.Message, e.InnerException, bmlStr));
        }
        finally
        {
            if (bml != null)
            {
                bml.Close();
            }

            if (reader != null)
            {
                reader.Close();
            }

            FinishedReadingBML(succeeded);
        }

        return succeeded;
    }

    void ReadBML(XmlTextReader reader)
    {
        while (reader.Read())
        {
            switch (reader.NodeType)
            {
            case XmlNodeType.Element:
                if (reader.Name == "sync")
                {
                    string id = reader["id"];
                    float time = float.Parse(reader["time"]);

                    // NOTE I removed this because it was skipping the next line unless the current line was structed like: <sync id="T2" time="0.435" />test
                    // if the "test" wasn't there, then the line would be skipped. Now the word at the end of the line is no longer getting parsed, so
                    // that needs to be fixed. This, however, is malformed xml.
                    reader.ReadInnerXml(); // this is a hack, i do this so I can get to the text portion of the xml
                    if (m_ParsedBMLTimingCB != null)
                    {
                        BMLTiming bmlTiming = new BMLTiming(id, time, reader.Value.Trim()/*fix this*/);
                        m_BMLTimings.Add(bmlTiming);
                        m_ParsedBMLTimingCB(bmlTiming);
                    }
                }
                else if (reader.Name == "lips")
                {
                    float start = 0;
                    float.TryParse(reader["start"], out start);

                    float end = 0;
                    float.TryParse(reader["end"], out end);

                    float ready = 0;
                    float.TryParse(reader["ready"], out ready);

                    float relax = 0;
                    float.TryParse(reader["relax"], out relax);

                    float articulation = 1.0f;
                    float.TryParse(reader["articulation"], out articulation);

                    LipData lipData = new LipData(reader["viseme"], articulation, start, ready, relax, end);
                    if (m_ParsedVisemeTimingCB != null)
                    {
                        m_ParsedVisemeTimingCB(lipData);
                    }
                }
                else if (reader.Name == "curve")
                {
                    int numKeys = 0;
                    int.TryParse(reader["num_keys"], out numKeys);

                    if (numKeys > 0)
                    {
                        CurveData curveData = new CurveData(reader["name"], reader["owner"], numKeys);
                        string curveString = reader.ReadString();
                        curveString = curveString.Trim();
                        string[] curves = curveString.Split(' ');

                        //Debug.Log("numKeys: " + numKeys + " curves.Length: " + curves.Length);
                        for (int i = 0; i < curves.Length; i += 4)
                        {
                            curveData.AddKey(new Quaternion(float.Parse(curves[i]), float.Parse(curves[i + 1]),
                                float.Parse(curves[i + 2]), float.Parse(curves[i + 3])), i / 4);
                        }

                        if (m_ParsedCurveDataCB != null)
                        {
                            m_ParsedCurveDataCB(curveData);
                        }
                    }
                }
                break;
            }
        }
    }

    public bool LoadXMLBMLStrings(string character, string xmlStr, string bmlStr)
    {
        LoadBMLString(bmlStr, false);
        return LoadXMLString(character, xmlStr);
    }

    public bool LoadXMLBMLStrings(string character, AudioSpeechFile speech)
    {
        m_Utterance = speech;
        return LoadXMLBMLStrings(character, speech.ConvertedXml, speech.BmlText);
    }

    /// <summary>
    /// Reads the contents of an xml file as a string
    /// </summary>
    /// <param name="xmlStr"></param>
    /// <returns></returns>
    public bool LoadXMLString(string character, string xmlStr)
    {
        m_Character = character;
        bool succeeded = true;
        StringReader xml = null;
        XmlTextReader reader = null;
        m_CachedEvents.Clear();
        m_CreatedEvents.Clear();
        m_ReadBMLFile = !IgnoreSpeechEvent;;
        m_CachedXml = xmlStr;

        try
        {
            xml = new StringReader(xmlStr);
            reader = new XmlTextReader(xml);
            ParseBMLEvents(reader);
        }
        catch (Exception e)
        {
            succeeded = false;
            Debug.LogError(string.Format("Failed when loading. Error: {0} {1}. couldn't load string {2}", e.Message, e.InnerException, xmlStr));
        }
        finally
        {
            if (xml != null)
            {
                xml.Close();
            }

            if (reader != null)
            {
                reader.Close();
            }
        }

        FinishedReadingXML(succeeded);
        return succeeded;
    }

    /// <summary>
    /// Reads an xml files. Internal use only
    /// </summary>
    /// <param name="filePathAndName"></param>
    /// <returns></returns>
    bool LoadXMLFile(string filePathAndName)
    {
        m_LoadPath = filePathAndName;
        bool succeeded = true;
        m_ReadBMLFile = !IgnoreSpeechEvent;;
        FileStream xml = null;
        XmlTextReader reader = null;

        try
        {
            xml = new FileStream(filePathAndName, FileMode.Open, FileAccess.Read);
            reader = new XmlTextReader(xml);
            ParseBMLEvents(reader);
        }
        catch (Exception e)
        {
            succeeded = false;
            Debug.LogError(string.Format("Failed when loading {0}. Error: {1} {2}", filePathAndName, e.Message, e.InnerException));
        }
        finally
        {
            if (xml != null)
            {
                xml.Close();
            }

            if (reader != null)
            {
                reader.Close();
            }
        }

        FinishedReadingXML(succeeded);
        return succeeded;
    }

    void FinishedReadingBML(bool succeeded)
    {
        if (!string.IsNullOrEmpty(m_CachedXml))
        {
            if (VHUtils.IsWebGL())
            {
                m_BMLFileHasBeenRead = true; // do this first
                LoadXMLString(m_Character, m_CachedXml);
            }
        }
        

        if (string.IsNullOrEmpty(m_LoadPath))
        {
            // no xml is associated, but we still want to tell the receiver that we're done
            if (m_FinishedReadingCB != null)
            {
                m_FinishedReadingCB(succeeded, m_CreatedEvents);
            }
        }
    }

    void FinishedReadingXML(bool succeeded)
    {
        // handled the cached events first
        m_CachedEvents.ForEach(c => HandleCachedEvent(c));

        // then do the callback
        if (m_FinishedReadingCB != null)
        {
            m_FinishedReadingCB(succeeded, m_CreatedEvents);
        }

        // now reset all the data
        m_Utterance = null;
        m_BMLTimings.Clear();
        m_CachedEvents.Clear();
        m_CreatedEvents.Clear();
        m_Character = string.Empty;
        m_ReadBMLFile = false;
        //m_CachedXml = string.Empty;
        m_LoadPath = "";
        if (VHUtils.IsWebGL())
            m_BMLFileHasBeenRead = false;
    }

    /// <summary>
    /// Reads the xml file line by line and creates events based off the node type listed in EventXmlNames
    /// </summary>
    /// <param name="reader"></param>
    void ParseBMLEvents(XmlTextReader reader)
    {
        StringReader xml = null;

        if (VHUtils.IsWebGL())
        {
            // First we need to check if a BML file has to be loaded in order to find timing markers for events in the xml
            if (m_ReadBMLFile && !m_BMLFileHasBeenRead)
            {
                while (reader.Read())
                {
                    switch (reader.Name)
                    {
                        case "speech":
                            m_SpeechId = reader["id"];
                            m_LoadPath =  reader["ref"];
                            WWW www = new WWW("https://example.com/vhweb/Sounds/" + Path.ChangeExtension(m_LoadPath, ".bml"));
                            GameObject.Find("GenericEvents").GetComponent<MonoBehaviour>().StartCoroutine(WaitForBML(www));
                            return;
                    }
                }

                // if you've gotten this far, the reader needs to be reset because it didn't find any speech
                reader.Close();
                xml = new StringReader(m_CachedXml);
                reader = new XmlTextReader(xml);
            }
        }

        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    int index = Array.FindIndex<string>(EventXmlNames, s => s == reader.Name.ToLower());
                    if (index != -1)
                    {
                        CreateEvent(reader, reader.Name);
                    }
                    else if (reader.Name.ToLower() == Participant)
                    {
                        if (string.IsNullOrEmpty(m_Character))
                        {
                            m_Character = reader["id"];
                        }
                    }
                    else
                    {
                        // this is custom so it will require custom parsing
                        if (m_ParsedCustomEventCB != null)
                        {
                            m_ParsedCustomEventCB(reader);
                        }
                    }
                    break;
            }
        }

        if (VHUtils.IsWebGL())
        {
            if (xml != null)
            {
                xml.Close();
            }
        }
    }

    float ParseEventStartTime(string startTime)
    {
        float eventStart = 0;
        if (!float.TryParse(startTime, out eventStart))
        {
            if (!string.IsNullOrEmpty(startTime))
            {
                // looks for timing markers that were read from the bml
                string[] split = startTime.Split(':');
                for (int i = 0; i < split.Length; i++)
                {
                    if (split[i].IndexOf(m_SpeechId) != -1)
                    {
                        string timing = split[i + 1];
                        float offset = 0;
                        if (timing.Contains("+"))
                        {
                            string[] newSplit = timing.Split('+');
                            timing = newSplit[0];
                            float.TryParse(newSplit[1], out offset);
                        }
                        else if (timing.Contains("-"))
                        {
                            string[] newSplit = timing.Split('-');
                            timing = newSplit[0];
                            float.TryParse(newSplit[1], out offset);
                        }

                        BMLTiming bmlTiming = m_BMLTimings.Find(t => t.id == timing);
                        if (bmlTiming != null)
                        {
                            eventStart = bmlTiming.time + offset;
                        }
                        break;
                    }
                }
            }
        }

        return eventStart;
    }

    CutsceneEvent CreateNewEvent(XmlTextReader reader)
    {
        float eventStart = 0;
        float eventLength = 1;
        if (!string.IsNullOrEmpty(reader[start]))
        {
            eventStart = ParseEventStartTime(reader[start]);
        }
        else if (!string.IsNullOrEmpty(reader[stroke]))
        {
            eventStart = ParseEventStartTime(reader[stroke]);
        }
        else if (!string.IsNullOrEmpty(reader[relax]))
        {
            // TODO: this is a hack for now since I don't have a start time and relax time == start time - smartbody default values which aren't clear
            eventStart = ParseEventStartTime(reader[relax]);
        }

        if (!string.IsNullOrEmpty(reader[end]))
        {
            eventLength = Mathf.Max(ParseEventStartTime(reader[end]) - eventStart, 0);
        }

        CutsceneEvent ce = new CutsceneEvent(new Rect(), Guid.NewGuid().ToString());
        m_CreatedEvents.Add(ce);
        ce.Name = reader["id"];
        if (string.IsNullOrEmpty(ce.Name))
        {
            ce.Name = reader["mm:eventName"];
        }
        ce.StartTime = eventStart;
        ce.Length = eventLength;

        // sets up the target gameobject and component
        ChangedCutsceneEventType(m_EventCategoryName, ce);

        return ce;
    }

    /// <summary>
    /// Creates an event and sets up it's parameters based on the xml data
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="type"></param>
    void CreateEvent(XmlTextReader reader, string type)
    {
        CutsceneEvent ce = CreateNewEvent(reader);
        int functionOverload = 0;
        if (!int.TryParse(reader["mm:overload"], out functionOverload))
        {
            functionOverload = 0;
        }
        switch (type)
        {
            case "sbm:animation":
            case "animation":
                ce.ChangedEventFunction("PlayAnim", functionOverload);
                break;

            case "gaze":
                ce.ChangedEventFunction(reader["mm:advanced"] == "true" || reader["advanced"] == "true" ? "GazeAdvanced" : "Gaze", functionOverload);
                break;

            case "head":
                if (string.Compare(reader["type"], "NOD", true) == 0)
                {
                    ce.ChangedEventFunction("Nod", functionOverload);
                }
                else if (string.Compare(reader["type"], "SHAKE", true) == 0)
                {
                    ce.ChangedEventFunction("Shake", functionOverload);
                }
                else
                {
                    // toss
                    ce.ChangedEventFunction("Tilt", functionOverload);
                }
                break;

            case "saccade":
                ce.ChangedEventFunction("Saccade", functionOverload);
                break;

            case "face":
                ce.ChangedEventFunction("PlayFAC", functionOverload);
                break;

            case "sbm:event":
            case "event":
                ParseVhmsgEvent(reader, type, ce, functionOverload);
                break;

            case "gesture":
                ce.ChangedEventFunction("Gesture", functionOverload);
                break;

            case "body":
                ce.ChangedEventFunction("Posture", functionOverload);
                break;

            case "speech":
                m_SpeechId = reader["id"];
                if (string.IsNullOrEmpty(m_LoadPath))
                {
                    // only set the load path if it wasn't already set. You'll get here if LoadXmlString was called
                    m_LoadPath = reader["ref"];
                }

                string fileName = Path.ChangeExtension(m_LoadPath, ".bml");
                if (VHUtils.IsWebGL())
                    functionOverload = 1;

                if (m_EventCategoryName == GenericEventNames.Mecanim)
                {
                    functionOverload = 2;
                }

                ce.ChangedEventFunction("PlayAudio", functionOverload);

                if (!VHUtils.IsWebGL())
                {
                    if (m_ReadBMLFile)
                    {
                        string path = "";
                        if (m_LoadPath.Contains("/") || m_LoadPath.Contains("\\"))
                        {
                            // use the xml's path
                            path = fileName;
                        }
                        else
                        {
                            // the xml doesn't have a path, just a filename so we have to create a path
                            path = string.Format("{0}/{1}", Application.dataPath, fileName);
                            if (!string.IsNullOrEmpty(path))
                            {
                                path = string.Format("{0}/{1}/{2}", Application.dataPath, m_LoadPathSubFolder, fileName);
                            }
                        }

                        LoadFile(path);
                    }
                }
                break;
        }

        ce.SetParameters(reader);

        SetCharacterParam(ce, m_Character);

        if (!string.IsNullOrEmpty(reader[start]))
            m_CachedEvents.Add(new CachedEvent(ce, start, reader[start]));

        if (!string.IsNullOrEmpty(reader[stroke]))
            m_CachedEvents.Add(new CachedEvent(ce, stroke, reader[stroke]));

        if (!string.IsNullOrEmpty(reader[relax]))
            m_CachedEvents.Add(new CachedEvent(ce, relax, reader[relax]));

        if (!string.IsNullOrEmpty(reader[end]))
            m_CachedEvents.Add(new CachedEvent(ce, end, reader[end]));

        if (m_ParsedBMLEventCB != null)
        {
            m_ParsedBMLEventCB(reader, reader.Name, ce);
        }
    }


    public void SetCharacterParam(CutsceneEvent ce, string characterName)
    {
        if (ce == null)
        {
            return;
        }


        if (ce.EventType == GenericEventNames.SmartBody || ce.EventType == GenericEventNames.Mecanim)
        {
            CutsceneEventParam characterParam = ce.FindParameter("character");
            if (characterParam != null)
            {
                if (characterParam.objData == null && !string.IsNullOrEmpty(characterName))
                {
                    if (ce.EventType == GenericEventNames.SmartBody)
                    {
                        ICharacter sceneCharacter = MecanimEvents.MecanimEvent_Base.FindCharacter(characterName, ce.Name);
                        if (sceneCharacter != null)
                        {
                            characterParam.SetObjData(sceneCharacter);
                        }
                    }

                    if (characterParam.objData == null)
                    {
                        if (string.IsNullOrEmpty(characterParam.stringData))
                        {
                            characterParam.stringData = characterName;
                        }

                    }
                }
            }
            else
            {
                Debug.LogError(string.Format("Event {0} doesn't have a character param?", ce.Name));
            }
        }
    }

    IEnumerator WaitForBML(WWW www)
    {
        while (!www.isDone) { yield return new WaitForEndOfFrame(); Debug.Log("still waiting"); }
        //Debug.Log("www.text: " + www.text);
        LoadBMLString(www.text);
    }

    static public void ChangedCutsceneEventType(string newType, CutsceneEvent ce)
    {
        ce.EventType = newType;

        // TODO: THIS IS A HACK! get a reference to a generic events object!
        GenericEvents[] genericEventsGO = GameObject.Find("GenericEvents").GetComponentsInChildren<GenericEvents>();
        if (genericEventsGO == null)
        {
            Debug.LogError(string.Format("BMLParser doesn't have a GenericEvents componenent anywhere"));
            return;
        }

        MonoBehaviour targetComponent = null;
        foreach (GenericEvents ge in genericEventsGO)
        {
            if (ge.GetEventType() == newType)
            {
                targetComponent = ge;
                break;
            }
        }

        if (targetComponent != null)
        {
            ce.SetFunctionTargets(targetComponent.gameObject, targetComponent);
        }
        else
        {
            ce.SetFunctionTargets(null, null);
        }
    }

    void ParseVhmsgEvent(XmlTextReader xml, string type, CutsceneEvent ce, int overload)
    {
        string message = xml["message"];
        if (message.IndexOf("saccade") != -1)
        {
            // this is a saccade event
            if (!string.IsNullOrEmpty(xml["mm:stopSaccade"]) || !string.IsNullOrEmpty(xml["stopSaccade"]))
            {
                ce.ChangedEventFunction("StopSaccade");
            }
            else
            {
                ce.ChangedEventFunction("Saccade");
            }
        }
        else if (message.IndexOf("viseme") != -1)
        {
            ce.ChangedEventFunction("PlayViseme", overload);
        }
        else if (message.IndexOf("gazefade out") != -1)
        {
            ce.ChangedEventFunction("StopGaze", overload);
        }
        else
        {
            // event start times are usually based off of other events using event names. Because of this,
            // we need to cache this event, and later try to find the event that it's parented to
            
            ChangedCutsceneEventType(GenericEventNames.Common, ce);
            ce.ChangedEventFunction("SendVHMsg");
        }
    }

    /// <summary>
    /// Called after all events have been read from the xml file. Handles timing adjustments
    /// for events that are timed based off of other events in the xml file
    /// </summary>
    /// <param name="cache"></param>
    void HandleCachedEvent(CachedEvent cache)
    {
        // typical format stroke=[event name]:start+[time offset]
        string[] colonSplit = cache.timing.Split(':');
        if (colonSplit.Length != 2)
        {
            return;
        }

        bool useMathOperation = true;
        string[] mathOpSplit = colonSplit[1].Split('+');
        if (mathOpSplit.Length != 2)
        {
            mathOpSplit = colonSplit[1].Split('-');
            if (mathOpSplit.Length != 2)
            {
                useMathOperation = false;
            }
        }

        string eventSyncPointName = cache.syncPointName;
        string parentSyncPointName = colonSplit[1];

        // the name of the event is the first half
        //m_CreatedEvents.ForEach(ce => Debug.Log(ce.Name));
        CutsceneEvent parentTimer = m_CreatedEvents.Find(ce => string.Compare(ce.Name, colonSplit[0], true) == 0);
        if (parentTimer != null)
        {
            float offset = 0;
            if (useMathOperation)
            {
                float.TryParse(mathOpSplit[1], out offset);
            }

            float eventTime = 0;
            if (parentSyncPointName == start || parentSyncPointName == stroke)
            {
                eventTime = parentTimer.StartTime + offset;
            }
            else
            {
                eventTime = parentTimer.EndTime + offset;
            }

            if (eventSyncPointName == start || eventSyncPointName == stroke)
            {
                cache.ce.StartTime = eventTime;
            }
            else
            {
                cache.ce.Length = eventTime - cache.ce.StartTime;
            }
        }
    }
    #endregion
}
