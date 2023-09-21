using System;

namespace GD_SIPSorcery_WebRTC;

public class SignalingStartException : Exception
{
    public SignalingStartException(string message) : base(message) { }
}

