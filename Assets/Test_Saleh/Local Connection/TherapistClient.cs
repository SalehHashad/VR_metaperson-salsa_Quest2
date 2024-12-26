using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;


public class TherapistClient
{
    private int userId;
    private string name;
    private Uri uri;
    private ClientWebSocket websocket;
    private List<(string sender, string content)> conversationHistory;

    public TherapistClient(int userId, string name)
    {
        this.userId = userId;
        this.name = name;
        this.uri = new Uri($"ws://69.30.204.3:8765/{userId}");
        this.websocket = new ClientWebSocket();
        this.conversationHistory = new List<(string sender, string content)>();
    }

    public async Task Connect()
    {
        await websocket.ConnectAsync(uri, CancellationToken.None);
        string response = await ReceiveMessage();
        var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

        Debug.Log($"\n[{name}] Connected to server");
        Debug.Log($"Server: {responseData["content"]}");

        conversationHistory.Add(("server", responseData["content"]));
    }

    public async Task SendMessage(string message)
    {
        if (websocket.State != WebSocketState.Open)
        {
            await Connect();
        }

        string messageJson = JsonConvert.SerializeObject(new { content = message });

        byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);
        await websocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

        Debug.Log($"\n[{name}]: {message}");
        conversationHistory.Add(("user", message));

        string response = await ReceiveMessage();
        var responseData = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

        Debug.Log($"Therapist: {responseData["content"]}");
        conversationHistory.Add(("therapist", responseData["content"]));
    }

    public async Task Close()
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
            Debug.Log($"\n[{name}] Disconnected");
        }
    }

    private async Task<string> ReceiveMessage()
    {
        var buffer = new byte[1024];
        var result = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }
}
