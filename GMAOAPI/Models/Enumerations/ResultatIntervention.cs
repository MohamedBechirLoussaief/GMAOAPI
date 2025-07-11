using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GMAOAPI.Models.Enumerations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ResultatIntervention
    {

        [EnumMember(Value = "Succès")]
        Succes,

        [EnumMember(Value = "Échec")]
        Echec,

        //[EnumMember(Value = "Partiel")]
        //Partiel
    }
}
