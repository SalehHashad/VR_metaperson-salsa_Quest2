using System;
using System.Collections;
using UnityEngine;
using TMPro;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;
using FrostweepGames.Plugins.GoogleCloud.SpeechRecognition.Examples;
using System.Threading.Tasks;
using System.Text.Json;

public class GendyTestWebSocket : MonoBehaviour
{
    private int maxReconnectAttempts = 3;
    private float reconnectDelay = 2f;

    private TherapistClient therapistClient;
    public TextMeshProUGUI textResponse;
    public TMP_InputField _InputField;
    public TMP_InputField inputFieldAI;
    private GameObject myObject;
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

        int userId = PlayerPrefs.GetInt("UserID", 1);
        string userName = PlayerPrefs.GetString("UserName", "User");
        therapistClient = new TherapistClient(userId, userName);
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

        try
        {
            await ConnectToServer();
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection failed: {e.Message}");
            isConnected = false;
            isSending = false;
        }

        speechRecognition = FindObjectOfType<Test_2>();
        if (speechRecognition != null)
        {
            speechRecognition.OnSpeechRecognized += HandleNewSpeechRecognized;
        }
        else
        {
            Debug.LogError("Speech Recognition script not found!");
        }
    }

    private async Task ConnectToServer()
    {
        try
        {
            await therapistClient.Connect();
            isConnected = true;
            Debug.Log("Connection established successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect: {e.Message}");
            throw;
        }
    }
    private async Task<bool> TryReconnect()
    {
        for (int attempt = 1; attempt <= maxReconnectAttempts; attempt++)
        {
            Debug.Log($"Reconnection attempt {attempt} of {maxReconnectAttempts}");
            try
            {
                isConnected = false;

                await Task.Delay(TimeSpan.FromSeconds(reconnectDelay));

                int userId = PlayerPrefs.GetInt("UserID", 1);
                string userName = PlayerPrefs.GetString("UserName", "User");
                therapistClient = new TherapistClient(userId, userName);

                await ConnectToServer();

                Debug.Log("Reconnection successful!");
                isConnected = true;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Reconnection attempt {attempt} failed: {e.Message}");
            }
        }

        Debug.LogError("Failed to reconnect after multiple attempts");
        return false;
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
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogError("Attempted to speak empty message");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null in SpeakToClip");
            audioSource = gameObject.AddComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("Failed to create AudioSource");
                return;
            }
        }

        if (Speaker.Instance == null)
        {
            Debug.LogError("RT-Voice Speaker instance is null!");
            return;
        }

        Debug.Log($"Audio Source Status - Enabled: {audioSource.enabled}, Volume: {audioSource.volume}");
        Debug.Log($"RT-Voice Status - Speaking: {Speaker.Instance.isSpeaking}");
        Debug.Log($"Attempting to speak message: '{message}'");

        // Configure audio source
        audioSource.enabled = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        audioSource.volume = 1f;

        this.message = message;

        currentUid = System.Guid.NewGuid().ToString();
        uid = currentUid;

        Debug.Log("Starting to speak message: " + message);
        IsAISpeaking = true;
        OnAIStartSpeaking?.Invoke();

        try
        {
            // Get the voice before speaking
            var voice = Speaker.Instance.VoiceForCulture("en");
            if (voice == null)
            {
                Debug.LogError("No English voice found!");
                return;
            }

            // Speak with the selected voice
            Speaker.Instance.Speak(message, audioSource, voice, true);
            Debug.Log("Speak command sent successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in Speak command: {e.Message}");
            IsAISpeaking = false;
            isProcessingMessage = false;
            return;
        }

        StartCoroutine(MonitorSpeech());
    }

    private IEnumerator MonitorSpeech()
    {
        isSpeaking = true;
        yield return new WaitForSeconds(0.2f);

        // Check if audioSource exists
        if (audioSource == null)
        {
            Debug.LogError("AudioSource is null in MonitorSpeech");
            yield break;
        }

        // Wait until we have a clip
        float maxWaitTime = 5f; // Maximum time to wait for clip
        float waitedTime = 0f;

        while (audioSource.clip == null && waitedTime < maxWaitTime)
        {
            waitedTime += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (audioSource.clip == null)
        {
            Debug.LogError("No audio clip was created");
            isSpeaking = false;
            IsAISpeaking = false;
            isProcessingMessage = false;
            OnAISpeechComplete?.Invoke();
            yield break;
        }

        // Now we can safely monitor the audio playback
        while (audioSource.isPlaying)
        {
            yield return new WaitForSeconds(0.1f); // Check more frequently
        }

        yield return new WaitForSeconds(0.2f);

        Debug.Log("Speech completed");
        isSpeaking = false;
        IsAISpeaking = false;
        isProcessingMessage = false;
        OnAISpeechComplete?.Invoke();
    }


    void Update()
    {
        if (myObject == null)
        {
            myObject = GameObject.Find("Contenttxt");
            if (myObject != null)
            {
                textResponse = myObject.GetComponent<TextMeshProUGUI>();
                Debug.Log("Object found!");
            }
        }
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

        if (therapistClient != null)
        {
            _ = therapistClient.Close();
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
        _ = SendWebSocketMessageAsync(_InputField.text);
    }

    private async Task SendWebSocketMessageAsync(string textval)
    {
        try
        {
            if (!isConnected)
            {
                Debug.LogWarning("Not connected to server. Attempting to reconnect...");
                bool reconnected = await TryReconnect();
                if (!reconnected)
                {
                    throw new Exception("Failed to reconnect to server");
                }
            }

            textResponse.text = "";
            Debug.Log("Sending message to server");

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
            await therapistClient.SendMessage(jsonString);

            var lastMessage = await GetLastTherapistMessage();
            if (!string.IsNullOrEmpty(lastMessage))
            {
                Debug.Log($"Received therapist response: {lastMessage}");
                if (!isProcessingMessage)
                {
                    isProcessingMessage = true;
                    message = lastMessage;
                    SpeakToClip(lastMessage);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending message: {e.Message}");
            isConnected = false;
            isSending = false;

            bool reconnected = await TryReconnect();
            if (reconnected)
            {
                await SendWebSocketMessageAsync(textval);
            }
        }
    }

    private async Task<string> GetLastTherapistMessage()
    {
        try
        {
            return await Task.FromResult(therapistClient.GetLastResponse());
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting last message: {e.Message}");
            return null;
        }
    }

    private async void OnApplicationQuit()
    {
        if (therapistClient != null)
        {
            await therapistClient.Close();
        }
        isConnected = false;
    }
}