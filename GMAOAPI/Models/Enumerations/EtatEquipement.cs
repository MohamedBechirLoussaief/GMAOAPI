using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace GMAOAPI.Models.Enumerations
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EtatEquipement
    {
        [EnumMember(Value = "En attente d'installation")]
        EnAttenteInstallation,

        [EnumMember(Value = "En cours d'installation")]
        EnCoursInstallation,

        [EnumMember(Value = "En panne")]
        EnPanne,

        [EnumMember(Value = "Hors service")]
        HorsService,

        [EnumMember(Value = "En service")]
        EnService,

        [EnumMember(Value = "En maintenance")]
        EnMaintenance
    }
}
