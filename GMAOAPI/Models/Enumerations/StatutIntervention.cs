using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GMAOAPI.Models.Enumerations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StatutIntervention
    {
        [EnumMember(Value = "En attente")]
        EnAttente,

        [EnumMember(Value = "En cours")]
        EnCours,

        [EnumMember(Value = "Terminée")]
        Terminee,

        [EnumMember(Value = "Annulée")]
        Annulee
    }
}
