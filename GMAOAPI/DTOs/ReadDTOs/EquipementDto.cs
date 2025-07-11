using GMAOAPI.Models.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace GMAOAPI.DTOs.ReadDTOs
{
    public class EquipementDto
    {
        public int Id { get; set; }

        public string Nom { get; set; }

        public string Reference { get; set; }

        public string Localisation { get; set; }

        public DateTime? DateInstallation { get; set; }

        public EtatEquipement Etat { get; set; }

        public string Type { get; set; }

        public bool IsArchived { get; set; }

    }
}
