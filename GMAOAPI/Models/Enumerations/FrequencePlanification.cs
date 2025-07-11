using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GMAOAPI.Models.Enumerations
{
    [JsonConverter(typeof(StringEnumConverter))] 
    public enum FrequencePlanification
    {
        [EnumMember(Value = "Hebdomadaire")]
        Hebdomadaire,

        [EnumMember(Value = "Mensuelle")]
        Mensuelle,

        [EnumMember(Value = "Trimestrielle")]
        Trimestrielle,

        [EnumMember(Value = "Semestrielle")]
        Semestrielle,

        [EnumMember(Value = "Annuelle")]
        Annuelle,

        [EnumMember(Value = "Ponctuelle")]
        Ponctuelle
    }
}
