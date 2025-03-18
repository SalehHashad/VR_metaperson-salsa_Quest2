using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.Examples
{
    public class Test_2 : MonoBehaviour
    {
        private GCSpeechRecognition _speechRecognition;
        public Image _speechRecognitionState;
        public Text _resultText;
        public TMP_InputField _InputFieldresult;
        public Image _voiceLevelImage;
        //private Connection connectionManager;
        public GendyTestWebSocket connectionManager;
        public bool canRecord = true;
        private float silenceTimer = 0f;

        public event Action<string> OnSpeechRecognized;

        private bool _voiceDetectionEnabled = true;
        private bool _recognizeDirectly = true;
        private Enumerators.LanguageCode _languageCode = Enumerators.LanguageCode.en_GB;
        private bool isAIResponding = false;

        private void Start()
        {

            InitializeSpeechRecognition();

            connectionManager = FindObjectOfType<GendyTestWebSocket>();
            if (connectionManager == null)
            {
                Debug.LogError("Connection script not found in scene!");
            }
            else
            {
                connectionManager.OnAISpeechComplete += HandleAISpeechComplete;
                connectionManager.OnAIStartSpeaking += HandleAIStartSpeaking;
            }

            StartAutomaticRecording();
        }

        private void HandleAIStartSpeaking()
        {
            Debug.Log("AI started speaking - Stopping recording");
            isAIResponding = true;
            StopRecording();
        }



        private void Update()
        {
            UpdateVoiceLevelIndicator();
        }
        private void InitializeSpeechRecognition()
        {
            _speechRecognition = GCSpeechRecognition.Instance;

            // Add event handlers
            _speechRecognition.RecognizeSuccessEvent += RecognizeSuccessEventHandler;
            _speechRecognition.RecognizeFailedEvent += RecognizeFailedEventHandler;
            _speechRecognition.FinishedRecordEvent += FinishedRecordEventHandler;
            _speechRecognition.StartedRecordEvent += StartedRecordEventHandler;
            _speechRecognition.RecordFailedEvent += RecordFailedEventHandler;
            _speechRecognition.BeginTalkigEvent += BeginTalkigEventHandler;
            _speechRecognition.EndTalkigEvent += EndTalkigEventHandler;

            // Initialize microphone
            _speechRecognition.RequestMicrophonePermission(null);
            SetupDefaultMicrophone();
        }
        private void UpdateVoiceLevelIndicator()
        {
            if (_speechRecognition.GetMaxFrame() > 0)
            {
                float max = (float)_speechRecognition.configs[_speechRecognition.currentConfigIndex].voiceDetectionThreshold;
                float current = _speechRecognition.GetLastFrame() / max;

                if (current < 5f)
                {
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
        private void HandleAISpeechComplete()
        {
            Debug.Log("AI Speech Complete - Resuming Recording");
            isAIResponding = false;
            canRecord = true;
            _InputFieldresult.text = "";
            StartAutomaticRecording();
        }


        public void StopRecording()
        {
            canRecord = false;
            if (_speechRecognition.IsRecording)
            {
                _speechRecognition.StopRecord();
            }
        }




        private void SetupDefaultMicrophone()
        {

            var devices = _speechRecognition.GetMicrophoneDevices();
            if (devices.Length > 0)
            {
                Debug.Log(devices[0]);

                _speechRecognition.SetMicrophoneDevice(devices[0]);
            }
        }
        private void StartAutomaticRecording()
        {
            if (!canRecord || isAIResponding || (connectionManager != null && connectionManager.IsAISpeaking))
            {
                Debug.Log("Cannot start recording: AI is speaking or recording is disabled");
                return;
            }

            if (_speechRecognition.HasConnectedMicrophoneDevices())
            {
                _speechRecognition.StartRecord(_voiceDetectionEnabled);
                Debug.Log("Started Recording");
            }
            else
            {
                Debug.LogError("No microphone device found!");
            }
        }


        private void OnDestroy()
        {
            if (connectionManager != null)
            {
                connectionManager.OnAISpeechComplete -= HandleAISpeechComplete;
                connectionManager.OnAIStartSpeaking -= HandleAIStartSpeaking;  
            }

            // Remove event handlers
            _speechRecognition.RecognizeSuccessEvent -= RecognizeSuccessEventHandler;
            _speechRecognition.RecognizeFailedEvent -= RecognizeFailedEventHandler;
            _speechRecognition.FinishedRecordEvent -= FinishedRecordEventHandler;
            _speechRecognition.StartedRecordEvent -= StartedRecordEventHandler;
            _speechRecognition.RecordFailedEvent -= RecordFailedEventHandler;
            _speechRecognition.BeginTalkigEvent -= BeginTalkigEventHandler;
            _speechRecognition.EndTalkigEvent -= EndTalkigEventHandler;
        }

        private void EndTalkigEventHandler(AudioClip clip, float[] raw)
        {
            if (canRecord && !isAIResponding)
            {
                FinishedRecordEventHandler(clip, raw);
                StartCoroutine(RestartRecording());
            }
        }

        private IEnumerator RestartRecording()
        {
            yield return new WaitForSeconds(0.5f);
            if (canRecord && !isAIResponding)
            {
                StartAutomaticRecording();
            }
        }

        private void FinishedRecordEventHandler(AudioClip clip, float[] raw)
        {
            if (clip == null || !canRecord) return;

            RecognitionConfig config = RecognitionConfig.GetDefault();
            config.languageCode = _languageCode.Parse();
            config.audioChannelCount = clip.channels;

            GeneralRecognitionRequest recognitionRequest = new GeneralRecognitionRequest();
            recognitionRequest.audio = new RecognitionAudioContent()
            {
                content = raw.ToBase64(channels: clip.channels)
            };
            recognitionRequest.config = config;

            _speechRecognition.Recognize(recognitionRequest);
        }

        private void RecognizeSuccessEventHandler(RecognitionResponse recognitionResponse)
        {
            Debug.Log("Recognition Success Handler Called"); 
            if (recognitionResponse != null && recognitionResponse.results.Length > 0)
            {
                string transcription = recognitionResponse.results[0].alternatives[0].transcript;
                Debug.Log($"Transcription received: {transcription}"); 
                _InputFieldresult.text = transcription;

                StopRecording();
                OnSpeechRecognized?.Invoke(transcription);
            }
        }

        private void StartedRecordEventHandler()
        {
            if (_speechRecognitionState != null)
            {
                _speechRecognitionState.color = Color.red;
            }
        }

        private void RecordFailedEventHandler()
        {
            if (canRecord)
            {
                StartCoroutine(RestartRecording());
            }

            if (_speechRecognitionState != null)
            {
                _speechRecognitionState.color = Color.yellow;
            }
        }

        private void BeginTalkigEventHandler() { }

        private void RecognizeFailedEventHandler(string error)
        {
            Debug.LogError($"Recognition failed: {error}");
            if (canRecord)
            {
                StartCoroutine(RestartRecording());
            }
        }
    }
}