using System;
using System.Collections;
using UnityEngine;
using NativeWebSocket;
using TMPro;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.Examples;
using System.Threading.Tasks;

public class Connection : MonoBehaviour
{
    WebSocket websocket;
    public TextMeshProUGUI textResponse;
    public TMP_InputField _InputField;
    public TMP_InputField inputFieldAI;
    private GameObject myObject;
    private Animator characteranimatior;
    private string uid;
    public float letterPause = 0.04f;
    private string message;
    AudioSource audioSource;
    private Test_2 speechRecognition;
    private bool isFirstMessage = true;
    private bool isSpeaking = false;
    private string currentUid = string.Empty;

    public event Action OnAISpeechComplete;
    private bool isConnected = false;
    private bool isProcessingMessage = false;
    private bool isSending = false;
    private bool isAudioReady = false;

    public bool IsAISpeaking { get; private set; }
    public event Action OnAIStartSpeaking;

    [Serializable]
    public class MessageData
    {
        public string prompt;
        public string targetLang;
        public int classification;
        public string store_type;
        public string prompt_template;
        public string system_prompt;
        public int exams;
        public string general_knowledge_base;
        public string data;
        public string file_name;
        public string web_mob;
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        VerifyAudioSetup();
    }

    private void VerifyAudioSetup()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.enabled = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        Debug.Log($"Audio Source Status - Enabled: {audioSource.enabled}, Volume: {audioSource.volume}");

        if (Speaker.Instance != null)
        {
            Debug.Log($"RT-Voice Status - Speaking: {Speaker.Instance.isSpeaking}");
        }
        else
        {
            Debug.LogError("RT-Voice Speaker instance is null!");
        }
    }

    async void Start()
    {
        Debug.Log($"RT-Voice initialized: {Speaker.Instance != null}");
        Debug.Log($"AudioSource initialized: {audioSource != null}");
        //Debug.Log($"Current voice provider: {Speaker.Instance?.CurrentVoiceProvider}");

        if (websocket != null) return;

        websocket = new WebSocket("wss://arabtestingai.org");

        websocket.OnOpen += () =>
        {
            isConnected = true;
            Debug.Log("Connection open!");
        };

        websocket.OnError += (e) =>
        {
            isConnected = false;
            isSending = false;
            if (textResponse != null)
                textResponse.text += "Error! " + e + "\n";
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            isConnected = false;
            isSending = false;
            if (textResponse != null)
                textResponse.text += "Connection closed!! " + e + "\n";
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            if (bytes != null && bytes.Length > 0)
            {
                string receivedMessage = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log($"Received OnMessage! ({bytes.Length} bytes) {receivedMessage}");

                if (isFirstMessage)
                {
                    isFirstMessage = false;
                    isProcessingMessage = false;
                    isSending = false;
                    SendWebSocketMessageAsync(_InputField.text);
                    return;
                }

                if (!isProcessingMessage && !string.IsNullOrEmpty(receivedMessage))
                {
                    isProcessingMessage = true;
                    isSending = false;
                    message = receivedMessage;

                    if (!isAudioReady)
                    {
                        if (audioSource != null)
                        {
                            audioSource.enabled = true;
                            isAudioReady = true;
                            Debug.Log("Audio source enabled in OnMessage");
                        }
                    }

                    SpeakToClip(receivedMessage);
                }
            }
        };

        speechRecognition = FindObjectOfType<Test_2>();
        if (speechRecognition != null)
        {
            speechRecognition.OnSpeechRecognized += HandleNewSpeechRecognized;
        }
        else
        {
            Debug.LogError("Speech Recognition script not found!");
        }

        try
        {
            await websocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"WebSocket connection failed: {e.Message}");
            isConnected = false;
            isSending = false;
        }
    }

    private async void HandleNewSpeechRecognized(string text)
    {
        if (!isSending)
        {
            await StartSendTestAsync();
        }
    }

    IEnumerator TypeText(string message)
    {
        foreach (char letter in message.ToCharArray())
        {
            textResponse.text += letter;
            yield return new WaitForSeconds(letterPause);
        }
    }

    public void SpeakToClip(string message)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null in SpeakToClip");
            return;
        }

        audioSource.enabled = true;
        audioSource.playOnAwake = false;
        this.message = message;

        currentUid = System.Guid.NewGuid().ToString();
        uid = currentUid;

        Debug.Log("Starting to speak message: " + message);
        IsAISpeaking = true;
        OnAIStartSpeaking?.Invoke(); 
        Speaker.Instance.Speak(message, audioSource, Speaker.Instance.VoiceForCulture("en"), true);

        StartCoroutine(MonitorSpeech());
    }

    private IEnumerator MonitorSpeech()
    {
        isSpeaking = true;

        yield return new WaitForSeconds(0.2f);

        //while (Speaker.Instance.isSpeaking)
        //{
        //    Debug.Log("Currently speaking...");
        //    yield return new WaitForSeconds(0.1f);
        //}
        while (audioSource.isPlaying)
        {
            yield return new WaitForSeconds(audioSource.clip.length);

        }

        yield return new WaitForSeconds(0.2f);

        Debug.Log("Speech completed");
        isSpeaking = false;
        IsAISpeaking = false;  
        isProcessingMessage = false;
        yield return new WaitForSeconds(audioSource.clip.length);
        OnAISpeechComplete?.Invoke();
    }

    public void StartSpeek()
    {
        if (audioSource != null)
        {
            audioSource.enabled = true;
            Debug.Log("Audio source enabled");
        }
        else
        {
            Debug.LogError("AudioSource is null in StartSpeek");
        }
    }

    void Update()
    {
        if (myObject == null)
        {
            myObject = GameObject.Find("Contenttxt");
            if (myObject == null)
            {
                Debug.LogWarning("Object with name 'Contenttxt' not found!");
            }
            else
            {
                textResponse = myObject.GetComponent<TextMeshProUGUI>();
                Debug.Log("Object found!");
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    private void OnEnable()
    {
        Speaker.Instance.OnSpeakStart += HandleSpeakStart;
        Speaker.Instance.OnSpeakComplete += HandleSpeakComplete;
    }

    private void OnDisable()
    {
        Speaker.Instance.OnSpeakStart -= HandleSpeakStart;
        Speaker.Instance.OnSpeakComplete -= HandleSpeakComplete;
    }

    private void OnDestroy()
    {
        if (speechRecognition != null)
        {
            speechRecognition.OnSpeechRecognized -= HandleNewSpeechRecognized;
        }
    }

    private void HandleSpeakStart(Wrapper wrapper)
    {
        if (wrapper.Uid == currentUid)
        {
            Debug.Log($"Speech started: {wrapper.Text}");
            StopAllCoroutines();
            StartCoroutine(TypeText(message));
        }
    }

    private void HandleSpeakComplete(Wrapper wrapper)
    {
        if (wrapper.Uid == currentUid)
        {
            Debug.Log($"Speech completed: {wrapper.Text}");
            isProcessingMessage = false;
        }
    }

    private async Task StartSendTestAsync()
    {
        if (!string.IsNullOrEmpty(_InputField.text))
        {
            await SendWebSocketMessageAsync(_InputField.text);
        }
    }

    public void StartSendTest()
    {
        SendWebSocketMessageAsync(_InputField.text);
    }

    private async Task SendWebSocketMessageAsync(string textval)
    {
        textResponse.text = "";
        Debug.Log("call SendWebSocketMessage");
        if (websocket.State == WebSocketState.Open)
        {
            if (audioSource != null)
            {
                audioSource.enabled = true;
                isAudioReady = true;
                Debug.Log("Audio source enabled before sending message");
            }

            MessageData messageData = new MessageData
            {
                prompt = textval,
                targetLang = "en",
                classification = 24,
                store_type = PlayerPrefs.GetString("subject"),
                prompt_template = PlayerPrefs.GetString("level"),
                system_prompt = "adhd_app_sentence_chunks_emb",
                exams = PlayerPrefs.GetInt("Academicyear"),
                general_knowledge_base = "yes",
                data = "",
                file_name = "",
                web_mob = "\n"
            };

            string jsonString = JsonUtility.ToJson(messageData);
            await websocket.SendText(jsonString);
            CancelInvoke("SendWebSocketMessage");
        }
    }

    private async void OnApplicationQuit()
    {
        isConnected = false;
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}