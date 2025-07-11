using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GMAOAPI.Models.Enumerations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypeIntervention
    {
        [EnumMember(Value = "Préventive")]
        Preventive,

        [EnumMember(Value = "Corrective")]
        Corrective,

        [EnumMember(Value = "Installation")]
        Installation
    }
}
