using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GMAOAPI.Models.Enumerations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ActionType
    {
        [EnumMember(Value = "Création")]
        Creation,

        [EnumMember(Value = "Modification")]
        Modification,

        [EnumMember(Value = "Suppression")]
        Suppression
    }
}
