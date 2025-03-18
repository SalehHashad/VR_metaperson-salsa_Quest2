using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
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
        this.uri = new Uri($"wss://arabtestingai.org:8765/1{userId}");
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
        try
        {
            if (websocket.State != WebSocketState.Open)
            {
                Debug.Log("WebSocket not open, attempting to reconnect...");
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
        catch (WebSocketException wsEx)
        {
            Debug.LogError($"WebSocket error: {wsEx.Message}");
            throw;  
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in SendMessage: {e.Message}");
            throw;
        }
    }

    public async Task Close()
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
            Debug.Log($"\n[{name}] Disconnected");
        }
    }
    public string GetLastResponse()
    {
        var lastTherapistMessage = conversationHistory
            .FindLast(msg => msg.sender == "therapist");

        return lastTherapistMessage.content;
    }
    private async Task<string> ReceiveMessage()
    {
        try
        {
            var buffer = new byte[8192]; 
            var result = await websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new WebSocketException("Server initiated close");
            }

            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error receiving message: {e.Message}");
            throw;
        }
    }
}
