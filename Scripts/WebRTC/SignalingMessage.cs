using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GstreamerLauncher
{
    internal class SignalingMessage
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SignalingMessageType type;
        public string sdp;
        public string candidate;
        public string sdpMid;
        public int? sdpMLineIndex;
    }
}