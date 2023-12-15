
using EdjCase.ICP.Agent.Agents;
using EdjCase.ICP.Candid.Models;
using EdjCase.ICP.WebSockets;
using System.Threading;
using System;
using UnityEngine;
using EdjCase.ICP.Agent;
using EdjCase.ICP.Candid.Mapping;

public class WebSocketManager : MonoBehaviour
{

    public class AppMessage
    {
        [CandidName("text")]
        public string Text { get; set; }

        [CandidName("timestamp")]
        public ulong Timestamp { get; set; }
    }

    private Principal devCanisterId = Principal.FromText("bkyz2-fmaaa-aaaaa-qaaaq-cai");
    private Uri devGatewayUri = new Uri("ws://localhost:8080");
    private Uri devBoundryNodeUri = new Uri("http://localhost:4943");

    private Principal prodCanisterId = Principal.FromText("bkyz2-fmaaa-aaaaa-qaaaq-cai");
    private Uri prodGatewayUri = new Uri("wss://icwebsocketgateway.app.runonflux.io");

    private IWebSocketAgent<AppMessage> agent;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    async void Start()
    {
        bool development = true;
        Principal canisterId;
        Uri gatewayUri;
        if (development)
        {
            canisterId = devCanisterId;
            gatewayUri = devGatewayUri;
        }
        else
        {
            canisterId = prodCanisterId;
            gatewayUri = prodGatewayUri;
        }
        var builder = new WebSocketBuilder<AppMessage>(canisterId, gatewayUri)
            .OnMessage(this.OnMessage)
            .OnOpen(this.OnOpen)
            .OnError(this.OnError)
            .OnClose(this.OnClose);
        if (development)
        {
            // Set the root key as the dev network key
            SubjectPublicKeyInfo devRootKey = await new HttpAgent(
                httpBoundryNodeUrl: devBoundryNodeUri
            ).GetRootKeyAsync();
            builder = builder.WithRootKey(devRootKey);
        }
        this.agent = await builder.BuildAndConnectAsync(cancellationToken: cancellationTokenSource.Token);
        await this.agent.ReceiveAllAsync(cancellationTokenSource.Token);
    }

    void OnOpen()
    {
        Debug.Log("Open");
    }
    async void OnMessage(AppMessage message)
    {
        Debug.Log("Received Message: "+ message.Text);
        ICTimestamp.Now().NanoSeconds.TryToUInt64(out ulong now);
        var replyMessage = new AppMessage
        {
            Text = "pong",
            Timestamp = now
        };
        await this.agent.SendAsync(replyMessage, cancellationTokenSource.Token);
        Debug.Log("Sent Message: " + replyMessage.Text);
    }
    void OnError(Exception ex)
    {
        Debug.Log("Error: " + ex);
    }
    void OnClose()
    {
        Debug.Log("Close");
    }

    async void OnDestroy()
    {
        cancellationTokenSource.Cancel(); // Cancel any ongoing operations
        if (this.agent != null)
        {
            await this.agent.DisposeAsync();
        }
    }
}