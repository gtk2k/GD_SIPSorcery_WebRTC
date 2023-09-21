using Godot;
using GstreamerLauncher;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Encoders;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GD_SIPSorcery_WebRTC;

internal partial class SIPSorceryPeer : MeshInstance3D
{
    [Export]
    private uint StreamingFrameRate = 30;
    [Export]
    private int StreamingWidth = 640;
    [Export]
    private int StreamingHeight = 360;
    [Export]
    private int SignalingPort = 8991;

    [Export]
    private MeshInstance3D Display;
    [Export]
    private Shader RGBShader;
    [Export]
    private Shader BGRShader;

    private RTCPeerConnection pc;
    private VideoEncoderEndPoint videoSink;
    private DebugSignaling signaling;
    private ImageTexture tex;
    private VideoPixelFormatsEnum prevPixelFormat = VideoPixelFormatsEnum.Rgb;

    private RTCConfiguration config = new RTCConfiguration
    {
        iceServers = new List<RTCIceServer> { new RTCIceServer { urls = "stun:stun.l.google.com:19302" } }
    };

    public override void _Ready()
    {
        GD.Print("SIPSorceryPeer _Ready");

        try
        {
            tex = new ImageTexture();
            ((ShaderMaterial)Display.GetSurfaceOverrideMaterial(0)).SetShaderParameter("_tex", tex);

            ConnectSignaling();
        }
        catch (SignalingStartException e)
        {
            GD.PrintErr(e.Message);
            return;
        }
    }

    private double fpsPeriod = 0;

    public override void _Process(double delta)
    {
        if (videoSink == null) return;

        //if ((delta - fpsPeriod) < ((double)1 / (double)StreamingFrameRate)) return;
        //fpsPeriod = delta;

        var tex = GetViewport().GetTexture();
        var w = tex.GetWidth();
        var h = tex.GetHeight();
        var img = tex.GetImage();
        var data = img.GetData();
        var fmt = img.GetFormat();
        var vpFmt = VideoPixelFormatsEnum.Rgb;
        switch (fmt)
        {
            case Image.Format.Rgb8:
                vpFmt = VideoPixelFormatsEnum.Rgb;
                break;
            case Image.Format.Rgba8:
                vpFmt = VideoPixelFormatsEnum.Bgra;
                break;
        }
        videoSink.ExternalVideoSourceRawSample(StreamingFrameRate, w, h, data, vpFmt);
    }


    private void ConnectSignaling()
    {
        GD.Print("SIPSorceryPeer ConnectSignaling");

        signaling = new DebugSignaling(SignalingPort);
        signaling.OnReady += Signaling_OnReady;
        signaling.OnOpen += () => GD.Print("WS Open");
        signaling.OnSignalingMessage += (msg) => { _ = ReceiveSignalingMessage(msg); };
        signaling.OnClose += (c, r) => GD.PrintS("WS Close", c, r);
        signaling.OnError += (msg) => GD.PrintS("WS Error:", msg);
        signaling.Connect();
    }

    private void Signaling_OnReady()
    {
        GD.Print("SIPSorceryPeer OnReady");

        CreatePC();
        _ = CreateOffer();
    }

    private async Task ReceiveSignalingMessage(SignalingMessage msg)
    {
        GD.Print("SIPSorceryPeer ReceiveSignalingMessage");

        if (pc == null)
        {
            CreatePC();
        }

        if (msg.type == SignalingMessageType.candidate)
        {
            var candidate = new RTCIceCandidateInit
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = (ushort)msg.sdpMLineIndex.Value
            };
            GD.Print("AddIceCandidate");
            pc.addIceCandidate(candidate);
        }
        else
        {
            var desc = new RTCSessionDescriptionInit
            {
                type = msg.type == SignalingMessageType.offer ? RTCSdpType.offer : RTCSdpType.answer,
                sdp = msg.sdp
            };
            GD.Print($"Set Remote: {desc.type}");
            var res = pc.setRemoteDescription(desc);
            if (res != SetDescriptionResultEnum.OK)
            {
                GD.PrintErr($"Set Remote {desc.type} Error");
                return;
            }
            if (desc.type == RTCSdpType.offer)
            {
                GD.Print($"Create Answer");
                var answer = pc.createAnswer(null);
                GD.PrintS("Answer", answer.sdp);
                await pc.setLocalDescription(answer).ConfigureAwait(continueOnCapturedContext: false); ;
                signaling.Send(answer);
            }
        }
    }

    private void CreatePC()
    {
        GD.Print("SIPSorceryPeer CreatePC");

        videoSink = new VideoEncoderEndPoint();
        var videoTrack = new MediaStreamTrack(videoSink.GetVideoSourceFormats(), MediaStreamStatusEnum.SendRecv);
        videoSink.OnVideoSinkDecodedSample += VideoSink_OnVideoSinkDecodedSample;

        pc = new RTCPeerConnection(config);
        pc.addTrack(videoTrack);
        pc.onicegatheringstatechange += Pc_onicegatheringstatechange;
        pc.onconnectionstatechange += Pc_onconnectionstatechange;
        pc.onicecandidate += Pc_onicecandidate;
        pc.OnTimeout += (mediaType) => GD.PrintErr($"Timeout on media {mediaType}.");
        pc.OnVideoFormatsNegotiated += (sdpFormat) => videoSink.SetVideoSourceFormat(sdpFormat.First());
        pc.OnVideoFrameReceived += videoSink.GotVideoFrame;
        videoSink.OnVideoSourceEncodedSample += pc.SendVideo;
    }

    private async Task CreateOffer()
    {
        GD.Print("SIPSorceryPeer CreateOffer");

        var offer = pc.createOffer(null);
        GD.PrintS("offer", offer.sdp);
        await pc.setLocalDescription(offer).ConfigureAwait(continueOnCapturedContext: false);
        signaling.Send(offer);
    }

    private void VideoSink_OnVideoSinkDecodedSample(byte[] sample, uint width, uint height, int stride, VideoPixelFormatsEnum pixelFormat)
    {
        //GD.PrintS("SIPSorceryPeer", "OnVideoSinkDecodedSample", pixelFormat);

        Image.Format format = Image.Format.Rgba8;

        switch (pixelFormat)
        {
            case VideoPixelFormatsEnum.Rgb:
            case VideoPixelFormatsEnum.Bgr:
                format = Image.Format.Rgb8;
                break;
        }
        var img = Image.Create((int)width, (int)height, false, Image.Format.Rgba8);
        img.SetData((int)width, (int)height, false, format, sample);
        tex.SetImage(img);

        if (prevPixelFormat != pixelFormat)
        {
            var mat = (ShaderMaterial)Display.GetSurfaceOverrideMaterial(0);
            if (pixelFormat == VideoPixelFormatsEnum.Bgr)
            {
                mat.Shader = BGRShader;
            }
            else
            {
                mat.Shader = RGBShader;
            }
            mat.SetShaderParameter("_tex", tex);
            prevPixelFormat = pixelFormat;
        }
    }

    private void Pc_onicecandidate(RTCIceCandidate cand)
    {
        GD.PrintS("Cand:", cand.candidate);
        signaling.Send(cand);
    }

    private void Pc_onconnectionstatechange(RTCPeerConnectionState state)
    {
        GD.PrintS("ConnectionStateChange", state);
    }

    private void Pc_onicegatheringstatechange(RTCIceGatheringState state)
    {
        GD.PrintS("IceGatheringStateChange", state);
    }
}
