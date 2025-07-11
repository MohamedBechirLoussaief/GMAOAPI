using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GMAOAPI.Models.Enumerations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StatutNotification
    {
        [EnumMember(Value = "Envoyée")]
        Envoyee,

        [EnumMember(Value = "Lue")]
        Lue
    }
}
