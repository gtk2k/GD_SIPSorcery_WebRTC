using Godot;
using GstreamerLauncher;
using Newtonsoft.Json;
using SIPSorcery.Net;
using System;
using System.Threading;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace GD_SIPSorcery_WebRTC;

internal class DebugSignaling
{
    public event Action OnReady;
    public event Action OnOpen;
    public event Action<SignalingMessage> OnSignalingMessage;
    public event Action<int, string> OnClose;
    public event Action<string> OnError;

    private SynchronizationContext ctx;
    private WebSocketServer wss;
    private WebSocket ws;
    private int port;

    private JsonSerializerSettings settings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    private class SignalingBehaviour : WebSocketBehavior
    {
        public event Action OnSignalingClientConnect;
        public event Action<string> OnSignailngMessage;
        public event Action<int, string> OnSignalingClientDisconnect;
        public event Action<string> OnSignalingClientError;

        protected override void OnOpen()
        {
            GD.PrintS($"DebugSignaling Signaling Client Connect: {ID}");
            OnSignalingClientConnect?.Invoke();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            GD.Print("SignalingBehaviour OnMessage");
            OnSignailngMessage?.Invoke(e.Data);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            OnSignalingClientDisconnect?.Invoke(e.Code, e.Reason);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            OnSignalingClientError?.Invoke(e.Message);
        }
    }

    public DebugSignaling(int port)
    {
        ctx = SynchronizationContext.Current;
        this.port = port;
    }

    public void Connect()
    {
        GD.PrintS($"DebugSignaling Connect");

        ctx = SynchronizationContext.Current;

        try
        {
            wss = new WebSocketServer(port);
            wss.AddWebSocketService<SignalingBehaviour>("/", behaviour =>
            {
                behaviour.OnSignalingClientConnect += () => OnReady?.Invoke();
                behaviour.OnSignailngMessage += Ws_OnMessage;
                behaviour.OnSignalingClientDisconnect += (c, r) => OnClose?.Invoke(c, r);
                behaviour.OnSignalingClientError += (m) => OnError?.Invoke(m);
            });
            wss.Start();
            GD.Print("WebSocketServer Started");
            return;
        }
        catch (Exception ex)
        {
            GD.Print("Could not Start Signaling Server");
        }

        try
        {
            wss = null;
            ws = new WebSocket($"ws://localhost:{port}");
            ws.OnOpen += (s, e) => OnOpen?.Invoke();
            ws.OnMessage += (s, e) => Ws_OnMessage(e.Data);
            ws.OnClose += (s, e) => OnClose?.Invoke(e.Code, e.Reason);
            ws.OnError += (s, e) => OnError?.Invoke(e.Message);
            ws.Connect();
            GD.Print("WebSocketClient Connecting");
        }
        catch (Exception ex)
        {
            throw new SignalingStartException("Could Not Start Signaling");
        }
    }

    public void Close()
    {
        GD.PrintS($"DebugSignaling Close");

        ws?.Close();
        ws = null;
    }

    private void Ws_OnMessage(string data)
    {
        GD.PrintS($"DebugSignaling Ws_OnMessage");

        ctx.Post(_ =>
        {
            var msg = JsonConvert.DeserializeObject<SignalingMessage>(data);
            OnSignalingMessage?.Invoke(msg);
        }, null);
    }

    public void Send(RTCSessionDescriptionInit desc)
    {
        GD.PrintS($"DebugSignaling Send: {desc.type}");

        var msg = new SignalingMessage
        {
            type = desc.type == RTCSdpType.offer ? SignalingMessageType.offer : SignalingMessageType.answer,
            sdp = desc.sdp
        };
        var data = JsonConvert.SerializeObject(msg, settings);
        Send(data);
    }

    public void Send(RTCIceCandidate cand)
    {
        GD.PrintS($"DebugSignaling Send: candidate");

        var msg = new SignalingMessage
        {
            type = SignalingMessageType.candidate,
            candidate = cand.candidate,
            sdpMid = cand.sdpMid,
            sdpMLineIndex = cand.sdpMLineIndex,
        };
        var data = JsonConvert.SerializeObject(msg, settings);
        Send(data);
    }

    public void Send(string data)
    {
        if (wss != null)
        {
            GD.PrintS("SessionCnt", wss.WebSocketServices["/"].Sessions.Count);
            wss.WebSocketServices["/"].Sessions.Broadcast(data);
        }
        else
        {
            GD.Print("DebguSignaling Send()");
            ws.Send(data);
        }
    }
}
