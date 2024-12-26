using System;
using System.Collections;
using UnityEngine;
using NativeWebSocket;
using TMPro;
using Crosstales.RTVoice;
using Crosstales.RTVoice.Model;

public class LocalConnection : MonoBehaviour
{
    WebSocket websocket;
    public TextMeshProUGUI textResponse;
    public TMP_InputField _InputField;
    private GameObject myObject;
    //public GameObject character;
    private Animator characteranimatior;
    private string uid; //Unique id of the speech
    public float letterPause = 0.04f; // Time between each character
    private string message;
    AudioSource audioSource;

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

    async void Start()
    {
        websocket = new WebSocket("ws://69.30.204.3:8765/");

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
        };

        websocket.OnError += (e) =>
        {
            textResponse.text += "Error! " + e + "\n";
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            textResponse.text += "Connection closed!! " + e + "\n";
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            message = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("Received OnMessage! (" + bytes.Length + " bytes) " + message);
            //uid = Speaker.Instance.Speak(message, null, Speaker.Instance.VoiceForCulture("en"));
            //SpeakToClip(message);
            //  text.text += message + "\n";

        };

        InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);
        await websocket.Connect();
    }
    IEnumerator TypeText(string message)

    {
        foreach (char letter in message.ToCharArray())
        {
            textResponse.text += letter;
            yield return new WaitForSeconds(letterPause);
        }
    }
    //public void SpeakToClip(string message)
    //{

    //    audioSource = GetComponent<AudioSource>();
    //    audioSource.enabled = false;

    //    // Use SpeakNative to output directly to an AudioSource
    //    Speaker.Instance.Speak(message, audioSource, Speaker.Instance.VoiceForCulture("en"), false);

    //    // Now audioSource.clip should contain the generated AudioClip after speaking
    //    StartCoroutine(WaitForAudioClip(audioSource));
    //}

    private System.Collections.IEnumerator WaitForAudioClip(AudioSource audioSource)
    {
        // Wait until the audio source has finished playing
        while (audioSource.isPlaying)
        {
            yield return null;
        }

        // At this point, the AudioClip should be available
        AudioClip generatedClip = audioSource.clip;
        Debug.Log("Speech complete. AudioClip generated.");

        // You can now use the generatedClip as needed
        // For example, save it, process it, etc.
    }

    public void StartSpeek()
    {
        audioSource.enabled = true;
    }
    void Update()
    {
        if (myObject == null)
        {
            myObject = GameObject.Find("Contenttxt");
            //character = GameObject.Find("gcharacter");
            if (myObject == null)
            {
                Debug.LogWarning("Object with name 'ObjectName' not found!");
            }
            else
            {
                textResponse = myObject.GetComponent<TextMeshProUGUI>();
                //characteranimatior = character.GetComponent<Animator>();
                Debug.Log("Object with name 'ObjectName' found!");
            }
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }
    private void OnEnable()
    {
        Speaker.Instance.OnSpeakStart += speakStart;
        Speaker.Instance.OnSpeakComplete += speakComplete;
    }
    async void SendWebSocketMessage()
    {
        if (websocket.State == WebSocketState.Open)
        {
            MessageData messageData = new MessageData
            {
                prompt = "A professor going to work first walks 500 m along the campus wall, then enters the campus and goes 100 m perpendicularly to the wall towards his building, after that takes an elevator and mounts 10 m up to his office. The trip takes 10 minutes. Calculate the displacement, the distance between the initial and final points, the average velocity and the average speed.",
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

    public void CallSendWebSocketMessage(TextMeshProUGUI promptvalue)
    {
        string textval = promptvalue.text;
        if (string.IsNullOrEmpty(textval))
        {
            Debug.Log("Input field is null or empty.");
        }
        else
        {
            SendWebSocketMessage(textval);
        }

    }
    private void speakStart(Wrapper wrapper)
    {
        if (wrapper.Uid == uid) //Only write the log message if it's "our" speech
        {
            Debug.Log($"RT-Voice: speak started: {wrapper}");
            // characteranimatior.Play("_Talk_F");
            StopAllCoroutines();
            StartCoroutine(TypeText(message));
        }
    }

    private void speakComplete(Wrapper wrapper)
    {
        if (wrapper.Uid == uid) //Only write the log message if it's "our" speech
        {
            Debug.Log($"RT-Voice: speak completed: {wrapper}");
            //characteranimatior.Play("idel");
        }
    }

    public void StartSendTest()
    {
        SendWebSocketMessage(_InputField.text);
    }
    

    async void SendWebSocketMessage(string textval)
    {
        textResponse.text = "";
        Debug.Log("call SendWebSocketMessage");
        if (websocket.State == WebSocketState.Open)
        {
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
        await websocket.Close();
    }
}
