using System;
using System.Collections;
using System.Text;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

public class TherapistNativeClient : MonoBehaviour
{
    // WebSocket configuration
    private WebSocket websocket;
    private string sessionToken = "bf4be0ef-d893-482e-84a3-9fc7ca21d2a8"; // Replace with your actual token
    private string serverUrl = "wss://arabtestingai.org:8765/";
    private bool isConnected = false;

    // Message classes
    [Serializable]
    private class RequestMessage
    {
        public string content;
    }

    [Serializable]
    private class ResponseMessage
    {
        public string type;
        public string content;
    }

    // Predefined messages to test
    private string[] predefinedMessages = new string[]
    {
        "how do u know my name",
        "what can you help me with",
        "tell me about yourself"
    };

    private int currentMessageIndex = 0;

    void Start()
    {
        // Connect to WebSocket server
        ConnectToServer();
    }

    async void ConnectToServer()
    {
        try
        {
            Debug.Log($"Connecting to {serverUrl}{sessionToken}...");

            // Create WebSocket with session token in URL
            websocket = new WebSocket(serverUrl + sessionToken);

            // Set up event handlers
            websocket.OnOpen += () =>
            {
                Debug.Log("Connection open!");
                isConnected = true;

                // Send first message
                SendNextMessage();
            };

            websocket.OnError += (e) =>
            {
                Debug.LogError($"Error! {e}");
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log($"Connection closed! Code: {e}");
                isConnected = false;
            };

            websocket.OnMessage += (bytes) =>
            {
                // Convert bytes to string
                string message = Encoding.UTF8.GetString(bytes);
                Debug.Log($"Received raw message: {message}");

                try
                {
                    // Parse response
                    ResponseMessage response = JsonConvert.DeserializeObject<ResponseMessage>(message);

                    // Just log the content of the response
                    Debug.Log($"Response content: {response.content}");

                    // Send next message after delay
                    StartCoroutine(SendNextAfterDelay(3.0f));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing response: {e.Message}");
                }
            };

            // Connect to the server
            await websocket.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection error: {e.Message}");
        }
    }

    private IEnumerator SendNextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SendNextMessage();
    }

    private void SendNextMessage()
    {
        if (!isConnected || currentMessageIndex >= predefinedMessages.Length)
        {
            return;
        }

        // Get current message
        string messageContent = predefinedMessages[currentMessageIndex];
        currentMessageIndex++;

        // Send the message
        SendMessage(messageContent);
    }

    private void SendMessage(string content)
    {
        try
        {
            RequestMessage request = new RequestMessage
            {
                content = content
            };

            string json = JsonConvert.SerializeObject(request);
            Debug.Log($"Sending: {json}");

            // Send message
            websocket.SendText(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending message: {e.Message}");
        }
    }

    // For clean up
    private async void OnDestroy()
    {
        if (websocket != null && isConnected)
        {
            await websocket.Close();
        }
    }

    // Keep the connection alive
    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
        {
            websocket.DispatchMessageQueue();
        }
#endif
    }
}