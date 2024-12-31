using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.MemoryProfiler;
using UnityEngine;
using UnityEngine.UI;
namespace FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.Examples

{
    public class TestMicThreshold : MonoBehaviour
    {
        private GCSpeechRecognition _speechRecognition;
        public Image _speechRecognitionState;
        public Text _resultText;
        public TMP_InputField _InputFieldresult;
        public Button askai;
        public Image _voiceLevelImage;

        // New variables to handle automatic recording
        private bool isListening = false;
        private float silenceTimer = 0f;
        private const float SILENCE_THRESHOLD = 2f; // Time in seconds to wait before processing speech
        private Connection _aiConnection;


        private bool _isWaitingForAI = false;
        private void Awake()
        {
            // connection = FindObjectOfType<Connection>();
        }

        private void Start()
        {
            InitializeSpeechRecognition();
            StartAutomaticRecording();
        }

        private void SetupAIConnection()
        {
            _aiConnection = FindObjectOfType<Connection>();
            if (_aiConnection == null)
            {
                Debug.LogError("Connection script not found in the scene!");
            }


        }
        private void InitializeSpeechRecognition()
        {
            _speechRecognition = GCSpeechRecognition.Instance;

            // Set up event handlers
            _speechRecognition.RecognizeSuccessEvent += RecognizeSuccessEventHandler;
            // _speechRecognition.RecognizeFailedEvent += RecognizeFailedEventHandler;
            // _speechRecognition.LongRunningRecognizeSuccessEvent += LongRunningRecognizeSuccessEventHandler;
            // _speechRecognition.LongRunningRecognizeFailedEvent += LongRunningRecognizeFailedEventHandler;
            _speechRecognition.FinishedRecordEvent += FinishedRecordEventHandler;
            _speechRecognition.StartedRecordEvent += StartedRecordEventHandler;
            _speechRecognition.RecordFailedEvent += RecordFailedEventHandler;
            _speechRecognition.BeginTalkigEvent += BeginTalkigEventHandler;
            _speechRecognition.EndTalkigEvent += EndTalkigEventHandler;

            // Initialize microphone
            _speechRecognition.RequestMicrophonePermission(null);
            if (_speechRecognition.HasConnectedMicrophoneDevices())
            {
                _speechRecognition.SetMicrophoneDevice(_speechRecognition.GetMicrophoneDevices()[0]);
            }
        }

        private void StartAutomaticRecording()
        {
            isListening = true;
            _speechRecognition.StartRecord(true); // true enables voice detection
        }

        private void Update()
        {
            if (_speechRecognition.IsRecording)
            {
                UpdateVoiceLevelIndicator();
                HandleSilenceDetection();
            }
        }

        private void UpdateVoiceLevelIndicator()
        {
            if (_speechRecognition.GetMaxFrame() > 0)
            {
                float max = (float)_speechRecognition.configs[_speechRecognition.currentConfigIndex].voiceDetectionThreshold;
                float current = _speechRecognition.GetLastFrame() / max;

                // If sound is below threshold, increment silence timer
                if (current < 5f)
                {
                    //Debug.Log("Your voice Threshold is " + current);
                    silenceTimer += Time.deltaTime;
                }
                else
                {
                    silenceTimer = 0f;
                }

                if (_voiceLevelImage != null)
                {
                    if (current >= 1f)
                    {
                        _voiceLevelImage.fillAmount = Mathf.Lerp(_voiceLevelImage.fillAmount, Mathf.Clamp(current / 2f, 0, 1f), 30 * Time.deltaTime);
                    }
                    else
                    {
                        _voiceLevelImage.fillAmount = Mathf.Lerp(_voiceLevelImage.fillAmount, Mathf.Clamp(current / 2f, 0, 0.5f), 30 * Time.deltaTime);
                    }
                    _voiceLevelImage.color = current >= 1f ? Color.green : Color.red;
                }
            }
        }

        private void HandleSilenceDetection()
        {
            // If silence has been detected for long enough, process the recording
            if (silenceTimer >= SILENCE_THRESHOLD)
            {
                ProcessCurrentRecording();
            }
        }

        private void ProcessCurrentRecording()
        {
            if (_speechRecognition.LastRecordedClip != null)
            {
                RecognitionConfig config = RecognitionConfig.GetDefault();
                config.languageCode = Enumerators.LanguageCode.en_GB.Parse();
                config.audioChannelCount = _speechRecognition.LastRecordedClip.channels;

                GeneralRecognitionRequest recognitionRequest = new GeneralRecognitionRequest();
                recognitionRequest.audio = new RecognitionAudioContent()
                {
                    content = _speechRecognition.LastRecordedRaw.ToBase64(channels: _speechRecognition.LastRecordedClip.channels)
                };
                recognitionRequest.config = config;

                _speechRecognition.Recognize(recognitionRequest);
            }

            // Restart recording for the next utterance
            RestartRecording();
        }

        private void RestartRecording()
        {
            if (isListening)
            {
                _speechRecognition.StopRecord();
                StartCoroutine(RestartRecordingCoroutine());
            }
        }

        private IEnumerator RestartRecordingCoroutine()
        {
            yield return new WaitForSeconds(0.5f); // Short delay to ensure clean recording restart
            if (isListening)
            {
                _speechRecognition.StartRecord(true);
            }
            silenceTimer = 0f;
        }

        private void OnDestroy()
        {
            // Clean up event handlers
            _speechRecognition.RecognizeSuccessEvent -= RecognizeSuccessEventHandler;
            // _speechRecognition.RecognizeFailedEvent -= RecognizeFailedEventHandler;
            // _speechRecognition.LongRunningRecognizeSuccessEvent -= LongRunningRecognizeSuccessEventHandler;
            // _speechRecognition.LongRunningRecognizeFailedEvent -= LongRunningRecognizeFailedEventHandler;
            _speechRecognition.FinishedRecordEvent -= FinishedRecordEventHandler;
            _speechRecognition.StartedRecordEvent -= StartedRecordEventHandler;
            _speechRecognition.RecordFailedEvent -= RecordFailedEventHandler;
            _speechRecognition.BeginTalkigEvent -= BeginTalkigEventHandler;
            _speechRecognition.EndTalkigEvent -= EndTalkigEventHandler;
        }

        // Event handlers remain largely the same but simplified for automatic operation
        private void RecognizeSuccessEventHandler(RecognitionResponse recognitionResponse)
        {
            if (recognitionResponse != null && recognitionResponse.results.Length > 0)
            {
                string transcript = recognitionResponse.results[0].alternatives[0].transcript;
                Debug.LogError(transcript);
                _InputFieldresult.text = transcript;
                _resultText.text = transcript;
                //connection.SendAndSpeak();

                //if (askai != null)
                //{
                //    askai.interactable = true;
                //}
            }
        }

        // Implement other necessary event handlers...
        private void StartedRecordEventHandler()
        {
            if (_speechRecognitionState != null)
            {
                _speechRecognitionState.color = Color.red;
            }
        }

        private void RecordFailedEventHandler()
        {
            if (_speechRecognitionState != null)
            {
                _speechRecognitionState.color = Color.yellow;
            }
            Debug.LogError("Recording failed. Please check microphone device and try again.");
            isListening = false;
        }

        private void BeginTalkigEventHandler()
        {
            silenceTimer = 0f;
        }

        private void EndTalkigEventHandler(AudioClip clip, float[] raw)
        {
            ProcessCurrentRecording();
        }

        private void FinishedRecordEventHandler(AudioClip clip, float[] raw)
        {
            // This will be handled by ProcessCurrentRecording
        }

        // Implement other event handlers as needed...
    }
}