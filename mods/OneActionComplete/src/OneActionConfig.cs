using System.Runtime.Serialization;

namespace Yuuka.DTMAPI.OneActionComplete
{
    [DataContract]
    internal sealed class OneActionConfig
    {
        [DataMember] public bool CompleteTrees { get; set; }
        [DataMember] public bool CompleteOres { get; set; }
        [DataMember] public bool CompleteGarbage { get; set; }
        [DataMember] public bool CompleteWeeds { get; set; }
        [DataMember] public bool CompleteMachineFuel { get; set; }
        [DataMember] public bool CompleteFeeder { get; set; }
        internal OneActionConfig Copy() => (OneActionConfig)MemberwiseClone();
    }
}
