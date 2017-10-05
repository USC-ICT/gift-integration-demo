using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

[RequireComponent(typeof(VHWayPointNavigator))]

public class VHTimeDemo : MonoBehaviour
{
    #region Constants
    const string TimeDemoCommand = "time_demo";
    const float AmountToTurnAtWP = 360; // degrees
    const string TimeDemoUrl = "http://uperf/AddTimeDemo.php";
    const int NumAvgFpsEntries = 50;

    struct TimeData
    {
        public float time;
        public float curfps;
        public float avgfps;
        public TimeData(float _time, float _curfps, float _avgfps)
        {
            time = _time;
            curfps = _curfps;
            avgfps = _avgfps;
        }
    }
    #endregion

    #region Variables
    public FpsCounter m_FpsCounter;
    public string m_TimeDemoName = "timeDemo01";
    public string m_PerformanceLogName = "vhPerformanceLog.log";
    public string m_ProjectName = "Unnamed Project";
    public bool m_TurnWhenReachWP = true;
    float m_TurnSpeed = 180;
    public float m_TimeDemoLength = 10; // in seconds
    public float m_FpsSamplingRate = 0.1f;
    public float m_DelayWhenStarted = 0;
    public GameObject m_CallbackFunctionContainer;
    public string m_OnStartFuncCallbackName = "";
    public string m_OnFinishFuncCallbackName = "";
    public bool m_ExitAfterFinished = false;

    // private
    VHWayPointNavigator m_WPNavigator;
    //VHBinaryParser m_BinaryParser = new VHBinaryParser();
    Vector2 m_TimeDemoStartEndFrames = new Vector2();
    float m_StartTime = 0;
    List<TimeData> m_TimeData = new List<TimeData>();
    //List<float> m_Fps = new List<float>();
    LinkedList<float> m_AvgFpsTracker = new LinkedList<float>();

    string m_sceneName;
    string m_computerName;
    System.DateTime m_dateOfDemo;
    bool m_LookAtTargetWayPoint;


    #endregion

    #region Properties
    public float TimeDemoLength
    {
        get { return m_TimeDemoLength; }
        set { m_TimeDemoLength = value; }
    }
    #endregion

    #region Functions
    // Use this for initialization
    //public override void VHStart()
    void Start()
    {
        m_WPNavigator = (VHWayPointNavigator)GetComponent<VHWayPointNavigator>();
        if (m_WPNavigator == null)
        {
            Debug.LogError("VHTimeDemo needs VHWayPointNavigator");
        }
        else
        {
            if (m_TurnWhenReachWP)
            {
                m_WPNavigator.AddWayPointReachedCallback(OnWayPointReached);
            }
            m_LookAtTargetWayPoint = m_WPNavigator.TurnTowardsTargetPosition;
            m_WPNavigator.TurnTowardsTargetPosition = false; // we turn this off otherwise it messes up our calculations
        }

        if (m_FpsCounter == null)
        {
            Debug.LogError("VHTimeDemo needs FpsCounter");
        }

        //Profiler.logFile = m_PerformanceLogName;
        //Profiler.enableBinaryLog = true; // writes to "m_PerformanceLogName.log.data"
        //Profiler.enabled = true;

        m_sceneName = SceneManager.GetActiveScene().name;
        m_computerName = System.Environment.MachineName;

        if (m_WPNavigator.ImmediatelyStartPathing)
        {
            StartTimeDemo(m_ExitAfterFinished);
        }

        // uncomment this to dump a bunch of data in the database
        //UploadTestData();
    }

    public void StartTimeDemo(bool exitAfterFinished, string projectName, string timeDemoName)
    {
        m_ProjectName = projectName;
        m_TimeDemoName = timeDemoName;
        StartTimeDemo(exitAfterFinished);
    }

    public void StartTimeDemo(bool exitAfterFinished)
    {
        if (m_WPNavigator == null)
        {
            m_WPNavigator = (VHWayPointNavigator)GetComponent<VHWayPointNavigator>();
        }

        if (!string.IsNullOrEmpty(m_OnStartFuncCallbackName))
        {
            m_CallbackFunctionContainer.SendMessage(m_OnStartFuncCallbackName, SendMessageOptions.RequireReceiver);
        }

        m_ExitAfterFinished = exitAfterFinished;
        m_TimeDemoStartEndFrames.x = Time.frameCount;
        m_StartTime = Time.timeSinceLevelLoad;

        // record fps
        StartCoroutine(RecordFPS());
    }

    void FinishedTimeDemo()
    {
        m_TimeDemoStartEndFrames.y = Time.frameCount;
        m_dateOfDemo = System.DateTime.Now;
        //m_Fps = new List<float>((int)(m_TimeDemoStartEndFrames.y - m_TimeDemoStartEndFrames.x));
        Debug.Log(Time.timeSinceLevelLoad - m_StartTime);
        StopAllCoroutines();

        if (!string.IsNullOrEmpty(m_OnFinishFuncCallbackName))
        {
            m_CallbackFunctionContainer.SendMessage(m_OnFinishFuncCallbackName, SendMessageOptions.RequireReceiver);
        }

        //ReadFramerateData();
        StartCoroutine(UploadTimeDemoData(TimeDemoUrl, m_ExitAfterFinished));
    }

    IEnumerator RecordFPS()
    {
        yield return new WaitForSeconds(m_DelayWhenStarted);

        m_TimeData.Clear();


        float travelSpeedBetweenWPs = 0;
        if (m_TurnWhenReachWP)
        {
            // spend half your time moving between waypoints and the other half rotating
            // 360 degrees when you reach each waypoint
            travelSpeedBetweenWPs = (m_WPNavigator.GetTotalPathLength()) / (m_TimeDemoLength * 0.5f);
            m_TurnSpeed = (AmountToTurnAtWP * m_WPNavigator.NumWayPoints) / (m_TimeDemoLength * 0.5f);
        }
        else
        {
            // spend all your time moving, not turning
            travelSpeedBetweenWPs = m_WPNavigator.GetTotalPathLength() / m_TimeDemoLength;
        }

        m_WPNavigator.SetSpeed(travelSpeedBetweenWPs);
        m_WPNavigator.NavigatePath(true);

        float startTime = Time.timeSinceLevelLoad;
        while (true)
        {
            if (m_FpsCounter.Fps == 0)
            {
                yield return new WaitForSeconds(m_FpsSamplingRate);
                continue;
            }

            if (m_AvgFpsTracker.Count >= NumAvgFpsEntries)
            {
                m_AvgFpsTracker.RemoveFirst();
            }

            m_AvgFpsTracker.AddLast(m_FpsCounter.Fps);

            m_TimeData.Add(new TimeData(Time.timeSinceLevelLoad - startTime, m_FpsCounter.Fps, GetSamplingAverageFps()));
            yield return new WaitForSeconds(m_FpsSamplingRate);
        }
    }

    void OnWayPointReached(VHWayPointNavigator navigator, Vector3 wp, int wpId, int totalNumWPs)
    {
        // do a 360 degree turn
        StartCoroutine(Turn(navigator.Pather, m_TurnSpeed, AmountToTurnAtWP, navigator.Pather.transform.up, wpId == totalNumWPs - 1));
        navigator.SetIsPathing(false);
    }

    IEnumerator Turn(GameObject turner, float turnSpeed, float degreesToTurn, Vector3 rotationAxis, bool finishedPath)
    {
        if (turnSpeed == 0)
            yield break;

        float amountTurned = 0;

        while (true)
        {
            float amountThisFrame = turnSpeed * Time.deltaTime;
            amountTurned += amountThisFrame;
            turner.transform.Rotate(rotationAxis, amountThisFrame);

            if (amountTurned >= degreesToTurn)
                break;

            yield return new WaitForEndOfFrame();
        }

        if (finishedPath)
        {
            FinishedTimeDemo();
        }
        else
        {
            m_WPNavigator.SetIsPathing(true);

            if (m_LookAtTargetWayPoint)
            {
                VHMath.TurnTowardsTarget(this, m_WPNavigator.Pather, m_WPNavigator.TurnTarget, m_WPNavigator.AngularVelocity);
            }
        }
    }

    //public override void VHOnApplicationQuit ()
    void OnApplicationQuit()
    {
        //ReadPerformanceData();
        //ReadFramerateData();
    }

    /*void ReadFramerateData()
    {
        string line = string.Empty;
        string frameSearchString = "-- Frame ";
        string frameRateRearchString = "Framerate: ";
        int startIndex = 0;
        int currentFrame = 0;
        StreamReader file = null;
        m_Fps.Clear();

        try
        {
            // Read the file and display it line by line.
            file = new StreamReader(m_PerformanceLogName);
            while((line = file.ReadLine()) != null)
            {
                // first read the frame number to make sure it's in the
                // time span of our time demo
                startIndex = line.IndexOf(frameSearchString);
                if (startIndex != 0)
                {
                    line = line.Remove(0, startIndex + frameSearchString.Length);
                    line = line.Substring(0, line.IndexOf(" "));
                    currentFrame = int.Parse(line);
                }

                if (currentFrame < m_TimeDemoStartEndFrames.x
                    || currentFrame > m_TimeDemoStartEndFrames.y)
                {
                    // the frame didn't happen during the time demo
                    continue;
                }

                // read the fps of the frame
                startIndex = line.IndexOf(frameRateRearchString);
                if (startIndex != -1)
                {
                    line = line.Remove(0, startIndex + frameRateRearchString.Length);
                    line = line.Substring(0, line.IndexOf(" "));
                    m_Fps.Add(float.Parse(line));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed parsing file " + m_PerformanceLogName + " Error: " + e.Message);
        }
        finally
        {
            if (file != null)
                file.Close();
        }
    }

    void ReadPerformanceData()
    {
        try
        {
            int val = 0;
            m_BinaryParser.Open(m_PerformanceLogName + ".data");
            Debug.Log(val);

            // Headers
            val = m_BinaryParser.ReadInt32(); // 4 bytes : Length of frame data
            val = m_BinaryParser.ReadInt32(); // 4 bytes : Little endian (1) or big endian (0)
            val = m_BinaryParser.ReadInt32(); // 4 bytes : Profiler version

            // Frame data
            val = m_BinaryParser.ReadInt32(); // 4 bytes: frameIndex
            val = m_BinaryParser.ReadInt32(); // 4 bytes : realFrame;
            val = m_BinaryParser.ReadInt32(); // 4 bytes : Total GPU Time In MicroSec

            // Memory Stats
            val = m_BinaryParser.ReadInt32(); // 4 bytes : bytesUsed
            val = m_BinaryParser.ReadInt32(); // 4 bytes : bytesUsedDelta
            val = m_BinaryParser.ReadInt32(); // 4 bytes : textureCount
            val = m_BinaryParser.ReadInt32(); // 4 bytes : textureBytes
            val = m_BinaryParser.ReadInt32(); // 4 bytes : meshCount
            val = m_BinaryParser.ReadInt32(); // 4 bytes : meshBytes
            val = m_BinaryParser.ReadInt32(); // 4 bytes : materialCount
            val = m_BinaryParser.ReadInt32(); // 4 bytes : materialBytes
            val = m_BinaryParser.ReadInt32(); // 4 bytes : animationClipCount
            val = m_BinaryParser.ReadInt32(); // 4 bytes : animationClipBytes
            val = m_BinaryParser.ReadInt32(); // 4 bytes : audioCount
            val = m_BinaryParser.ReadInt32(); // 4 bytes : audioBytes
            val = m_BinaryParser.ReadInt32(); // 4 bytes : assetCount
            val = m_BinaryParser.ReadInt32(); // 4 bytes : sceneObjectCount
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gameObjectCount
            val = m_BinaryParser.ReadInt32(); // 4 bytes : totalObjectsCount

            // Classes
            int classCount = m_BinaryParser.ReadInt32(); //4 bytes : classCount
            for (int i = 0; i < classCount; i++)
            {
                val = m_BinaryParser.ReadInt32(); // 4 bytes : i
                val = m_BinaryParser.ReadInt32(); // 4 bytes : value
                val = m_BinaryParser.ReadInt32(); // 4 bytes : -1 (ff ff ff ff)
            }

            // Memory Allocation
            int memoryAllocatorCount = m_BinaryParser.ReadInt32(); //4 bytes : count of memoryAllocatorInformation
            for (int i = 0; i < memoryAllocatorCount; i++)
            {
                val = m_BinaryParser.ReadInt32(); // 4 bytes : memoryAllocatorInformation[i].used
                val = m_BinaryParser.ReadInt32(); // 4 bytes : memoryAllocatorInformation[i].reserved
            }

            // Physics stats
            val = m_BinaryParser.ReadInt32(); // 4 bytes: activeRigidbodies
            val = m_BinaryParser.ReadInt32(); // 4 bytes: sleepingRigidbodies
            val = m_BinaryParser.ReadInt32(); // 4 bytes: numberOfShapePairs
            val = m_BinaryParser.ReadInt32(); // 4 bytes: numberOfStaticColliders
            val = m_BinaryParser.ReadInt32(); // 4 bytes: numberOfDynamicColliders

            // Debug stats
            val = m_BinaryParser.ReadInt32(); // 4 bytes : m_ProfilerMemoryUsage
            val = m_BinaryParser.ReadInt32(); // 4 bytes : m_ProfilerMemoryUsageOthers
            val = m_BinaryParser.ReadInt32(); // 4 bytes : m_AllocatedProfileSamples

            // Audio Stats
            val = m_BinaryParser.ReadInt32(); // 4 bytes : playingSources
            val = m_BinaryParser.ReadInt32(); // 4 bytes : pausedSources
            val = m_BinaryParser.ReadInt32(); // 4 bytes : audioCPUusage
            val = m_BinaryParser.ReadInt32(); // 4 bytes : audioMemUsage
            val = m_BinaryParser.ReadInt32(); // 4 bytes : audioMaxMemUsage
            val = m_BinaryParser.ReadInt32(); // 4 bytes : audioVoices

            // Chart sample
            val = m_BinaryParser.ReadInt32(); // 4 bytes : rendering
            val = m_BinaryParser.ReadInt32(); // 4 bytes : scripts
            val = m_BinaryParser.ReadInt32(); // 4 bytes : physics
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gc
            val = m_BinaryParser.ReadInt32(); // 4 bytes : vsync
            val = m_BinaryParser.ReadInt32(); // 4 bytes : others
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuOpaque
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuTransparent
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuShadows
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuPostProcess
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuDeferredPrePass
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuDeferredLighting
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuOther
            val = m_BinaryParser.ReadInt32(); // 4 bytes : hasGPUProfiler

            // Chart sample selected
            val = m_BinaryParser.ReadInt32(); // 4 bytes : rendering
            val = m_BinaryParser.ReadInt32(); // 4 bytes : scripts
            val = m_BinaryParser.ReadInt32(); // 4 bytes : physics
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gc
            val = m_BinaryParser.ReadInt32(); // 4 bytes : vsync
            val = m_BinaryParser.ReadInt32(); // 4 bytes : others
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuOpaque
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuTransparent
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuShadows
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuPostProcess
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuDeferredPrePass
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuDeferredLighting
            val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuOther
            val = m_BinaryParser.ReadInt32(); // 4 bytes : hasGPUProfiler

            // GPU Time Samples
            int size = m_BinaryParser.ReadInt32(); //4 bytes : array size
            for (int i = 0; i < size; i++)
            {
                //for each item in array
                val = m_BinaryParser.ReadInt32(); // 4 bytes : relatedSampleIndex
                val = m_BinaryParser.ReadInt32(); // 4 bytes : timerQuery
                val = m_BinaryParser.ReadInt32(); // 4 bytes : gpuTimeInMicroSec
                val = m_BinaryParser.ReadInt32(); // 4 bytes : GpuSection gpuSection
            }

            // AllocatedGCMemorySamples
            size = m_BinaryParser.ReadInt32(); // 4 bytes : array size
            for (int i = 0; i < size; i++)
            {
                //for each item in array
                val = m_BinaryParser.ReadInt32(); // 4 bytes : relatedSampleIndex
                val = m_BinaryParser.ReadInt32(); // 4 bytes : allocatedGCMemory
            }


            // Iteration on some objects
            int tf = m_BinaryParser.ReadInt32(); // 4 bytes : 1 or 0

            if (tf == 1)
            {
                m_BinaryParser.ReadInt32(); // n bytes + (0x00000000) = Null terminated string
                m_BinaryParser.ReadInt32(); // 4 bytes : flags
            }

            // End of Frame
            m_BinaryParser.ReadInt32(); // 4 bytes : End of Frame = AFAFAFAF
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed parsing file " + m_PerformanceLogName + " Error: " + e.Message);
        }
    }*/

    float GetSamplingAverageFps()
    {
        float average = 0;
        foreach (float data in m_AvgFpsTracker)
        {
            average += data;
        }

        if (average == 0)
        {
            average = m_FpsCounter.Fps;
        }

        return average / Mathf.Max(m_AvgFpsTracker.Count, 1);
    }

    float GetTimeDemoAverageFps()
    {
        float average = 0;
        for (int i = 0; i < m_TimeData.Count; i++)
        {
            average += m_TimeData[i].avgfps;
        }

        if (average == 0)
        {
            average = m_FpsCounter.Fps;
        }

        return average / Mathf.Max((float)m_TimeData.Count, 1);
    }

    float GetMaxFps()
    {
        float retVal = m_TimeData[0].curfps;
        for (int i = 1; i < m_TimeData.Count; i++)
        {
            if (retVal < m_TimeData[i].curfps)
                retVal = m_TimeData[i].curfps;
        }

        return retVal;
    }

    float GetMinFps()
    {
        float retVal = m_TimeData[0].curfps;
        for (int i = 1; i < m_TimeData.Count; i++)
        {
            if (retVal > m_TimeData[i].curfps)
                retVal = m_TimeData[i].curfps;
        }

        return retVal;
    }

    string TimeDataAsString()
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < m_TimeData.Count; i++)
        {
            builder.Append(string.Format("{0},{1},{2}|", m_TimeData[i].time, m_TimeData[i].curfps, m_TimeData[i].avgfps));
        }
        builder.Remove(builder.Length - 1, 1);
        return builder.ToString();
    }


    IEnumerator UploadTimeDemoData(string dbUrl, bool exitApplication)
    {
        WWWForm form = new WWWForm();
        form.AddField("project_name", m_ProjectName);
        form.AddField("scene_name", string.IsNullOrEmpty(m_sceneName) ? "Unnamed Scene" : m_sceneName);
        form.AddField("computer_name", m_computerName);
        form.AddField("timedemo_name", m_TimeDemoName);
        form.AddField("timedemo_length", m_TimeDemoLength.ToString("F2"));
        form.AddField("max_fps", GetMaxFps().ToString("F2"));
        form.AddField("min_fps", GetMinFps().ToString("F2"));
        form.AddField("avg_fps", GetTimeDemoAverageFps().ToString("F2"));
        form.AddField("timedata", TimeDataAsString());
        form.AddField("date", m_dateOfDemo.ToString("yyyy-MM-dd HH:mm:ss")); // i.e. 2012-07-06 11:45:26
        form.AddField("screen_width", Screen.width);
        form.AddField("screen_height", Screen.height);
        form.AddField("fullscreen", Screen.fullScreen ? 1 : 0);
        form.AddField("quality_level", QualitySettings.names[QualitySettings.GetQualityLevel()]);
        form.AddField("editor", Application.isEditor ? 1 : 0);
        form.AddField("cpu", SystemInfo.processorType);
        form.AddField("memory", ((int)(SystemInfo.systemMemorySize / 1000.0f)).ToString());
        form.AddField("graphics_card", SystemInfo.graphicsDeviceName);

        WWW www = new WWW(dbUrl, form);

        // when uploading large data chunks at the same time, uncomment this to prevent overloading the db
        //while (!www.isDone)  {}

        yield return www;

        if (www.error != null)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Successfully uploaded timedemo data");
        }

        Debug.Log(www.text);

        if (exitApplication)
        {
            Application.Quit();
        }
    }


    public void UploadTestData()
    {
        // modify these values to customize the data sent to the database
        // this is for uploaded large amounts of dummy test data to populate the database

        // test info that gets uploaded
        m_ProjectName = "TimeDemoTest";
        m_sceneName = "Scene1";
        m_computerName = "machine1";
        m_TimeDemoName = "TimeDemoScene9";
        m_TimeDemoLength = 10;
        m_dateOfDemo = new System.DateTime(2012, 8, 15);

        // fps data that gets uploaded
        float lengthSeconds = 10;
        float interval = 0.1f;
        float fpsMax = 60;
        float fpsMin = 50;
        float fpsDecay = 0.1f;
        float fpsMinSeparation = 10;

        for (int day = 0; day < 30; day++)
        {
            float curTime = 0;
            m_TimeData = new List<TimeData>();

            const int numFpsEntries = 50;
            float[] averageFpsArray = new float[numFpsEntries];
            float averageSum = 0;

            while (curTime <= lengthSeconds)
            {
                float fps = Random.Range(fpsMin, fpsMax);


                if (m_TimeData.Count == 0)
                {
                    for (int init = 0; init < numFpsEntries; init++)
                    {
                        averageFpsArray[init] = fps;
                        averageSum += fps;
                    }
                }

                int i = m_TimeData.Count % numFpsEntries;
                averageSum -= averageFpsArray[i];
                averageFpsArray[i] = fps;
                averageSum += fps;
                float averageFps = averageSum / numFpsEntries;

                m_TimeData.Add(new TimeData(curTime, fps, averageFps));
                curTime += interval;
            }

            StartCoroutine(UploadTimeDemoData(TimeDemoUrl, false));


            // get slower each day
            fpsMax -= fpsDecay;
            fpsMin = fpsMax - fpsMinSeparation;
            m_dateOfDemo = m_dateOfDemo.AddDays(1);
        }
    }

    #endregion
}
