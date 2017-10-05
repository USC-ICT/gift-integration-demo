using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;

public class DictationRecognizer : MonoBehaviour
{
    public delegate void OnStartRecordingDelegate();
    public event OnStartRecordingDelegate OnStartRecording = delegate { };

    public delegate void OnHypothesisDelegate(string hypothesis);
    public event OnHypothesisDelegate OnHypothesis = delegate { };

    public delegate void OnStopRecordingDelegate(string result, bool success);
    public event OnStopRecordingDelegate OnStopRecording = delegate { };


#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    public UnityEngine.Windows.Speech.DictationRecognizer m_dictationRecognizer;
#endif

    bool m_isRecording = false;

    string m_errorMessage = "This system is not configured properly to use Speech Recognition";


    public bool IsRecording { get { return m_isRecording; } }


    public bool PhraseRecognitionSystemIsSupported
    {
        get
        {
            if (VHUtils.IsWindows10OrGreater())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                return UnityEngine.Windows.Speech.PhraseRecognitionSystem.isSupported;
#endif
            }

            return false;
        }
    }

    public string PhraseRecognitionSystemStatus
    {
        get
        {
            if (VHUtils.IsWindows10OrGreater())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                return UnityEngine.Windows.Speech.PhraseRecognitionSystem.Status.ToString();
#endif
            }

            return "Not Supported.";
        }
    }

    public string DictationRecognizerStatus
    {
        get
        {
            if (VHUtils.IsWindows10OrGreater())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                return m_dictationRecognizer.Status.ToString();
#endif
            }

            return "Not Supported.";
        }
    }

    public float AutoSilenceTimeoutSeconds
    {
        get
        {
            if (VHUtils.IsWindows10OrGreater())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                return m_dictationRecognizer.AutoSilenceTimeoutSeconds;
#endif
            }

            return 0;
        }
        set
        {
            if (VHUtils.IsWindows10OrGreater())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                m_dictationRecognizer.AutoSilenceTimeoutSeconds = value;
#endif
            }
        }
    }


    public float InitialSilenceTimeoutSeconds
    {
        get
        {
            if (VHUtils.IsWindows10OrGreater())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                return m_dictationRecognizer.InitialSilenceTimeoutSeconds;
#endif
            }

            return 0;
        }
        set
        {
            if (VHUtils.IsWindows10OrGreater())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                m_dictationRecognizer.InitialSilenceTimeoutSeconds = value;
#endif
            }
        }
    }


    void Start()
    {
        if (VHUtils.IsWindows10OrGreater())
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            m_dictationRecognizer = new UnityEngine.Windows.Speech.DictationRecognizer();

            m_dictationRecognizer.DictationResult += (text, confidence) =>
            {
                Debug.LogFormat("Dictation result: {0}", text);

                OnStopRecording(text, true);

                StopRecording();
            };

            m_dictationRecognizer.DictationHypothesis += (text) =>
            {
                Debug.LogFormat("Dictation hypothesis: {0}", text);

                OnHypothesis(text);
            };

            m_dictationRecognizer.DictationComplete += (completionCause) =>
            {
                if (completionCause != UnityEngine.Windows.Speech.DictationCompletionCause.Complete)
                {
                    Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", completionCause);

                    OnStopRecording(completionCause.ToString(), false);
                }
            };

            m_dictationRecognizer.DictationError += (error, hresult) =>
            {
                string errorString = string.Format("Dictation error: {0}; HResult = {1}.", error, hresult);

                Debug.LogErrorFormat(errorString);

                OnStopRecording(errorString, false);
            };

            m_dictationRecognizer.InitialSilenceTimeoutSeconds = 999;
#endif
        }
    }


    void Update()
    {
    }

    public void StartRecording()
    {
        if (VHUtils.IsWindows10OrGreater())
        {
            if (!m_isRecording)
            {
                m_isRecording = true;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                m_dictationRecognizer.Start();
#endif

                OnStartRecording();
            }
        }
        else
        {
            OnStopRecording(m_errorMessage, false);
        }
    }

    public void StopRecording()
    {
        if (VHUtils.IsWindows10OrGreater())
        {
            if (m_isRecording)
            {
                m_isRecording = false;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                m_dictationRecognizer.Stop();
#endif
            }
        }
    }

#if false
    void OnGUIASR()
    {
        if (VHUtils.IsWindows10OrGreater())
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            GUILayout.Label(string.Format("PhraseRecogntionSystem.isSupported: {0}", UnityEngine.Windows.Speech.PhraseRecognitionSystem.isSupported));
            GUILayout.Label(string.Format("PhraseRecogntionSystem.status: {0}", UnityEngine.Windows.Speech.PhraseRecognitionSystem.Status));
            if (GUILayout.Button("PhraseRecogntionSystem.Restart()"))
            {
                UnityEngine.Windows.Speech.PhraseRecognitionSystem.Restart();
            }

            if (GUILayout.Button("PhraseRecogntionSystem.Stop()"))
            {
                UnityEngine.Windows.Speech.PhraseRecognitionSystem.Shutdown();
            }

            GUILayout.Label(string.Format("Dictation.status: {0}", m_dictationRecognizer.Status));
            GUILayout.Label(string.Format("Dictation.AutoSilenceTimeoutSeconds: {0}", m_dictationRecognizer.AutoSilenceTimeoutSeconds));
            GUILayout.Label(string.Format("Dictation.InitialSilenceTimeoutSeconds: {0}", m_dictationRecognizer.InitialSilenceTimeoutSeconds));

            if (GUILayout.Button("Dictation Recognizer Setup"))
            {
                m_dictationRecognizer.DictationResult += (text, confidence) =>
                {
                    Debug.LogFormat("Dictation result: {0}", text);
                };

                m_dictationRecognizer.DictationHypothesis += (text) =>
                {
                    Debug.LogFormat("Dictation hypothesis: {0}", text);
                };

                m_dictationRecognizer.DictationComplete += (completionCause) =>
                {
                    if (completionCause != UnityEngine.Windows.Speech.DictationCompletionCause.Complete)
                        Debug.LogErrorFormat("Dictation completed unsuccessfully: {0}.", completionCause);
                };

                m_dictationRecognizer.DictationError += (error, hresult) =>
                {
                    Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
                };

                m_dictationRecognizer.Start();
            }

            if (GUILayout.Button("Dictation Recognizer Stop"))
            {
                m_dictationRecognizer.Stop();
            }

            if (GUILayout.Button("Dictation Recognizer Dispose"))
            {
                m_dictationRecognizer.Dispose();
            }

            GUILayout.Space(10);
#endif
        }
    }
#endif
}
