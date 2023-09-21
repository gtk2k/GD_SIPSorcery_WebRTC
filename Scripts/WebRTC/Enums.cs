namespace GstreamerLauncher
{
    public enum PeerType
    {
        Gamepad,
        CloudRendering
    }

    public enum ClientType
    {
        web,
        app,
        gstreamer
    }

    public enum IceTransportPolicy
    {
        Relay = 1,
        All = 3
    }

    public enum SignalingMessageType
    {
        connect,
        offer,
        answer,
        candidate,
        log,
        ping,
        pong,
        notify,
        config,
        requestStart,
        requestSora,
        requestLog
    }

    public enum LogLevel
    {
        None = 0,
        Log = 1,
        Warning = 2,
        Error = 3
    }

    public enum LogType
    {
        None,
        Log,
        Warning,
        Error
    }

    public enum LogCategory
    {
        None = 0,
        Manager = 1,
        Signaling = 2,
        Peer = 3,
        Stream = 4,
        DataChannel = 5,
        SoraSignaling = 6,
        Coinfig = 7,
        VGamepad = 8,
        Http = 9,
        Gst = 10
    }

    public enum CloudRenderingType
    {
        p2p,
        sora,
        gstreamer
    }

    public enum CodecType
    {
        // Video
        None,
        VP8,
        VP9,
        H264,
        AV1,

        // Audio
        OPUS,
        MULTIOPUS,
        ILBC,
        ISAC,
        G722,
        PCMU,
        PCMA,
        L16,
        CN,
        telephone_event
    }

    public enum SignalingType
    {
        p2p,
        sora
    }

    public enum SoraRole
    {
        sendonly,
        recvonly,
        sendreceive
    }

    public enum SoraTurnTransportPolicy
    {
        udp
    }

    public enum FrameDataType
    {
        None = 0,
        Video = 1,
        Audio = 2
    }
}