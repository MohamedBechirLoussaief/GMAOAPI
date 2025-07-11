using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GMAOAPI.Models.Enumerations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SpecialiteTechnicien
    {
        [EnumMember(Value = "Électricité")]
        Electricite,

        [EnumMember(Value = "Mécanique")]
        Mecanique,

        [EnumMember(Value = "HVAC")]
        HVAC,

        [EnumMember(Value = "Électronique")]
        Electronique
    }
}
